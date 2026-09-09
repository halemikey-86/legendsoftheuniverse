# Supabase setup — WILLBOUND Card Catalog

Use Supabase as hosted Postgres. Card **images** still live on disk (or migrate to Supabase Storage later); the database holds card rules JSON and image **paths**.

## 1. Create a Supabase project

1. Go to [supabase.com](https://supabase.com) → **New project**
2. Pick a region close to you and set a strong **database password** (save it)

## 2. Get connection strings

**Project → Settings → Database → Connection string**

You need two URIs (replace `[PASSWORD]` with your DB password):

| Use | Supabase tab | Port | Env var |
| --- | --- | --- | --- |
| **Migrations** (`npm run db:migrate`) | **Direct connection** | 5432 | `DATABASE_MIGRATE_URL` |
| **API / export** (`npm run api`) | **Transaction pooler** | 6543 | `DATABASE_URL` |

Example (shape only — copy yours from the dashboard):

```env
DATABASE_MIGRATE_URL=postgresql://postgres.xxxxxxxxxxxx:[PASSWORD]@aws-0-us-east-1.pooler.supabase.com:5432/postgres
DATABASE_URL=postgresql://postgres.xxxxxxxxxxxx:[PASSWORD]@aws-0-us-east-1.pooler.supabase.com:6543/postgres
```

SSL is turned on automatically when the host contains `supabase.com`.

## 3. Configure `.env`

```bash
cd catalog
cp .env.example .env
# Paste both Supabase URIs into .env
npm install
```

## 4. Run migrations

**Option A — CLI (recommended)**

```bash
npm run db:migrate
```

Applies `sql/0001_schema.sql`, `0002_seed.sql`, `0003_card_images.sql` in order.

**Option B — Supabase SQL Editor**

1. **SQL → New query**
2. Paste and run each file from `catalog/sql/` in order (0001 → 0002 → 0003)

## 5. Run the API

```bash
npm run api
```

Test:

```bash
curl http://localhost:8787/api/cards/stats
curl http://localhost:8787/api/cards
```

## 6. Export to Unity

```bash
npm run export:unity
```

Reads cards from Supabase, writes JSON + copies local upload images.

---

## Sharing with a colleague

1. Share the repo (not `.env`)
2. They create their own Supabase project **or** you invite them to yours:
   - **Project → Settings → Team**
3. Each person copies connection strings into their own `.env`
4. Same `ADMIN_TOKEN` if you want shared write access to the API

---

## Troubleshooting

| Error | Fix |
| --- | --- |
| `ECONNREFUSED` | Wrong host/port; use pooler 6543 for API, direct 5432 for migrate |
| SSL / certificate errors | Set `DATABASE_SSL=true` (auto for Supabase) |
| Migration timeout on pooler | Use `DATABASE_MIGRATE_URL` (direct), not the 6543 pooler |
| `relation "cards" does not exist` | Run `npm run db:migrate` |
| Password special chars break URL | URL-encode the password in the connection string |

---

## Optional: Supabase Storage for images

Today, JPG/PNG uploads go to `catalog/uploads/` on the machine running the API. For a fully hosted setup:

1. Create a **Storage** bucket (e.g. `card-art`)
2. Upload files via Supabase SDK
3. Store public URLs in `front_image_path` / `back_image_path`

That is a follow-up; the schema already stores paths as text so URLs work without a migration.

## Optional: Row Level Security

The catalog API uses the **service role** connection string with full DB access. Do not expose `DATABASE_URL` in the browser. For a public Supabase client later, add RLS policies and use the anon key only for read-only views.
