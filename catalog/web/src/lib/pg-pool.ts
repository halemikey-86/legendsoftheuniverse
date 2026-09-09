import pg from "pg";

export type PgPoolOptions = {
  connectionString: string;
  ssl?: pg.ConnectionConfig["ssl"];
  max?: number;
};

/** Prefer direct URL for DDL; pooler URL for the API. */
export function resolveDatabaseUrl(forMigrate = false): string {
  if (forMigrate && process.env.DATABASE_MIGRATE_URL?.trim()) {
    return process.env.DATABASE_MIGRATE_URL.trim();
  }
  if (process.env.DATABASE_URL?.trim()) {
    return process.env.DATABASE_URL.trim();
  }
  // Local Docker fallback only when explicitly using docker-compose defaults
  if (process.env.USE_LOCAL_DB === "true") {
    return "postgresql://willbound:willbound_dev@localhost:5432/willbound";
  }
  return "";
}

export function isSupabaseUrl(connectionString: string): boolean {
  return /supabase\.(com|co)/i.test(connectionString);
}

export function resolveSsl(connectionString: string): pg.ConnectionConfig["ssl"] | undefined {
  const flag = process.env.DATABASE_SSL?.toLowerCase();
  if (flag === "false" || flag === "0") return undefined;
  if (flag === "true" || flag === "1") {
    return { rejectUnauthorized: false };
  }

  if (isSupabaseUrl(connectionString)) {
    return { rejectUnauthorized: false };
  }

  if (/sslmode=require/i.test(connectionString)) {
    return { rejectUnauthorized: false };
  }

  return undefined;
}

export function createPgPoolOptions(
  connectionString = resolveDatabaseUrl(false),
): PgPoolOptions {
  return {
    connectionString,
    ssl: resolveSsl(connectionString),
    max: Number(process.env.DATABASE_POOL_MAX ?? 10),
  };
}

export function createPgPool(connectionString?: string): pg.Pool {
  const url = connectionString ?? resolveDatabaseUrl(false);
  if (!url) {
    throw new Error("DATABASE_URL is not set. See catalog/SUPABASE.md");
  }
  const options = createPgPoolOptions(url);
  return new pg.Pool(options);
}
