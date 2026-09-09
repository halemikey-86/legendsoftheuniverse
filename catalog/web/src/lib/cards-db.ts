import pg from "pg";
import {
  CARD_TYPES,
  COST_FILTERS,
  FRAMES,
  KEYWORDS,
  ROLES,
  SCHEMA_VERSION,
  SORTS,
  cardInputSchema,
  normalizeInput,
  toEngineJson,
  type Card,
  type CardInput,
  type CardSearch,
  type CardType,
  type CostFilter,
  type Frame,
  type Keyword,
  type Role,
  type SortKey,
} from "./card-schema.js";
import {
  assertCardValid,
  validateCardRules,
  type ValidationIssue,
} from "./card-validator.js";
import {
  deleteAllCardImages,
  imagePublicUrl,
  type ImageSlot,
} from "./image-storage.js";
import { createPgPool, resolveDatabaseUrl } from "./pg-pool.js";

export type Sql = {
  <T = Record<string, unknown>>(
    strings: TemplateStringsArray,
    ...values: unknown[]
  ): Promise<T[]>;
  query<T = Record<string, unknown>>(
    text: string,
    params?: unknown[],
  ): Promise<T[]>;
};

export function createPool(connectionString?: string): pg.Pool {
  return createPgPool(connectionString ?? resolveDatabaseUrl(false));
}

export async function createSql(connectionString: string): Promise<Sql> {
  const pool = createPool(connectionString);
  const query = async <T>(text: string, params: unknown[] = []) => {
    const result = await pool.query<T>(text, params);
    return result.rows;
  };
  const sql = (async <T = Record<string, unknown>>(
    strings: TemplateStringsArray,
    ...values: unknown[]
  ): Promise<T[]> => {
    let text = strings[0] ?? "";
    for (let i = 0; i < values.length; i += 1) {
      text += `$${i + 1}${strings[i + 1] ?? ""}`;
    }
    return query<T>(text, values);
  }) as Sql;
  sql.query = query;
  return sql;
}

const CARD_COLUMNS = `
  id, schema_version, set_name, number, series, name, type, subtype, role, frame,
  will_cost, store_worth, honor_cost, honor_gain, strike, guard, health,
  keywords, hunted, aftereffect, mend, doubleteam, starts_in_play,
  abilities, spells, flavor, art_prompt, notes,
  front_image_path, back_image_path,
  created_at::text as created_at, updated_at::text as updated_at
`;

export function attachImageUrls(cards: Card[], publicBaseUrl: string): Card[] {
  return cards.map((card) => ({
    ...card,
    frontImageUrl: imagePublicUrl(card.frontImagePath, publicBaseUrl),
    backImageUrl: imagePublicUrl(card.backImagePath, publicBaseUrl),
  }));
}

function asString(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

function parseJson<T>(value: unknown, fallback: T): T {
  if (value == null) return fallback;
  if (typeof value === "string") {
    try {
      return JSON.parse(value) as T;
    } catch {
      return fallback;
    }
  }
  return value as T;
}

function asInt(value: unknown, fallback = 0): number {
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? n : fallback;
}

function asBool(value: unknown): boolean {
  return value === true || value === "t" || value === "true";
}

export function mapRow(row: Record<string, unknown>): Card {
  const typeRaw = asString(row.type, "Companion");
  const type = (CARD_TYPES as readonly string[]).includes(typeRaw)
    ? (typeRaw as CardType)
    : "Companion";
  const frameRaw = asString(row.frame, "standard");
  const frame = (FRAMES as readonly string[]).includes(frameRaw)
    ? (frameRaw as Frame)
    : "standard";
  const roleRaw = row.role == null ? null : asString(row.role);
  const role =
    roleRaw && (ROLES as readonly string[]).includes(roleRaw)
      ? (roleRaw as Role)
      : null;
  const keywords = parseJson<unknown[]>(row.keywords, []).filter((k): k is Keyword =>
    (KEYWORDS as readonly string[]).includes(String(k)),
  );
  const aftereffect = parseJson<{ text?: string; oncePerTurn?: boolean; costWill?: number } | null>(
    row.aftereffect,
    null,
  );
  return {
    schemaVersion: asString(row.schema_version, SCHEMA_VERSION),
    keywords,
    hunted: asInt(row.hunted),
    aftereffect:
      aftereffect && typeof aftereffect === "object" && aftereffect.text
        ? {
            text: String(aftereffect.text ?? ""),
            oncePerTurn: Boolean(aftereffect.oncePerTurn),
            costWill: asInt(aftereffect.costWill),
          }
        : null,
    mend: asInt(row.mend),
    doubleteam: asInt(row.doubleteam),
    startsInPlay: asBool(row.starts_in_play),
    id: asString(row.id),
    set: asString(row.set_name),
    number: asString(row.number),
    series: asString(row.series),
    name: asString(row.name),
    type,
    subtype: asString(row.subtype),
    role,
    frame,
    willCost: asInt(row.will_cost),
    storeWorth: asInt(row.store_worth),
    honorCost: asInt(row.honor_cost),
    honorGain: asInt(row.honor_gain),
    strike: asInt(row.strike),
    guard: asInt(row.guard),
    health: asInt(row.health),
    abilities: parseJson(row.abilities, []),
    spells: parseJson(row.spells, []),
    flavor: asString(row.flavor),
    artPrompt: asString(row.art_prompt),
    notes: asString(row.notes),
    frontImagePath: row.front_image_path == null ? null : asString(row.front_image_path),
    backImagePath: row.back_image_path == null ? null : asString(row.back_image_path),
    createdAt: asString(row.created_at) || undefined,
    updatedAt: asString(row.updated_at) || undefined,
  };
}

function bindParams(input: CardInput): unknown[] {
  return [
    input.id,
    SCHEMA_VERSION,
    input.set,
    input.number,
    input.series,
    input.name,
    input.type,
    input.subtype,
    input.role,
    input.frame,
    input.willCost,
    input.storeWorth,
    input.honorCost,
    input.honorGain,
    input.strike,
    input.guard,
    input.health,
    JSON.stringify(input.keywords),
    input.hunted,
    input.aftereffect ? JSON.stringify(input.aftereffect) : null,
    input.mend,
    input.doubleteam,
    input.startsInPlay,
    JSON.stringify(input.abilities),
    JSON.stringify(input.spells),
    input.flavor,
    input.artPrompt,
    input.notes,
  ];
}

function orderBy(sort: SortKey): string {
  switch (sort) {
    case "will":
      return "will_cost asc, name asc";
    case "worth":
      return "store_worth asc, name asc";
    case "newest":
      return "created_at desc, id desc";
    default:
      return "name asc";
  }
}

function likeNeedle(q: string): string | null {
  const trimmed = q.trim().replace(/[%_]/g, "");
  if (!trimmed) return null;
  return `%${trimmed}%`;
}

export function parseSearch(data: unknown): CardSearch {
  const raw = (data ?? {}) as Record<string, unknown>;
  const sort = asString(raw.sort, "name");
  const cost = asString(raw.cost, "");
  return {
    q: asString(raw.q),
    type: asString(raw.type),
    set: asString(raw.set),
    series: asString(raw.series),
    frame: asString(raw.frame),
    keyword: asString(raw.keyword),
    role: asString(raw.role),
    cost: (COST_FILTERS as readonly string[]).includes(cost)
      ? (cost as CostFilter)
      : "",
    sort: (SORTS as readonly string[]).includes(sort) ? (sort as SortKey) : "name",
  };
}

async function audit(
  sql: Sql,
  cardId: string,
  action: string,
  payload: unknown,
  actor = "admin",
) {
  await sql.query(
    "insert into card_audit (card_id, action, actor, payload) values ($1, $2, $3, $4::jsonb)",
    [cardId, action, actor, JSON.stringify(payload)],
  );
}

export async function getCardById(sql: Sql, id: string): Promise<Card | null> {
  const rows = await sql.query<Record<string, unknown>>(
    `select ${CARD_COLUMNS} from cards where id = $1`,
    [id],
  );
  const row = rows[0];
  return row ? mapRow(row) : null;
}

export async function setCardImagePath(
  sql: Sql,
  cardId: string,
  slot: ImageSlot,
  relativePath: string,
  actor = "admin",
): Promise<Card> {
  const column = slot === "front" ? "front_image_path" : "back_image_path";
  const updated = await sql.query<Record<string, unknown>>(
    `update cards set ${column} = $1, updated_at = now() where id = $2 returning ${CARD_COLUMNS}`,
    [relativePath, cardId],
  );
  const row = updated[0];
  if (!row) throw new Error("Card not found");
  await audit(sql, cardId, `image_${slot}`, { path: relativePath }, actor);
  return mapRow(row);
}

export async function clearCardImagePath(
  sql: Sql,
  cardId: string,
  slot: ImageSlot,
  actor = "admin",
): Promise<Card> {
  const column = slot === "front" ? "front_image_path" : "back_image_path";
  const updated = await sql.query<Record<string, unknown>>(
    `update cards set ${column} = null, updated_at = now() where id = $1 returning ${CARD_COLUMNS}`,
    [cardId],
  );
  const row = updated[0];
  if (!row) throw new Error("Card not found");
  await audit(sql, cardId, `image_${slot}_clear`, {}, actor);
  return mapRow(row);
}

export async function listAllCards(sql: Sql): Promise<Card[]> {
  const rows = await sql.query<Record<string, unknown>>(
    `select ${CARD_COLUMNS} from cards order by set_name, number, name`,
  );
  return rows.map(mapRow);
}

export async function searchCards(sql: Sql, data: CardSearch): Promise<Card[]> {
  const clauses: string[] = [];
  const params: unknown[] = [];
  let i = 1;

  const like = likeNeedle(data.q);
  if (like) {
    clauses.push(
      `(name ilike $${i} or id ilike $${i} or subtype ilike $${i} or flavor ilike $${i} or notes ilike $${i} or set_name ilike $${i} or series ilike $${i} or abilities::text ilike $${i} or spells::text ilike $${i})`,
    );
    params.push(like);
    i += 1;
  }
  if (data.type && (CARD_TYPES as readonly string[]).includes(data.type)) {
    clauses.push(`type = $${i}`);
    params.push(data.type);
    i += 1;
  }
  if (data.set) {
    clauses.push(`set_name = $${i}`);
    params.push(data.set);
    i += 1;
  }
  if (data.series) {
    clauses.push(`series = $${i}`);
    params.push(data.series);
    i += 1;
  }
  if (data.frame && (FRAMES as readonly string[]).includes(data.frame)) {
    clauses.push(`frame = $${i}`);
    params.push(data.frame);
    i += 1;
  }
  if (data.keyword && (KEYWORDS as readonly string[]).includes(data.keyword)) {
    clauses.push(`keywords @> $${i}::jsonb`);
    params.push(JSON.stringify([data.keyword]));
    i += 1;
  }
  if (data.role && (ROLES as readonly string[]).includes(data.role)) {
    clauses.push(`role = $${i}`);
    params.push(data.role);
    i += 1;
  }
  if (data.cost === "6+") {
    clauses.push(`will_cost >= $${i}`);
    params.push(6);
    i += 1;
  } else if (data.cost !== "") {
    const n = Number(data.cost);
    if (Number.isInteger(n)) {
      clauses.push(`will_cost = $${i}`);
      params.push(n);
      i += 1;
    }
  }

  const where = clauses.length ? `where ${clauses.join(" and ")}` : "";
  const rows = await sql.query<Record<string, unknown>>(
    `select ${CARD_COLUMNS} from cards ${where} order by ${orderBy(data.sort)}`,
    params,
  );
  return rows.map(mapRow);
}

export async function getLibraryStats(sql: Sql) {
  const rows = await sql<{ count: number }>`select count(*)::int as count from cards`;
  return { total: rows[0]?.count ?? 0 };
}

export type SaveResult = {
  card: Card;
  warnings: ValidationIssue[];
};

export async function createCard(sql: Sql, raw: unknown, actor = "admin"): Promise<SaveResult> {
  const parsed = normalizeInput(cardInputSchema.parse(raw));
  const existing = await listAllCards(sql);
  const warnings = assertCardValid(parsed, { existingCards: existing });

  const inserted = await sql.query<Record<string, unknown>>(
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
     returning ${CARD_COLUMNS}`,
    bindParams(parsed),
  );
  const row = inserted[0];
  if (!row) throw new Error("Could not create card");
  await audit(sql, parsed.id, "create", toEngineJson(mapRow(row)), actor);
  return { card: mapRow(row), warnings };
}

export async function updateCard(
  sql: Sql,
  raw: unknown,
  originalId: string,
  actor = "admin",
): Promise<SaveResult> {
  const parsed = normalizeInput(cardInputSchema.parse(raw));
  const existing = await listAllCards(sql);
  const warnings = assertCardValid(parsed, {
    existingCards: existing,
    originalId,
  });

  const params = [...bindParams(parsed), originalId];
  const updated = await sql.query<Record<string, unknown>>(
    `update cards set
       id = $1, schema_version = $2, set_name = $3, number = $4, series = $5,
       name = $6, type = $7, subtype = $8, role = $9, frame = $10,
       will_cost = $11, store_worth = $12, honor_cost = $13, honor_gain = $14,
       strike = $15, guard = $16, health = $17,
       keywords = $18::jsonb, hunted = $19, aftereffect = $20::jsonb,
       mend = $21, doubleteam = $22, starts_in_play = $23,
       abilities = $24::jsonb, spells = $25::jsonb,
       flavor = $26, art_prompt = $27, notes = $28, updated_at = now()
     where id = $29
     returning ${CARD_COLUMNS}`,
    params,
  );
  const row = updated[0];
  if (!row) throw new Error("Card not found");
  await audit(sql, parsed.id, "update", { originalId, card: toEngineJson(mapRow(row)) }, actor);
  return { card: mapRow(row), warnings };
}

export async function deleteCard(sql: Sql, id: string, actor = "admin") {
  const rows = await sql.query<{ id: string }>(
    "delete from cards where id = $1 returning id",
    [id],
  );
  if (!rows[0]) throw new Error("Card not found");
  deleteAllCardImages(id);
  await audit(sql, id, "delete", { id }, actor);
  return { ok: true as const };
}

export async function validateOnly(sql: Sql, raw: unknown, originalId?: string) {
  const parsed = normalizeInput(cardInputSchema.parse(raw));
  const existing = await listAllCards(sql);
  return validateCardRules(parsed, { existingCards: existing, originalId });
}

export async function exportEngineCatalog(sql: Sql) {
  const cards = await listAllCards(sql);
  return {
    schemaVersion: SCHEMA_VERSION,
    exportedAt: new Date().toISOString(),
    cards: cards.map(toEngineJson),
  };
}
