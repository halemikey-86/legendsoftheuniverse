/**
 * Postgres pool — Supabase-aware (SSL auto-enabled for *.supabase.com).
 * Copy to db.ts or import createPgPool from here.
 */
import pg from "pg";
import { createPgPool, resolveDatabaseUrl } from "./pg-pool.js";

const pool = createPgPool();

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

export async function getSql(): Promise<Sql> {
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

export { resolveDatabaseUrl };
