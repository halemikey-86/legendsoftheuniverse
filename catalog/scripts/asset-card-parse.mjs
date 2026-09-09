import fs from "node:fs";
import path from "node:path";

export const SKIP_FOLDERS = new Set(["cardbacks", "card backs", "playmats"]);
export const SKIP_FILES = new Set(["tablebackground", "backofcard", "cardback"]);

export const FILENAME_TO_ID = {
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

export const FOLDER_TO_SET = {
  aliens: "Aliens",
  "goblin king set": "Goblin King",
  "history'sfinest": "History's Finest",
  jamestheendless: "James The Endless",
  "oval years": "Oval Years",
  riseofpride: "Rise of Pride",
  rivermerchant: "River Merchant",
  "scooby-doo": "Scooby-Doo",
};

export function normalize(value) {
  return (value ?? "").toLowerCase().replace(/[^a-z0-9]+/g, "");
}

export function humanize(text) {
  return text
    .replace(/_/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/\s+/g, " ")
    .trim();
}

export function setFromFolder(folder) {
  const key = folder.toLowerCase();
  if (FOLDER_TO_SET[key]) return FOLDER_TO_SET[key];
  return humanize(folder);
}

export function parseAssetPath(fullPath, assetsRoot) {
  const relative = path.relative(assetsRoot, fullPath).replace(/\\/g, "/");
  const parts = relative.split("/");
  const folder = parts.length > 1 ? parts[parts.length - 2] : "";
  const fileName = path.basename(fullPath);
  const base = path.basename(fullPath, path.extname(fullPath));
  const set = setFromFolder(folder);

  let cardType = "Companion";
  let subtype = "";
  let displayName = base;
  let isLegendaryIcon = false;
  let isJamesIcon = false;

  if (/^Legendary Icon_?/i.test(base)) {
    isLegendaryIcon = true;
    displayName = base.replace(/^Legendary Icon_?\s*/i, "");
    if (normalize(displayName) === "jamestheendless") {
      cardType = "Icon";
      isJamesIcon = true;
      subtype = "Endless";
    } else {
      cardType = "Companion";
      subtype = "Legendary Icon";
    }
  } else if (/^Companion_/i.test(base)) {
    displayName = base.replace(/^Companion_/i, "");
  } else if (/^Relic_/i.test(base)) {
    cardType = "Relic";
    displayName = base.replace(/^Relic_/i, "");
  } else if (/^Bond_/i.test(base)) {
    cardType = "Bond";
    displayName = base.replace(/^Bond_/i, "");
  } else if (/^Surge_/i.test(base)) {
    cardType = "Surge";
    displayName = base.replace(/^Surge_/i, "");
  } else if (/^WillSite|^Willsite/i.test(base)) {
    cardType = "Will";
    displayName = base.replace(/^WillSite_|^Willsite_/i, "");
  } else if (/^Kings_/i.test(base)) {
    cardType = "Relic";
    subtype = "Kings";
    displayName = base.replace(/^Kings_/i, "");
  }

  const name = humanize(displayName);
  const key = normalize(displayName);

  return {
    relative,
    folder,
    fileName,
    base,
    set,
    name,
    key,
    cardType,
    subtype,
    isLegendaryIcon,
    isJamesIcon,
  };
}

export function suggestCardId(asset) {
  if (FILENAME_TO_ID[asset.key]) return FILENAME_TO_ID[asset.key];
  const setSlug =
    normalize(asset.set).slice(0, 14) || "set";
  const nameSlug = asset.key.slice(0, 24).replace(/^-+|-+$/g, "") || "card";
  return `${setSlug}-${nameSlug}`.slice(0, 40);
}

export function collectImages(dir) {
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

export function buildStubCard(asset, number, iconCountInSet) {
  const id = suggestCardId(asset);
  let type = asset.cardType;
  let frame = "standard";

  if (asset.isJamesIcon) {
    type = "Icon";
    frame = "gold";
  } else if (asset.isLegendaryIcon && iconCountInSet < 3) {
    type = "Icon";
    frame = "gold";
  } else if (asset.isLegendaryIcon) {
    type = "Companion";
  }

  const hasCombat = type === "Icon" || type === "Companion";

  return {
    schemaVersion: "1.2",
    keywords: [],
    hunted: 0,
    aftereffect: null,
    mend: 0,
    doubleteam: 0,
    startsInPlay: false,
    id,
    set: asset.set,
    number,
    series: "",
    name: asset.name,
    type,
    subtype: asset.subtype,
    role: null,
    frame,
    willCost: 0,
    storeWorth: 0,
    honorCost: 0,
    honorGain: 0,
    strike: hasCombat ? 1 : 0,
    guard: hasCombat ? 1 : 0,
    health: hasCombat ? 1 : 0,
    abilities: [],
    spells: [],
    flavor: "",
    artPrompt: "",
    notes: "Auto-imported from Assets/Cards. Review stats and abilities in admin UI.",
  };
}
