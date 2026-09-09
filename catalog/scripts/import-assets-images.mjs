#!/usr/bin/env node
/**
 * Bulk-import front art from Unity Assets/Cards into the catalog API + Postgres.
 *
 * Usage:
 *   node scripts/import-assets-images.mjs --dry-run
 *   node scripts/import-assets-images.mjs --api https://willbound-catalog-api.fly.dev
 *   node scripts/import-assets-images.mjs --local
 */
import fs from "node:fs";
import path from "node:path";
import { root } from "./load-env.mjs";
import { createPgPool } from "./pg-config.mjs";

const args = process.argv.slice(2);
const dryRun = args.includes("--dry-run");
const localMode = args.includes("--local");
const apiFlag = args.indexOf("--api");
const apiBase = (
  apiFlag >= 0 ? args[apiFlag + 1] : process.env.VITE_API_BASE || process.env.PUBLIC_BASE_URL || "https://willbound-catalog-api.fly.dev"
).replace(/\/$/, "");
const assetsFlag = args.indexOf("--assets");
const defaultAssets = path.resolve(root, "..", "Assets", "Cards");
const assetsRoot = path.resolve(assetsFlag >= 0 ? args[assetsFlag + 1] : process.env.ASSETS_CARDS_DIR || defaultAssets);
const adminToken = process.env.ADMIN_TOKEN || process.env.VITE_ADMIN_TOKEN || "";

const SKIP_FOLDERS = new Set(["cardbacks", "card backs"]);
const SKIP_FILES = new Set(["tablebackground", "backofcard", "cardback"]);
const TYPE_PREFIX = /^(Companion|Relic|Bond|Surge|WillSite|Willsite|Kings)_/i;
const LEGENDARY_PREFIX = /^Legendary Icon_?/i;

/** Known James The Endless art filenames → catalog card ids (Engine block 01–10). */
const FILENAME_TO_ID = {
  legendaryiconjamestheendless: "endless-01",
  jamestheendless: "endless-01",
  eddietheprofessorbravo: "endless-02",
  stevehardrocklee: "endless-03",
  damonthedempseycole: "endless-04",
  sanjayblacklegvero: "endless-05",
  zanethebladesrorik: "endless-06",
  zanethreebladesrorik: "endless-06",
  ronintheanchorcross: "endless-07",
  ronantheanchorcross: "endless-07",
  ryueightlimbssoren: "endless-08",
  katoirongriptanaka: "endless-09",
  kaitoirongriptanaka: "endless-09",
  kironthemirrorren: "endless-10",
  kirothemirrorren: "endless-10",
};

const FOLDER_HINTS = [
  ["jamestheendless", "james the endless"],
  ["goblinkingset", "goblin king"],
  ["riseofpride", "rise of pride"],
  ["rivermerchant", "river merchant"],
  ["historysfinest", "history's finest"],
  ["oval years", "oval years"],
  ["aliens", "aliens"],
  ["scooby-doo", "scooby"],
];

function normalize(value) {
  return (value ?? "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "");
}

function parseAssetPath(fullPath) {
  const relative = path.relative(assetsRoot, fullPath).replace(/\\/g, "/");
  const parts = relative.split("/");
  const folder = parts.length > 1 ? parts[parts.length - 2] : "";
  const fileName = path.basename(fullPath);
  const base = path.basename(fullPath, path.extname(fullPath));
  let body = base.replace(TYPE_PREFIX, "").replace(LEGENDARY_PREFIX, "");
  body = body.replace(/_/g, " ").trim();
  return { relative, folder, fileName, base, body, key: normalize(body) };
}

function mimeForExt(ext) {
  return ext.toLowerCase() === ".png" ? "image/png" : "image/jpeg";
}

function folderSetHint(folder) {
  const key = normalize(folder);
  for (const [needle, setHint] of FOLDER_HINTS) {
    if (key.includes(normalize(needle))) return setHint;
  }
  return folder.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function scoreCard(card, asset, setHint) {
  const nameKey = normalize(card.name);
  const idKey = normalize(card.id);
  const setKey = normalize(card.set_name);

  if (FILENAME_TO_ID[asset.key] === card.id) return 1000;
  if (asset.key === idKey) return 900;
  if (asset.key === nameKey) return 850;
  if (nameKey.includes(asset.key) || asset.key.includes(nameKey)) {
    let score = 500 + Math.min(asset.key.length, nameKey.length);
    if (setHint && setKey.includes(normalize(setHint))) score += 100;
    return score;
  }

  const setNorm = normalize(setHint);
  if (setNorm && setKey.includes(setNorm) && asset.key.length >= 4) {
    const overlap = [...asset.key].filter((c, i) => nameKey[i] === c).length;
    if (overlap >= Math.min(asset.key.length, nameKey.length) * 0.6) {
      return 200 + overlap;
    }
  }

  return 0;
}

function findBestCard(cards, asset) {
  if (FILENAME_TO_ID[asset.key]) {
    const id = FILENAME_TO_ID[asset.key];
    const hit = cards.find((c) => c.id === id);
    if (hit) return { card: hit, score: 1000 };
  }

  const setHint = folderSetHint(asset.folder);
  let best = null;
  let bestScore = 0;
  for (const card of cards) {
    const score = scoreCard(card, asset, setHint);
    if (score > bestScore) {
      bestScore = score;
      best = card;
    }
  }
  if (bestScore >= 700) return { card: best, score: bestScore };
  return null;
}

function collectImages(dir) {
  const results = [];
  if (!fs.existsSync(dir)) return results;

  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (SKIP_FOLDERS.has(entry.name.toLowerCase())) continue;
      results.push(...collectImages(full));
      continue;
    }
    const ext = path.extname(entry.name).toLowerCase();
    if (![".jpg", ".jpeg", ".png"].includes(ext)) continue;
    if (SKIP_FILES.has(normalize(path.basename(entry.name, ext)))) continue;
    results.push(full);
  }
  return results;
}

async function fetchCardsRemote() {
  const res = await fetch(`${apiBase}/api/cards`);
  if (!res.ok) throw new Error(`GET /api/cards failed (${res.status})`);
  return res.json();
}

async function fetchCardsLocal(pool) {
  const { rows } = await pool.query("select id, name, set_name from cards order by set_name, number, name");
  return rows;
}

async function uploadRemote(cardId, filePath, fileName, mimeType) {
  if (!adminToken) throw new Error("ADMIN_TOKEN is required for remote upload");
  const data = fs.readFileSync(filePath);
  const form = new FormData();
  form.append("file", new Blob([data], { type: mimeType }), fileName);
  const res = await fetch(`${apiBase}/api/cards/${encodeURIComponent(cardId)}/images/front`, {
    method: "POST",
    headers: { Authorization: `Bearer ${adminToken}` },
    body: form,
  });
  const body = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(body.error ?? `Upload failed (${res.status})`);
  return body;
}

function saveLocal(cardId, filePath, fileName) {
  const ext = path.extname(fileName).toLowerCase() === ".jpeg" ? ".jpg" : path.extname(fileName).toLowerCase();
  const relative = `${cardId.toLowerCase()}/front${ext}`;
  const uploadRoot = path.resolve(process.env.UPLOAD_DIR ?? path.join(root, "uploads"));
  const dest = path.join(uploadRoot, relative);
  fs.mkdirSync(path.dirname(dest), { recursive: true });
  fs.copyFileSync(filePath, dest);
  return relative;
}

async function updateLocalPath(pool, cardId, relative) {
  await pool.query(
    "update cards set front_image_path = $1, updated_at = now() where id = $2",
    [relative, cardId],
  );
}

async function main() {
  console.log(`Assets: ${assetsRoot}`);
  console.log(`Mode: ${localMode ? "local (Postgres + disk)" : `API (${apiBase})`}${dryRun ? " · dry-run" : ""}`);

  if (!fs.existsSync(assetsRoot)) {
    throw new Error(`Assets folder not found: ${assetsRoot}`);
  }

  const images = collectImages(assetsRoot);
  console.log(`Found ${images.length} image(s)`);

  const pool = localMode ? createPgPool(false) : null;
  const cards = localMode ? await fetchCardsLocal(pool) : await fetchCardsRemote();
  console.log(`Catalog has ${cards.length} card(s)`);

  const matched = [];
  const skipped = [];
  const unmatched = [];

  for (const filePath of images) {
    const asset = parseAssetPath(filePath);
    const hit = findBestCard(cards, asset);
    if (!hit) {
      unmatched.push(asset.relative);
      continue;
    }
    matched.push({ filePath, asset, card: hit.card, score: hit.score });
  }

  console.log(`\nMatched ${matched.length}, unmatched ${unmatched.length}, skipped folders ${[...SKIP_FOLDERS].join(", ")}`);

  for (const { filePath, asset, card, score } of matched.sort((a, b) => a.asset.relative.localeCompare(b.asset.relative))) {
    const label = `${asset.relative} → ${card.id} (${card.name}) [${score}]`;
    if (dryRun) {
      console.log(`  would upload: ${label}`);
      continue;
    }

    try {
      const mime = mimeForExt(path.extname(asset.fileName));
      if (localMode) {
        const relative = saveLocal(card.id, filePath, asset.fileName);
        await updateLocalPath(pool, card.id, relative);
        console.log(`  ✓ local ${label}`);
      } else {
        await uploadRemote(card.id, filePath, asset.fileName, mime);
        console.log(`  ✓ api ${label}`);
      }
    } catch (err) {
      console.error(`  ✗ ${label}: ${err.message}`);
    }
  }

  if (unmatched.length > 0) {
    console.log("\nUnmatched (no catalog card yet — create in admin UI first):");
    for (const rel of unmatched.slice(0, 40)) console.log(`  ${rel}`);
    if (unmatched.length > 40) console.log(`  … and ${unmatched.length - 40} more`);
  }

  if (pool) await pool.end();
}

main().catch((err) => {
  console.error(err.message ?? err);
  process.exit(1);
});
