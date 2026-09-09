# WILLBOUND Card Catalog

Postgres-backed admin catalog → **Engine B JSON** → Unity game. Whatever you save in the database can be exported into the game.

## Five phases

| Phase | Status | What |
| --- | --- | --- |
| **1 — Database** | Ready | Supabase Postgres or local Docker; schema, seed, migrations |
| **2 — Interface** | Scaffolded | Admin UI components in `web/`; wire to API (next step) |
| **3 — Card engine** | Ready | Zod schema + `card-validator.ts` blocks bad/duplicate cards |
| **4 — Integrate** | Ready | Export JSON → Unity `StreamingAssets/willbound-cards.json` |
| **5 — Share** | Ready | Supabase project + `.env.example` for colleagues |

## Quick start (Supabase — recommended)

```bash
cd catalog
cp .env.example .env
# Add DATABASE_URL + DATABASE_MIGRATE_URL from Supabase dashboard (see SUPABASE.md)
npm install
npm run db:migrate     # schema + seed cards on Supabase
npm run api
npm run export:unity   # pull cards into Unity
```

Full steps: **[SUPABASE.md](./SUPABASE.md)**

## Quick start (local Docker)

```bash
cd catalog
cp .env.example .env
npm install
npm run db:up          # optional — local Postgres only
npm run db:migrate
npm run export
```

## Admin API (Phase 2/3)

```bash
npm run api
```

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| GET | `/api/health` | — | Health check |
| GET | `/api/cards` | — | List / search catalog |
| GET | `/api/cards/stats` | — | Card count |
| GET | `/api/cards/export` | — | Full Engine B export |
| POST | `/api/cards/validate` | Bearer | Dry-run validation |
| POST | `/api/cards` | Bearer | Create card |
| PUT | `/api/cards/:id` | Bearer | Update card |
| DELETE | `/api/cards/:id` | Bearer | Delete card |

Set `ADMIN_TOKEN` in `.env`. Send `Authorization: Bearer <token>` on writes.

## Validation (Phase 3)

Errors **block save**. Warnings show in the admin UI but allow save.

- Duplicate **id**, **set + number**, or **set + name**
- **Icon** must use **gold** frame; max **3 Icons per set**
- **Role** only on **Companion**; combat stats only on Icon/Companion
- **Keywords** must appear in card text (your “keywords only when text uses one” rule)
- Duplicate ability names on one card
- Will cost ≤ 8; balance warnings for stat-heavy zero-cost cards

DB also enforces unique `(set_name, number)` and type/frame/cost checks.

## Unity integration (Phase 4)

```bash
npm run export:unity
```

Writes `Assets/StreamingAssets/willbound-cards.json` in the Unity project.

Engine B loads it in `WillboundCardCatalog.Load()` via `CatalogJsonLoader` (overrides embedded catalog lines when the file exists).

**Pipeline:** Admin saves card → Postgres → `npm run export:unity` → Unity play mode sees new card.

Optional: add a pre-build step or CI job that exports before Unity builds.

## Share with a colleague (Phase 5)

1. Clone the repo.
2. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
3. `cd catalog && cp .env.example .env`
4. `npm install && npm run db:up && npm run db:migrate && npm run api`
5. Same `.env` values on each machine, or share a team password for `POSTGRES_PASSWORD` and `ADMIN_TOKEN`.

For remote Postgres instead of Docker, set `DATABASE_URL` to your hosted instance.

## Admin UI (Phase 2 — next)

UI components live in `catalog/web/src/components/` (`willbound-app.tsx`, `card-form.tsx`, `game-card.tsx`).

Wire them to this API with Vite + React (instead of TanStack Start) by pointing fetch calls at `http://localhost:8787/api/...`.

## Project layout

```
catalog/
  docker-compose.yml
  sql/0001_schema.sql
  sql/0002_seed.sql
  scripts/migrate.mjs
  scripts/export-engine-json.mjs
  api/server.ts
  web/src/lib/card-schema.ts    # Types + Zod
  web/src/lib/card-validator.ts # Game-breaking guards
  web/src/lib/cards-db.ts       # Postgres CRUD
  web/src/components/           # Admin UI (Phase 2)
```

Unity:

```
Scripts/Rules/EngineJsonParser.cs
Scripts/Rules/CatalogJsonLoader.cs
Scripts/Rules/WillboundCardCatalog.cs  # loads export when present
```

## Card images (JPG / PNG)

Each card can have **front art** and an optional **custom back**. Files are stored on disk under `catalog/uploads/{card-id}/` and referenced in Postgres (`front_image_path`, `back_image_path`).

### Upload API

| Method | Path | Body |
| --- | --- | --- |
| POST | `/api/cards/:id/images/front` | `multipart/form-data` field `file` |
| POST | `/api/cards/:id/images/back` | same |
| DELETE | `/api/cards/:id/images/front` | — |
| DELETE | `/api/cards/:id/images/back` | — |
| GET | `/uploads/{card-id}/front.jpg` | public image |

Rules: **JPEG or PNG only**, max **8 MB**. Save the card first so it has an engine id.

### Admin UI

On the **Face** tab, `CardImageUpload` lets admins pick front/back files after the card is saved.

### Unity export

`npm run export:unity` writes JSON plus copies images to:

- `Assets/StreamingAssets/willbound-cards.json`
- `Assets/Cards/Catalog/{card-id}/front.jpg` (and back if set)

Engine B JSON includes `frontImage` and `backImage` relative paths.

---

See root `README` in the original catalog package or `examples/james-the-endless.json`. Schema version **1.2**.
