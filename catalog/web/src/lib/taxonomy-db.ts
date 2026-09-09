import { mergeSuggestions, SERIES, SETS } from "./card-schema.js";
import type { Sql } from "./cards-db.js";

export type TaxonomyKind = "set" | "series";

export type TaxonomyEntry = {
  id: number;
  kind: TaxonomyKind;
  name: string;
  code: string | null;
  createdAt: string;
};

function asString(value: unknown): string {
  return value == null ? "" : String(value);
}

export async function listTaxonomyEntries(sql: Sql, kind?: TaxonomyKind): Promise<TaxonomyEntry[]> {
  const rows = kind
    ? await sql.query<Record<string, unknown>>(
        `select id, kind, name, code, created_at
         from catalog_taxonomy
         where kind = $1
         order by lower(name)`,
        [kind],
      )
    : await sql.query<Record<string, unknown>>(
        `select id, kind, name, code, created_at
         from catalog_taxonomy
         order by kind, lower(name)`,
      );

  return rows.map((row) => ({
    id: Number(row.id),
    kind: asString(row.kind) as TaxonomyKind,
    name: asString(row.name),
    code: row.code == null ? null : asString(row.code),
    createdAt: asString(row.created_at),
  }));
}

export async function getTaxonomyOptions(sql: Sql): Promise<{ sets: string[]; series: string[] }> {
  const [registered, fromCards] = await Promise.all([
    listTaxonomyEntries(sql),
    sql.query<{ set_name: string; series: string }>(
      `select distinct set_name, series from cards`,
    ),
  ]);

  const registeredSets = registered.filter((e) => e.kind === "set").map((e) => e.name);
  const registeredSeries = registered.filter((e) => e.kind === "series").map((e) => e.name);

  return {
    sets: mergeSuggestions(SETS, registeredSets, fromCards.map((row) => row.set_name)),
    series: mergeSuggestions(SERIES, registeredSeries, fromCards.map((row) => row.series)),
  };
}

export async function addTaxonomyEntry(
  sql: Sql,
  kind: TaxonomyKind,
  name: string,
  code?: string | null,
): Promise<TaxonomyEntry> {
  const trimmed = name.trim();
  if (!trimmed) throw new Error("Name is required");
  if (trimmed.length > 60) throw new Error("Name must be 60 characters or less");

  const rows = await sql.query<Record<string, unknown>>(
    `insert into catalog_taxonomy (kind, name, code)
     values ($1, $2, $3)
     on conflict (kind, name) do update set
       code = coalesce(excluded.code, catalog_taxonomy.code)
     returning id, kind, name, code, created_at`,
    [kind, trimmed, code?.trim() || null],
  );

  const row = rows[0];
  if (!row) throw new Error("Could not save taxonomy entry");

  return {
    id: Number(row.id),
    kind: asString(row.kind) as TaxonomyKind,
    name: asString(row.name),
    code: row.code == null ? null : asString(row.code),
    createdAt: asString(row.created_at),
  };
}

export async function deleteTaxonomyEntry(sql: Sql, id: number): Promise<void> {
  const rows = await sql.query<{ id: number }>(
    `delete from catalog_taxonomy where id = $1 returning id`,
    [id],
  );
  if (rows.length === 0) throw new Error("Entry not found");
}
