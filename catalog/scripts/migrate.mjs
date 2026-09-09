#!/usr/bin/env node
/**
 * Apply SQL migrations in catalog/sql/ in filename order.
 * Works with local Postgres or Supabase (set DATABASE_URL in .env).
 *
 * For Supabase migrations, prefer DATABASE_MIGRATE_URL = direct connection (port 5432).
 */
import fs from "node:fs";
import path from "node:path";
import { root } from "./load-env.mjs";
import { createPgPool, resolveDatabaseUrl } from "./pg-config.mjs";

const sqlDir = path.join(root, "sql");

const files = fs
  .readdirSync(sqlDir)
  .filter((f) => f.endsWith(".sql"))
  .sort();

if (files.length === 0) {
  console.error("No SQL files in catalog/sql");
  process.exit(1);
}

const dbUrl = resolveDatabaseUrl(true);

if (!dbUrl) {
  console.error(`
Migration failed: no database URL configured.

Add your Supabase connection strings to catalog/.env:

  DATABASE_MIGRATE_URL=postgresql://...supabase.com:5432/postgres   (Direct — for migrations)
  DATABASE_URL=postgresql://...pooler.supabase.com:6543/postgres    (Pooler — for API)

See SUPABASE.md → Project Settings → Database → Connection string.

Or for local Docker: npm run db:up  then set
  USE_LOCAL_DB=true
  DATABASE_URL=postgresql://willbound:willbound_dev@localhost:5432/willbound
`);
  process.exit(1);
}

const pool = createPgPool(true);
console.log(`Connecting to ${/supabase/i.test(dbUrl) ? "Supabase" : "Postgres"}…`);

try {
  for (const file of files) {
    const full = path.join(sqlDir, file);
    const sql = fs.readFileSync(full, "utf8");
    console.log(`→ ${file}`);
    await pool.query(sql);
  }
  const { rows } = await pool.query("select count(*)::int as count from cards");
  console.log(`Done. cards table has ${rows[0]?.count ?? 0} row(s).`);
} catch (err) {
  const message = formatPgError(err);
  console.error("Migration failed:", message);
  if (message.includes("ECONNREFUSED") || message.includes("connect")) {
    console.error("\nTip: Is Postgres running? For Supabase, fill DATABASE_MIGRATE_URL in .env (see SUPABASE.md).");
  }
  process.exit(1);
} finally {
  await pool.end();
}

function formatPgError(err) {
  if (!err) return "Unknown error";
  if (err instanceof AggregateError && err.errors?.length) {
    return err.errors.map((e) => e?.message || e?.code || String(e)).join("; ");
  }
  return err.message || err.code || String(err);
}
