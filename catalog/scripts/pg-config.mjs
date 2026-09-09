/**
 * Shared Postgres pool config for scripts (migrate, export).
 * Supabase requires SSL — set DATABASE_URL to your Supabase URI.
 */
import pg from "pg";

export function resolveDatabaseUrl(forMigrate = false) {
  if (forMigrate && process.env.DATABASE_MIGRATE_URL?.trim()) {
    return process.env.DATABASE_MIGRATE_URL.trim();
  }
  if (process.env.DATABASE_URL?.trim()) {
    return process.env.DATABASE_URL.trim();
  }
  if (process.env.USE_LOCAL_DB === "true") {
    return "postgresql://willbound:willbound_dev@localhost:5432/willbound";
  }
  return "";
}

export function resolveSsl(connectionString) {
  const flag = process.env.DATABASE_SSL?.toLowerCase();
  if (flag === "false" || flag === "0") return undefined;
  if (flag === "true" || flag === "1") return { rejectUnauthorized: false };
  if (/supabase\.(com|co)/i.test(connectionString)) return { rejectUnauthorized: false };
  if (/sslmode=require/i.test(connectionString)) return { rejectUnauthorized: false };
  return undefined;
}

export function createPgPool(forMigrate = false) {
  const connectionString = resolveDatabaseUrl(forMigrate);
  if (!connectionString) {
    throw new Error("DATABASE_URL is not set");
  }
  return new pg.Pool({
    connectionString,
    ssl: resolveSsl(connectionString),
    max: Number(process.env.DATABASE_POOL_MAX ?? 10),
  });
}
