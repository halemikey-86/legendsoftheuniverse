import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

export type ImageSlot = "front" | "back";

const ALLOWED_MIME = new Set(["image/jpeg", "image/png"]);
const ALLOWED_EXT = new Set([".jpg", ".jpeg", ".png"]);
const MAX_BYTES = 8 * 1024 * 1024;

const __dirname = path.dirname(fileURLToPath(import.meta.url));
export const UPLOAD_ROOT = path.resolve(
  process.env.UPLOAD_DIR ?? path.join(__dirname, "..", "..", "uploads"),
);

export function imagePublicUrl(relativePath: string | null | undefined, baseUrl: string) {
  if (!relativePath) return null;
  const normalized = relativePath.replace(/\\/g, "/");
  return `${baseUrl.replace(/\/$/, "")}/uploads/${normalized}`;
}

export function cardImageRelativePath(cardId: string, slot: ImageSlot, ext: string): string {
  const safeId = sanitizeCardId(cardId);
  const safeExt = ext.toLowerCase() === ".jpeg" ? ".jpg" : ext.toLowerCase();
  return `${safeId}/${slot}${safeExt}`;
}

export function sanitizeCardId(cardId: string): string {
  const trimmed = cardId.trim().toLowerCase();
  if (!/^[a-z0-9-]+$/.test(trimmed)) {
    throw new Error("Invalid card id for image storage");
  }
  return trimmed;
}

export function assertAllowedImage(fileName: string, mimeType: string, size: number) {
  const ext = path.extname(fileName).toLowerCase();
  if (!ALLOWED_EXT.has(ext)) {
    throw new Error("Only .jpg and .png images are allowed");
  }
  if (!ALLOWED_MIME.has(mimeType)) {
    throw new Error("File must be image/jpeg or image/png");
  }
  if (size <= 0 || size > MAX_BYTES) {
    throw new Error(`Image must be between 1 byte and ${MAX_BYTES / (1024 * 1024)} MB`);
  }
}

export function resolveUploadPath(relativePath: string): string {
  const full = path.resolve(UPLOAD_ROOT, relativePath);
  if (!full.startsWith(UPLOAD_ROOT)) {
    throw new Error("Invalid image path");
  }
  return full;
}

export async function saveCardImage(
  cardId: string,
  slot: ImageSlot,
  fileName: string,
  mimeType: string,
  data: Buffer,
): Promise<string> {
  assertAllowedImage(fileName, mimeType, data.length);
  const ext = path.extname(fileName).toLowerCase() === ".jpeg" ? ".jpg" : path.extname(fileName).toLowerCase();
  const relative = cardImageRelativePath(cardId, slot, ext);
  const full = resolveUploadPath(relative);
  fs.mkdirSync(path.dirname(full), { recursive: true });

  // Remove other extensions for same slot
  for (const other of [".jpg", ".png"]) {
    const candidate = resolveUploadPath(cardImageRelativePath(cardId, slot, other));
    if (candidate !== full && fs.existsSync(candidate)) {
      fs.unlinkSync(candidate);
    }
  }

  fs.writeFileSync(full, data);
  return relative;
}

export function deleteCardImage(relativePath: string | null | undefined) {
  if (!relativePath) return;
  const full = resolveUploadPath(relativePath);
  if (fs.existsSync(full)) fs.unlinkSync(full);
}

export function deleteAllCardImages(cardId: string) {
  const dir = path.join(UPLOAD_ROOT, sanitizeCardId(cardId));
  if (fs.existsSync(dir)) {
    fs.rmSync(dir, { recursive: true, force: true });
  }
}

export function contentTypeForPath(relativePath: string): string {
  return relativePath.toLowerCase().endsWith(".png") ? "image/png" : "image/jpeg";
}

export function ensureUploadRoot() {
  fs.mkdirSync(UPLOAD_ROOT, { recursive: true });
}
