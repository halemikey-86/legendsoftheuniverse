#!/usr/bin/env node
/**
 * Export catalog JSON + copy card images for Unity.
 */
import fs from "node:fs";
import path from "node:path";
import { root } from "./load-env.mjs";
import { createPgPool } from "./pg-config.mjs";

const uploadRoot = path.resolve(process.env.UPLOAD_DIR ?? path.join(root, "uploads"));
const defaultJson = path.join(root, "dist", "willbound-cards.json");
const defaultImagesDir = path.join(root, "dist", "card-images");

const outJson = process.argv[2] ? path.resolve(process.argv[2]) : defaultJson;
let outImagesDir = process.env.UNITY_IMAGES_DIR
  ? path.resolve(process.env.UNITY_IMAGES_DIR)
  : defaultImagesDir;

if (outJson.includes(`${path.sep}StreamingAssets${path.sep}`)) {
  const assetsDir = path.resolve(outJson, "..", "..");
  outImagesDir = path.join(assetsDir, "Cards", "Catalog");
}

const pool = createPgPool(false);

function mapRow(row) {
  return {
    schemaVersion: row.schema_version ?? "1.2",
    keywords: row.keywords ?? [],
    hunted: row.hunted ?? 0,
    aftereffect: row.aftereffect ?? null,
    mend: row.mend ?? 0,
    doubleteam: row.doubleteam ?? 0,
    startsInPlay: row.starts_in_play ?? false,
    id: row.id,
    set: row.set_name,
    number: row.number,
    series: row.series,
    name: row.name,
    type: row.type,
    subtype: row.subtype,
    role: row.role,
    frame: row.frame,
    willCost: row.will_cost,
    storeWorth: row.store_worth,
    honorCost: row.honor_cost,
    honorGain: row.honor_gain,
    strike: row.strike,
    guard: row.guard,
    health: row.health,
    abilities: row.abilities ?? [],
    spells: row.spells ?? [],
    flavor: row.flavor ?? "",
    artPrompt: row.art_prompt ?? "",
    notes: row.notes ?? "",
    frontImage: row.front_image_path ?? null,
    backImage: row.back_image_path ?? null,
  };
}

function copyImage(relativePath, destDir) {
  if (!relativePath) return null;
  const src = path.join(uploadRoot, relativePath);
  if (!fs.existsSync(src)) {
    console.warn(`  ⚠ missing upload: ${relativePath}`);
    return null;
  }
  const dest = path.join(destDir, relativePath.replace(/\\/g, "/"));
  fs.mkdirSync(path.dirname(dest), { recursive: true });
  fs.copyFileSync(src, dest);
  return relativePath.replace(/\\/g, "/");
}

try {
  const { rows } = await pool.query(`select * from cards order by set_name, number, name`);
  fs.mkdirSync(path.dirname(outJson), { recursive: true });
  fs.mkdirSync(outImagesDir, { recursive: true });

  let copied = 0;
  const cards = rows.map((row) => {
    const mapped = mapRow(row);
    if (copyImage(row.front_image_path, outImagesDir)) copied += 1;
    if (copyImage(row.back_image_path, outImagesDir)) copied += 1;
    return mapped;
  });

  const payload = {
    schemaVersion: "1.2",
    exportedAt: new Date().toISOString(),
    cards,
  };
  fs.writeFileSync(outJson, JSON.stringify(payload, null, 2));
  console.log(`Exported ${rows.length} card(s) → ${outJson}`);
  console.log(`Copied ${copied} image(s) → ${outImagesDir}`);
} catch (err) {
  console.error("Export failed:", err.message);
  process.exit(1);
} finally {
  await pool.end();
}
