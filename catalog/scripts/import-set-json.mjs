#!/usr/bin/env node
/**
 * Import a structured set JSON into Supabase (upsert cards + upload art).
 *
 *   node scripts/import-set-json.mjs catalog/examples/goblin-king-set.json
 *   node scripts/import-set-json.mjs catalog/examples/goblin-king-set.json --dry-run
 */
import fs from "node:fs";
import path from "node:path";
import { root } from "./load-env.mjs";
import { createPgPool } from "./pg-config.mjs";

const VALID_KEYWORDS = new Set([
  "Aggression",
  "Bazerk",
  "Gashing",
  "HeavyHitter",
  "Still",
  "Drain",
  "Sealed",
  "Closed",
  "Silence",
]);

const VALID_ROLES = new Set(["Healer", "Tank", "Striker", "Play Maker", "Recursor"]);

/** Default fileKey → filename overrides (Goblin King legacy names). Per-set JSON may supply assetMap. */
const DEFAULT_ASSET_MAP = {
  Icon_GoblinKing: "Legendary Icon_TheGoblinKing",
  Companion_CaveSneak: "Companion_CavSneak",
  Relic_KingsCleaver: "Kings_Cleaver",
  Companion_MatronofthePits: "Companion_MatronOfThePits",
  Willsite_ThePits: "WillSites_ThePits",
  Willsite_TitheStone: "WillSite_TitheStone",
  Willsite_WarrensMouth: "WillSite_WarrensMouth",
  Willsite_AshAltar: "WillSite_AshAlter",
  Willsite_MusteringGate: "WillSite_MusteringGate",
  Surge_FlingtheWeak: "Surge_FlingTheWeak",
  Bond_KingsShadow: "Bond_KingsShadwo",
  Relic_ScrapforgeAnvil: "Relic_ScapforgeAnvil",
  Relic_CagesoftheTithe: "Relic_CagesOfTheTithe",
  Relic_CrownHook: "Bond_CrownHook",
};

const args = process.argv.slice(2).filter((a) => !a.startsWith("--"));
const flags = new Set(process.argv.slice(2).filter((a) => a.startsWith("--")));
const dryRun = flags.has("--dry-run");
const jsonPath = path.resolve(args[0] ?? path.join(root, "examples", "goblin-king-set.json"));
const assetsRoot = path.resolve(
  process.env.ASSETS_CARDS_DIR ?? path.join(root, "..", "Assets", "Cards"),
);
let artFolder = "Goblin King Set";
let fileKeyAsset = { ...DEFAULT_ASSET_MAP };
const apiBase = (
  process.env.VITE_API_BASE || process.env.PUBLIC_BASE_URL || "https://willbound-catalog-api.fly.dev"
).replace(/\/$/, "");
const adminToken = process.env.ADMIN_TOKEN || process.env.VITE_ADMIN_TOKEN || "";

function inferTiming(raw) {
  const text = raw.text ?? "";
  if (raw.type === "Surge") return "now";
  if (raw.type === "Will site" || raw.type === "Will") return "static";
  if (/once per turn/i.test(text) && raw.type === "Icon") return "activated";
  if (/when this enters|when this enters the field/i.test(text)) return "onPlay";
  if (/when this presses|when this is pressed/i.test(text)) return "onPress";
  if (/when this is removed/i.test(text)) return "onRemoved";
  if (/at the start of your/i.test(text)) return "startStep";
  if (/at the end of your turn/i.test(text)) return "endStep";
  return "static";
}

function mapType(type) {
  if (type === "Will site") return "Will";
  if (type === "Token") return "Companion";
  return type;
}

function mapRole(raw, cardType) {
  if (cardType !== "Companion") return null;
  const role = raw.role?.trim();
  if (role && VALID_ROLES.has(role)) return role;
  return null;
}

function mapKeywords(raw) {
  return (raw.keywords ?? []).filter((k) => VALID_KEYWORDS.has(k));
}

function abilitiesFrom(raw) {
  const text = raw.text?.trim();
  if (!text) return [];
  return [
    {
      name: raw.name,
      timing: inferTiming(raw),
      costWill: 0,
      costHonor: 0,
      oncePerTurn: /once per turn/i.test(text),
      oncePerGame: /once per game/i.test(text),
      target: null,
      text,
    },
  ];
}

function mapCard(raw, setName, setCode) {
  const type = mapType(raw.type);
  const hasCombat = type === "Icon" || type === "Companion";
  const number = (raw.collector ?? "").replace(new RegExp(`^${setCode}\\s*·\\s*`, "i"), "").trim();
  const qtyNote = raw.qty != null ? `Qty in set: ${raw.qty}` : "";
  const notes = [qtyNote, raw.type === "Token" ? "TOKEN — not storeable." : ""]
    .filter(Boolean)
    .join(" · ");

  return {
    id: raw.id,
    schemaVersion: "1.2",
    set: setName,
    number,
    series: setCode,
    name: raw.name,
    type,
    subtype: raw.subtype ?? (raw.type === "Token" ? "Token" : ""),
    role: mapRole(raw, type),
    frame: type === "Icon" ? "gold" : "standard",
    willCost: raw.will ?? 0,
    storeWorth: raw.worth ?? 0,
    honorCost: 0,
    honorGain: 0,
    strike: hasCombat ? (raw.strike ?? 0) : 0,
    guard: hasCombat ? (raw.guard ?? 0) : 0,
    health: hasCombat ? (raw.health ?? (type === "Companion" ? 1 : 0)) : 0,
    keywords: mapKeywords(raw),
    hunted: 0,
    aftereffect: null,
    mend: 0,
    doubleteam: 0,
    startsInPlay: type === "Icon",
    abilities: abilitiesFrom(raw),
    spells: [],
    flavor: raw.flavor ?? "",
    artPrompt: raw.typeLine ?? "",
    notes: notes || `Imported from ${setName} JSON.`,
    fileKey: raw.fileKey ?? null,
  };
}

function resolveArtPath(fileKey) {
  if (!fileKey) return null;
  const base = fileKeyAsset[fileKey] ?? fileKey;
  const folder = path.join(assetsRoot, artFolder);
  for (const ext of [".jpg", ".jpeg", ".png"]) {
    const candidate = path.join(folder, `${base}${ext}`);
    if (fs.existsSync(candidate)) return candidate;
  }
  return null;
}

async function upsertCard(pool, card) {
  const { fileKey, ...row } = card;
  await pool.query(
    `insert into cards (
       id, schema_version, set_name, number, series, name, type, subtype, role, frame,
       will_cost, store_worth, honor_cost, honor_gain, strike, guard, health,
       keywords, hunted, aftereffect, mend, doubleteam, starts_in_play,
       abilities, spells, flavor, art_prompt, notes
     ) values (
       $1,$2,$3,$4,$5,$6,$7,$8,$9,$10,
       $11,$12,$13,$14,$15,$16,$17,
       $18::jsonb,$19,$20::jsonb,$21,$22,$23,
       $24::jsonb,$25::jsonb,$26,$27,$28
     )
     on conflict (id) do update set
       schema_version = excluded.schema_version,
       set_name = excluded.set_name,
       number = excluded.number,
       series = excluded.series,
       name = excluded.name,
       type = excluded.type,
       subtype = excluded.subtype,
       role = excluded.role,
       frame = excluded.frame,
       will_cost = excluded.will_cost,
       store_worth = excluded.store_worth,
       honor_cost = excluded.honor_cost,
       honor_gain = excluded.honor_gain,
       strike = excluded.strike,
       guard = excluded.guard,
       health = excluded.health,
       keywords = excluded.keywords,
       hunted = excluded.hunted,
       aftereffect = excluded.aftereffect,
       mend = excluded.mend,
       doubleteam = excluded.doubleteam,
       starts_in_play = excluded.starts_in_play,
       abilities = excluded.abilities,
       spells = excluded.spells,
       flavor = excluded.flavor,
       art_prompt = excluded.art_prompt,
       notes = excluded.notes,
       updated_at = now()`,
    [
      row.id,
      row.schemaVersion,
      row.set,
      row.number,
      row.series,
      row.name,
      row.type,
      row.subtype,
      row.role,
      row.frame,
      row.willCost,
      row.storeWorth,
      row.honorCost,
      row.honorGain,
      row.strike,
      row.guard,
      row.health,
      JSON.stringify(row.keywords),
      row.hunted,
      null,
      row.mend,
      row.doubleteam,
      row.startsInPlay,
      JSON.stringify(row.abilities),
      JSON.stringify(row.spells),
      row.flavor,
      row.artPrompt,
      row.notes,
    ],
  );
}

async function uploadImage(cardId, filePath) {
  if (!adminToken) throw new Error("ADMIN_TOKEN required for image upload");
  const ext = path.extname(filePath).toLowerCase();
  const mime = ext === ".png" ? "image/png" : "image/jpeg";
  const data = fs.readFileSync(filePath);
  const form = new FormData();
  form.append("file", new Blob([data], { type: mime }), path.basename(filePath));
  const res = await fetch(`${apiBase}/api/cards/${encodeURIComponent(cardId)}/images/front`, {
    method: "POST",
    headers: { Authorization: `Bearer ${adminToken}` },
    body: form,
  });
  const body = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(body.error ?? `Upload failed (${res.status})`);
}

async function upsertSetTaxonomy(pool, setCode, setName) {
  await pool.query(
    `insert into catalog_taxonomy (kind, name, code)
     values ('set', $1, $2)
     on conflict (kind, name) do update set code = excluded.code`,
    [setName, setCode],
  );
}

async function main() {
  const payload = JSON.parse(fs.readFileSync(jsonPath, "utf8"));
  const setName = payload.set?.name ?? "Unknown Set";
  const setCode = payload.set?.code ?? "SET";
  artFolder = payload.set?.assetsFolder ?? artFolder;
  fileKeyAsset = { ...DEFAULT_ASSET_MAP, ...(payload.assetMap ?? {}) };
  const entries = [...(payload.cards ?? [])];
  if (payload.token) entries.push(payload.token);

  console.log(
    `Importing ${entries.length} card(s) into "${setName}" (${setCode}) from ${jsonPath}${dryRun ? " (dry-run)" : ""}`,
  );
  console.log(`  art folder: ${path.join(assetsRoot, artFolder)}`);

  const mapped = entries.map((raw) => mapCard(raw, setName, setCode));
  const pool = dryRun ? null : createPgPool(false);

  if (!dryRun && pool) {
    const idPrefix = `${setCode.toLowerCase()}-`;
    const { rowCount } = await pool.query(
      `delete from cards
       where (series = $1 and id not like $2)
          or set_name = $3
          or id like $4`,
      [setCode, `${idPrefix}%`, setName, `${setCode.toLowerCase()}king-%`],
    );
    if (rowCount > 0) console.log(`Removed ${rowCount} old stub(s) for this set`);

    await upsertSetTaxonomy(pool, setCode, setName);
    console.log(`  ✓ taxonomy set ${setCode} · ${setName}`);
  }

  for (const card of mapped) {
    const artPath = resolveArtPath(card.fileKey);
    if (dryRun) {
      console.log(`  would upsert ${card.id} · ${card.name} (${card.type}) ${card.number}${artPath ? " + art" : ""}`);
      continue;
    }
    await upsertCard(pool, card);
    console.log(`  ✓ ${card.id} · ${card.name}`);
    if (artPath) {
      try {
        await uploadImage(card.id, artPath);
        console.log(`    ✓ art ${path.basename(artPath)}`);
      } catch (err) {
        console.warn(`    ⚠ art ${card.fileKey}: ${err.message}`);
      }
    } else if (card.fileKey) {
      console.warn(`    ⚠ no art for ${card.fileKey}`);
    }
  }

  if (pool) await pool.end();
  console.log("Done.");
}

main().catch((err) => {
  console.error(err.message ?? err);
  process.exit(1);
});
