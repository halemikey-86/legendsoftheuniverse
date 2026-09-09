#!/usr/bin/env node
/**
 * Create catalog stub cards from Assets/Cards filenames and upload front art.
 *
 *   node scripts/sync-assets-catalog.mjs --dry-run
 *   node scripts/sync-assets-catalog.mjs --api https://willbound-catalog-api.fly.dev
 */
import fs from "node:fs";
import path from "node:path";
import { root } from "./load-env.mjs";
import { createPgPool } from "./pg-config.mjs";
import {
  FILENAME_TO_ID,
  buildStubCard,
  collectImages,
  normalize,
  parseAssetPath,
  suggestCardId,
} from "./asset-card-parse.mjs";

const args = process.argv.slice(2);
const dryRun = args.includes("--dry-run");
const apiFlag = args.indexOf("--api");
const apiBase = (
  apiFlag >= 0 ? args[apiFlag + 1] : process.env.VITE_API_BASE || process.env.PUBLIC_BASE_URL || "https://willbound-catalog-api.fly.dev"
).replace(/\/$/, "");
const assetsFlag = args.indexOf("--assets");
const assetsRoot = path.resolve(
  assetsFlag >= 0 ? args[assetsFlag + 1] : process.env.ASSETS_CARDS_DIR || path.resolve(root, "..", "Assets", "Cards"),
);
const adminToken = process.env.ADMIN_TOKEN || process.env.VITE_ADMIN_TOKEN || "";

function mimeForExt(ext) {
  return ext.toLowerCase() === ".png" ? "image/png" : "image/jpeg";
}

function findExisting(cards, asset) {
  const mappedId = FILENAME_TO_ID[asset.key];
  if (mappedId) {
    const hit = cards.find((c) => c.id === mappedId);
    if (hit) return hit;
  }
  const id = suggestCardId(asset);
  const byId = cards.find((c) => c.id === id);
  if (byId) return byId;
  const cardSet = (c) => c.set ?? c.set_name;
  return cards.find(
    (c) => cardSet(c) === asset.set && normalize(c.name) === normalize(asset.name),
  );
}

function nextNumber(existingInSet, usedNumbers) {
  let max = 0;
  for (const c of existingInSet) {
    const n = Number((c.number ?? "").split("/")[0]?.replace(/\D/g, ""));
    if (Number.isFinite(n)) max = Math.max(max, n);
  }
  for (const n of usedNumbers) max = Math.max(max, n);
  const next = max + 1;
  usedNumbers.add(next);
  return `${String(next).padStart(2, "0")}/??`;
}

async function fetchCards(api) {
  const res = await fetch(`${api}/api/cards`);
  if (!res.ok) throw new Error(`GET /api/cards failed (${res.status})`);
  return res.json();
}

async function createCardRemote(card) {
  if (!adminToken) throw new Error("ADMIN_TOKEN required");
  const res = await fetch(`${apiBase}/api/cards`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${adminToken}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(card),
  });
  const body = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(body.error ?? `Create failed (${res.status})`);
  return body.card ?? body;
}

async function insertCardLocal(pool, card) {
  await pool.query(
    `insert into cards (
       id, schema_version, set_name, number, series, name, type, subtype, role, frame,
       will_cost, store_worth, honor_cost, honor_gain, strike, guard, health,
       keywords, hunted, aftereffect, mend, doubleteam, starts_in_play,
       abilities, spells, flavor, art_prompt, notes
     ) values (
       $1,$2,$3,$4,$5,$6,$7,$8,$9,$10,
       $11,$12,$13,$14,$15,$16,$17,
       '[]'::jsonb,$18,null,$19,$20,$21,
       '[]'::jsonb,'[]'::jsonb,$22,$23,$24
     )
     on conflict (id) do nothing`,
    [
      card.id,
      card.schemaVersion,
      card.set,
      card.number,
      card.series,
      card.name,
      card.type,
      card.subtype,
      card.role,
      card.frame,
      card.willCost,
      card.storeWorth,
      card.honorCost,
      card.honorGain,
      card.strike,
      card.guard,
      card.health,
      card.hunted,
      card.mend,
      card.doubleteam,
      card.startsInPlay,
      card.flavor,
      card.artPrompt,
      card.notes,
    ],
  );
}

async function uploadRemote(cardId, filePath, fileName, mimeType) {
  if (!adminToken) throw new Error("ADMIN_TOKEN required");
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

async function main() {
  console.log(`Assets: ${assetsRoot}`);
  console.log(`API: ${apiBase}${dryRun ? " · dry-run" : ""}`);

  if (!fs.existsSync(assetsRoot)) throw new Error(`Not found: ${assetsRoot}`);

  const entries = collectImages(assetsRoot).map((filePath) => ({
    filePath,
    asset: parseAssetPath(filePath, assetsRoot),
  }));
  console.log(`Found ${entries.length} art file(s)`);

  let cards = await fetchCards(apiBase);
  console.log(`Catalog starts with ${cards.length} card(s)`);

  const pool = dryRun ? null : createPgPool(false);
  const usedNumbers = new Map();
  let created = 0;
  let uploaded = 0;
  let skipped = 0;

  const cardSet = (c) => c.set ?? c.set_name;

  for (const { filePath, asset } of entries.sort((a, b) => a.asset.relative.localeCompare(b.asset.relative))) {
    let existing = findExisting(cards, asset);
    let cardId = existing?.id;

    if (!existing) {
      const setCards = cards.filter((c) => cardSet(c) === asset.set);
      const iconCount = setCards.filter((c) => c.type === "Icon").length;
      if (!usedNumbers.has(asset.set)) usedNumbers.set(asset.set, new Set());
      const number = nextNumber(setCards, usedNumbers.get(asset.set));
      const stub = buildStubCard(asset, number, iconCount);

      if (dryRun) {
        console.log(`  would create: ${stub.id} · ${stub.name} (${stub.type}) · ${asset.relative}`);
        cardId = stub.id;
        cards.push({ id: stub.id, name: stub.name, set: stub.set, type: stub.type, number: stub.number });
        created += 1;
      } else {
        try {
          try {
            await createCardRemote(stub);
          } catch (err) {
            const msg = String(err.message ?? err);
            if (msg.includes("already") || msg.includes("Icon")) {
              await insertCardLocal(pool, stub);
            } else {
              throw err;
            }
          }
          console.log(`  + created ${stub.id} · ${stub.name}`);
          cards.push({ id: stub.id, name: stub.name, set: stub.set, type: stub.type, number: stub.number });
          cardId = stub.id;
          created += 1;
        } catch (err) {
          console.error(`  ✗ create ${asset.relative}: ${err.message}`);
          skipped += 1;
          continue;
        }
      }
    } else {
      cardId = existing.id;
    }

    if (dryRun) {
      console.log(`  would upload: ${asset.relative} → ${cardId}`);
      uploaded += 1;
      continue;
    }

    try {
      await uploadRemote(cardId, filePath, asset.fileName, mimeForExt(path.extname(asset.fileName)));
      console.log(`  ✓ image ${asset.relative} → ${cardId}`);
      uploaded += 1;
    } catch (err) {
      console.error(`  ✗ upload ${asset.relative}: ${err.message}`);
      skipped += 1;
    }
  }

  console.log(`\nDone — created ${created}, uploaded ${uploaded}, skipped ${skipped}`);
  if (pool) await pool.end();
}

main().catch((err) => {
  console.error(err.message ?? err);
  process.exit(1);
});
