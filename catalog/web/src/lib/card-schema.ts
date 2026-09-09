import { z } from "zod";

export const SCHEMA_VERSION = "1.2" as const;

export const CARD_TYPES = [
  "Icon",
  "Companion",
  "Relic",
  "Bond",
  "Surge",
  "Will",
] as const;
export type CardType = (typeof CARD_TYPES)[number];

export const ROLES = [
  "Healer",
  "Tank",
  "Striker",
  "Play Maker",
  "Recursor",
] as const;
export type Role = (typeof ROLES)[number];

export const FRAMES = ["standard", "gold", "universe"] as const;
export type Frame = (typeof FRAMES)[number];

export const KEYWORDS = [
  "Aggression",
  "Bazerk",
  "Gashing",
  "HeavyHitter",
  "Still",
  "Drain",
  "Sealed",
  "Closed",
  "Silence",
] as const;
export type Keyword = (typeof KEYWORDS)[number];

export const TIMINGS = [
  "static",
  "onPlay",
  "onPress",
  "afterPress",
  "onHold",
  "onRemoved",
  "replacement",
  "activated",
  "now",
  "startStep",
  "endStep",
] as const;
export type Timing = (typeof TIMINGS)[number];

/** Common sets — free text is allowed; these power datalist suggestions. */
export const SETS = [
  "James The Endless",
  "10th Planet",
  "Mostorno",
  "Politics of Time",
  "Classic Cartoon",
  "Aliens",
  "Goblin King",
  "History's Finest",
  "Oval Years",
  "Rise of Pride",
  "River Merchant",
  "Scooby-Doo",
] as const;
export type SetName = (typeof SETS)[number];

/** Common series — free text is allowed; these power datalist suggestions. */
export const SERIES = [
  "History",
  "Movies",
  "Classic Cartoon",
  "Video Games",
  "Politics of Time",
  "10th Planet",
] as const;

export function mergeSuggestions(known: readonly string[], values: Iterable<string>): string[] {
  const out = new Set<string>();
  for (const item of known) out.add(item);
  for (const item of values) {
    const trimmed = item.trim();
    if (trimmed) out.add(trimmed);
  }
  return [...out].sort((a, b) => a.localeCompare(b));
}

export const SET_SLUGS: Record<string, string> = {
  "James The Endless": "endless",
  "10th Planet": "planet",
  Mostorno: "mostorno",
  "Politics of Time": "time",
  "Classic Cartoon": "cartoon",
};

export const COST_FILTERS = ["", "0", "1", "2", "3", "4", "5", "6+"] as const;
export type CostFilter = (typeof COST_FILTERS)[number];

export const SORTS = ["name", "will", "worth", "newest"] as const;
export type SortKey = (typeof SORTS)[number];

export const TYPE_LABEL: Record<CardType, string> = {
  Icon: "Icon",
  Companion: "Companion",
  Relic: "Relic",
  Bond: "Bond",
  Surge: "Surge",
  Will: "Will site",
};

export type Aftereffect = {
  text: string;
  oncePerTurn: boolean;
  costWill: number;
};

export type Ability = {
  name: string;
  timing: Timing;
  costWill: number;
  costHonor: number;
  oncePerTurn: boolean;
  oncePerGame: boolean;
  target: string | null;
  text: string;
};

export type Spell = {
  name: string;
  willCost: number;
  text: string;
};

export type Card = {
  schemaVersion: string;
  keywords: Keyword[];
  hunted: number;
  aftereffect: Aftereffect | null;
  mend: number;
  doubleteam: number;
  startsInPlay: boolean;
  id: string;
  set: string;
  number: string;
  series: string;
  name: string;
  type: CardType;
  subtype: string;
  role: Role | null;
  frame: Frame;
  willCost: number;
  storeWorth: number;
  honorCost: number;
  honorGain: number;
  strike: number;
  guard: number;
  health: number;
  abilities: Ability[];
  spells: Spell[];
  flavor: string;
  artPrompt: string;
  notes: string;
  frontImagePath?: string | null;
  backImagePath?: string | null;
  frontImageUrl?: string | null;
  backImageUrl?: string | null;
  createdAt?: string;
  updatedAt?: string;
};

export type CardSearch = {
  q: string;
  type: string;
  set: string;
  series: string;
  frame: string;
  keyword: string;
  role: string;
  cost: CostFilter;
  sort: SortKey;
};

export const emptySearch = (): CardSearch => ({
  q: "",
  type: "",
  set: "",
  series: "",
  frame: "",
  keyword: "",
  role: "",
  cost: "",
  sort: "name",
});

export const blankAbility = (): Ability => ({
  name: "",
  timing: "static",
  costWill: 0,
  costHonor: 0,
  oncePerTurn: false,
  oncePerGame: false,
  target: null,
  text: "",
});

export const blankSpell = (): Spell => ({
  name: "",
  willCost: 0,
  text: "",
});

export const blankCard = (): Card => ({
  schemaVersion: SCHEMA_VERSION,
  keywords: [],
  hunted: 0,
  aftereffect: null,
  mend: 0,
  doubleteam: 0,
  startsInPlay: false,
  id: "",
  set: "James The Endless",
  number: "",
  series: "",
  name: "",
  type: "Companion",
  subtype: "",
  role: null,
  frame: "standard",
  willCost: 0,
  storeWorth: 0,
  honorCost: 0,
  honorGain: 0,
  strike: 0,
  guard: 0,
  health: 0,
  abilities: [],
  spells: [],
  flavor: "",
  artPrompt: "",
  notes: "",
});

const abilitySchema = z.object({
  name: z.string().trim().max(80),
  timing: z.enum(TIMINGS),
  costWill: z.number().int().min(0).max(8),
  costHonor: z.number().int().min(0).max(20),
  oncePerTurn: z.boolean(),
  oncePerGame: z.boolean(),
  target: z.string().trim().max(40).nullable(),
  text: z.string().max(600),
});

const spellSchema = z.object({
  name: z.string().trim().max(80),
  willCost: z.number().int().min(0).max(8),
  text: z.string().max(600),
});

const aftereffectSchema = z
  .object({
    text: z.string().max(400),
    oncePerTurn: z.boolean(),
    costWill: z.number().int().min(0).max(8),
  })
  .nullable();

export const cardInputSchema = z.object({
  schemaVersion: z.string().min(1).max(8).default(SCHEMA_VERSION),
  keywords: z.array(z.enum(KEYWORDS)).max(9),
  hunted: z.number().int().min(0).max(20),
  aftereffect: aftereffectSchema,
  mend: z.number().int().min(0).max(20),
  doubleteam: z.number().int().min(0).max(6),
  startsInPlay: z.boolean(),
  id: z
    .string()
    .trim()
    .max(40)
    .regex(/^[a-z0-9-]*$/, "Id must be lowercase letters, numbers, and dashes"),
  set: z.string().trim().min(1, "Set is required").max(60),
  number: z.string().trim().max(20),
  series: z.string().trim().max(60),
  name: z.string().trim().min(1, "Name is required").max(80),
  type: z.enum(CARD_TYPES),
  subtype: z.string().trim().max(60),
  role: z.enum(ROLES).nullable(),
  frame: z.enum(FRAMES),
  willCost: z.number().int().min(0).max(8),
  storeWorth: z.number().int().min(0).max(20),
  honorCost: z.number().int().min(0).max(20),
  honorGain: z.number().int().min(0).max(20),
  strike: z.number().int().min(0).max(20),
  guard: z.number().int().min(0).max(20),
  health: z.number().int().min(0).max(30),
  abilities: z.array(abilitySchema).max(12),
  spells: z.array(spellSchema).max(8),
  flavor: z.string().max(300),
  artPrompt: z.string().max(400),
  notes: z.string().max(400),
});

export type CardInput = z.infer<typeof cardInputSchema>;

export function suggestId(set: string, number: string): string {
  const slug =
    SET_SLUGS[set] ??
    (set
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-|-$/g, "")
      .slice(0, 16) ||
      "card");
  const num = (number.split("/")[0] ?? "").replace(/\D/g, "").padStart(2, "0");
  return num === "00" ? slug : `${slug}-${num}`;
}

export function toEngineJson(card: Card): Record<string, unknown> {
  return {
    schemaVersion: card.schemaVersion || SCHEMA_VERSION,
    keywords: card.keywords,
    hunted: card.hunted,
    aftereffect: card.aftereffect,
    mend: card.mend,
    doubleteam: card.doubleteam,
    startsInPlay: card.startsInPlay,
    id: card.id,
    set: card.set,
    number: card.number,
    series: card.series,
    name: card.name,
    type: card.type,
    subtype: card.subtype,
    role: card.role,
    frame: card.frame,
    willCost: card.willCost,
    storeWorth: card.storeWorth,
    honorCost: card.honorCost,
    honorGain: card.honorGain,
    strike: card.strike,
    guard: card.guard,
    health: card.health,
    abilities: card.abilities,
    spells: card.spells,
    flavor: card.flavor,
    artPrompt: card.artPrompt,
    notes: card.notes,
    frontImage: card.frontImagePath ?? null,
    backImage: card.backImagePath ?? null,
  };
}

export function hasCombat(type: CardType): boolean {
  return type === "Icon" || type === "Companion";
}

export function rulesLines(card: Card): string[] {
  const lines: string[] = [];
  for (const ability of card.abilities) {
    const bits = [ability.name, ability.text].filter(Boolean);
    if (bits.length) lines.push(bits.join(" — "));
  }
  for (const spell of card.spells) {
    const cost = spell.willCost ? ` (${spell.willCost} Will)` : "";
    lines.push(`${spell.name}${cost}: ${spell.text}`.trim());
  }
  if (card.aftereffect?.text) {
    const cost = card.aftereffect.costWill
      ? ` · ${card.aftereffect.costWill} Will`
      : "";
    const once = card.aftereffect.oncePerTurn ? " · once per turn" : "";
    lines.push(`Aftereffect${cost}${once}: ${card.aftereffect.text}`);
  }
  if (card.hunted > 0) lines.push(`Hunted ${card.hunted}.`);
  if (card.mend > 0) lines.push(`Mend ${card.mend}.`);
  if (card.doubleteam > 0) lines.push(`Doubleteam ${card.doubleteam}.`);
  if (card.startsInPlay) lines.push("Starts in play.");
  return lines;
}

export function normalizeInput(input: CardInput): CardInput {
  const id = input.id.trim() || suggestId(input.set, input.number);
  const aftereffect =
    input.aftereffect && input.aftereffect.text.trim()
      ? {
          text: input.aftereffect.text.trim(),
          oncePerTurn: input.aftereffect.oncePerTurn,
          costWill: input.aftereffect.costWill,
        }
      : null;
  return {
    ...input,
    schemaVersion: SCHEMA_VERSION,
    id,
    name: input.name.trim(),
    set: input.set.trim(),
    number: input.number.trim(),
    series: input.series.trim(),
    subtype: input.subtype.trim(),
    role: input.type === "Companion" ? input.role : null,
    startsInPlay: input.type === "Icon" ? true : input.startsInPlay,
    flavor: input.flavor.trim(),
    artPrompt: input.artPrompt.trim(),
    notes: input.notes.trim(),
    aftereffect,
    abilities: input.abilities.map((a) => ({
      ...a,
      name: a.name.trim(),
      text: a.text.trim(),
      target: a.target?.trim() || null,
    })),
    spells: input.spells.map((s) => ({
      ...s,
      name: s.name.trim(),
      text: s.text.trim(),
    })),
    keywords: [...new Set(input.keywords)],
  };
}

export function hashString(value: string): number {
  let h = 2166136261;
  for (let i = 0; i < value.length; i += 1) {
    h ^= value.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return h >>> 0;
}

export function toCardInput(card: Card): CardInput {
  return {
    schemaVersion: SCHEMA_VERSION,
    keywords: card.keywords,
    hunted: card.hunted,
    aftereffect: card.aftereffect,
    mend: card.mend,
    doubleteam: card.doubleteam,
    startsInPlay: card.startsInPlay,
    id: card.id,
    set: card.set,
    number: card.number,
    series: card.series,
    name: card.name,
    type: card.type,
    subtype: card.subtype,
    role: card.role,
    frame: card.frame,
    willCost: card.willCost,
    storeWorth: card.storeWorth,
    honorCost: card.honorCost,
    honorGain: card.honorGain,
    strike: card.strike,
    guard: card.guard,
    health: card.health,
    abilities: card.abilities,
    spells: card.spells,
    flavor: card.flavor,
    artPrompt: card.artPrompt,
    notes: card.notes,
  };
}
