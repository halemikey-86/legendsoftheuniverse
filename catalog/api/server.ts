import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath, URL } from "node:url";
import dotenv from "dotenv";
import {
  attachImageUrls,
  clearCardImagePath,
  createCard,
  createSql,
  deleteCard,
  exportEngineCatalog,
  getCardById,
  getLibraryStats,
  listAllCards,
  parseSearch,
  searchCards,
  setCardImagePath,
  updateCard,
  validateOnly,
} from "../web/src/lib/cards-db.js";
import {
  contentTypeForPath,
  deleteCardImage,
  ensureUploadRoot,
  resolveUploadPath,
  saveCardImage,
  type ImageSlot,
} from "../web/src/lib/image-storage.js";
import { parseMultipart } from "../web/src/lib/multipart.js";
import { resolveDatabaseUrl } from "../web/src/lib/pg-pool.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
dotenv.config({ path: path.resolve(__dirname, "..", ".env") });

const connectionString = resolveDatabaseUrl(false);

const port = Number(process.env.PORT ?? process.env.API_PORT ?? 8787);
const publicBaseUrl = process.env.PUBLIC_BASE_URL ?? `http://localhost:${port}`;
const corsOrigins = (process.env.CORS_ORIGIN ?? "http://localhost:5173")
  .split(",")
  .map((o) => o.trim())
  .filter(Boolean);

const adminToken = process.env.ADMIN_TOKEN ?? "";

ensureUploadRoot();
const sqlPromise = createSql(connectionString);

function corsHeaders(req?: http.IncomingMessage) {
  const requestOrigin = req?.headers.origin;
  const allowOrigin =
    requestOrigin && corsOrigins.includes(requestOrigin)
      ? requestOrigin
      : corsOrigins[0] ?? "*";

  return {
    "Access-Control-Allow-Origin": allowOrigin,
    "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type, Authorization",
    Vary: "Origin",
  };
}

function json(res: http.ServerResponse, status: number, body: unknown, req?: http.IncomingMessage) {
  const payload = JSON.stringify(body);
  res.writeHead(status, {
    "Content-Type": "application/json",
    ...corsHeaders(req),
  });
  res.end(payload);
}

async function readBody(req: http.IncomingMessage): Promise<unknown> {
  const chunks: Buffer[] = [];
  for await (const chunk of req) chunks.push(Buffer.from(chunk));
  const text = Buffer.concat(chunks).toString("utf8");
  if (!text.trim()) return {};
  return JSON.parse(text);
}

function requireAdmin(req: http.IncomingMessage) {
  if (!adminToken) return;
  const auth = req.headers.authorization ?? "";
  const token = auth.startsWith("Bearer ") ? auth.slice(7) : "";
  if (token !== adminToken) {
    throw new Error("Unauthorized");
  }
}

function withUrls<T extends { frontImagePath?: string | null; backImagePath?: string | null }>(
  card: T,
) {
  const [enriched] = attachImageUrls([card as never], publicBaseUrl);
  return enriched;
}

function serveUpload(relativePath: string, res: http.ServerResponse, req: http.IncomingMessage) {
  const full = resolveUploadPath(relativePath);
  if (!fs.existsSync(full)) {
    json(res, 404, { error: "Image not found" }, req);
    return;
  }
  const data = fs.readFileSync(full);
  res.writeHead(200, {
    "Content-Type": contentTypeForPath(relativePath),
    "Cache-Control": "public, max-age=3600",
    ...corsHeaders(req),
  });
  res.end(data);
}

function parseImageRoute(path: string): { cardId: string; slot: ImageSlot } | null {
  const match = /^\/api\/cards\/([^/]+)\/images\/(front|back)$/.exec(path);
  if (!match) return null;
  return { cardId: decodeURIComponent(match[1]), slot: match[2] as ImageSlot };
}

const server = http.createServer(async (req, res) => {
  if (req.method === "OPTIONS") {
    res.writeHead(204, corsHeaders(req));
    res.end();
    return;
  }

  try {
    const sql = await sqlPromise;
    const url = new URL(req.url ?? "/", `http://localhost:${port}`);
    const path = url.pathname;

    if (req.method === "GET" && path.startsWith("/uploads/")) {
      serveUpload(decodeURIComponent(path.slice("/uploads/".length)), res, req);
      return;
    }

    if (req.method === "GET" && path === "/api/health") {
      json(res, 200, { ok: true }, req);
      return;
    }

    if (req.method === "GET" && path === "/api/cards/stats") {
      json(res, 200, await getLibraryStats(sql), req);
      return;
    }

    if (req.method === "GET" && path === "/api/cards/export") {
      json(res, 200, await exportEngineCatalog(sql), req);
      return;
    }

    if (req.method === "GET" && path === "/api/cards") {
      const search = parseSearch(Object.fromEntries(url.searchParams.entries()));
      const hasFilters = Object.values(search).some((v) => v && v !== "name");
      const cards = hasFilters ? await searchCards(sql, search) : await listAllCards(sql);
      json(res, 200, attachImageUrls(cards, publicBaseUrl), req);
      return;
    }

    const imageRoute = parseImageRoute(path);
    if (imageRoute && req.method === "POST") {
      requireAdmin(req);
      const existing = await getCardById(sql, imageRoute.cardId);
      if (!existing) throw new Error("Card not found — save the card before uploading art");

      const parsed = await parseMultipart(req);
      if (!parsed.file) throw new Error("Choose a .jpg or .png file");

      const relative = await saveCardImage(
        imageRoute.cardId,
        imageRoute.slot,
        parsed.file.fileName,
        parsed.file.mimeType,
        parsed.file.data,
      );
      const card = await setCardImagePath(sql, imageRoute.cardId, imageRoute.slot, relative);
      json(res, 200, { card: withUrls(card) }, req);
      return;
    }

    if (imageRoute && req.method === "DELETE") {
      requireAdmin(req);
      const existing = await getCardById(sql, imageRoute.cardId);
      if (!existing) throw new Error("Card not found");

      const currentPath =
        imageRoute.slot === "front" ? existing.frontImagePath : existing.backImagePath;
      deleteCardImage(currentPath);
      const card = await clearCardImagePath(sql, imageRoute.cardId, imageRoute.slot);
      json(res, 200, { card: withUrls(card) }, req);
      return;
    }

    if (req.method === "POST" && path === "/api/cards/validate") {
      requireAdmin(req);
      const body = await readBody(req);
      const originalId = typeof (body as { originalId?: string }).originalId === "string"
        ? (body as { originalId: string }).originalId
        : undefined;
      const issues = await validateOnly(sql, (body as { card?: unknown }).card ?? body, originalId);
      json(res, 200, { issues }, req);
      return;
    }

    if (req.method === "POST" && path === "/api/cards") {
      requireAdmin(req);
      const body = await readBody(req);
      const result = await createCard(sql, body);
      json(res, 201, { ...result, card: withUrls(result.card) }, req);
      return;
    }

    if (req.method === "PUT" && path.startsWith("/api/cards/") && !path.includes("/images/")) {
      requireAdmin(req);
      const originalId = decodeURIComponent(path.slice("/api/cards/".length));
      const body = await readBody(req);
      const result = await updateCard(sql, body, originalId);
      json(res, 200, { ...result, card: withUrls(result.card) }, req);
      return;
    }

    if (req.method === "DELETE" && path.startsWith("/api/cards/") && !path.includes("/images/")) {
      requireAdmin(req);
      const id = decodeURIComponent(path.slice("/api/cards/".length));
      json(res, 200, await deleteCard(sql, id), req);
      return;
    }

    json(res, 404, { error: "Not found" }, req);
  } catch (err) {
    const message = err instanceof Error ? err.message : "Server error";
    const status = message === "Unauthorized" ? 401 : 400;
    json(res, status, { error: message }, req);
  }
});

server.listen(port, () => {
  console.log(`WILLBOUND catalog API on http://localhost:${port}`);
  console.log(`Uploads: ${process.env.UPLOAD_DIR ?? "catalog/uploads"}`);
  console.log(`CORS origins: ${corsOrigins.join(", ")}`);
});
