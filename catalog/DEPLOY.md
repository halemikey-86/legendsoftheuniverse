# Deploy WILLBOUND Catalog — willbound.haleappsllc.com

Your main site **haleappsllc.com** is on **Netlify**. This guide adds:

| Host | What |
| --- | --- |
| **willbound.haleappsllc.com** | Admin UI (React / Vite on Netlify) |
| **api.willbound.haleappsllc.com** | Catalog API (Railway / Render / Fly) |
| **Supabase** | Postgres (already configured) |

---

## 1. Host the API (required for production)

The API cannot run on Netlify alone (Node server + file uploads). Use **Railway** (easiest) or Render.

### Railway

1. [railway.app](https://railway.app) → New Project → Deploy from GitHub
2. Root directory: `catalog`
3. Start command: `npm run api`
4. Variables (from `catalog/.env`):

   | Variable | Value |
   | --- | --- |
   | `DATABASE_URL` | Supabase pooler `:6543` |
   | `PUBLIC_BASE_URL` | `https://api.willbound.haleappsllc.com` |
   | `CORS_ORIGIN` | `https://willbound.haleappsllc.com` |
   | `ADMIN_TOKEN` | Strong secret |
   | `DATABASE_SSL` | `true` |

5. Railway → Settings → Networking → generate domain, then add custom domain `api.willbound.haleappsllc.com`

### Keep API running locally (dev only)

Terminal 1: `cd catalog && npm run api`  
Terminal 2: `cd catalog/web && npm run dev`

---

## 2. Deploy admin UI to Netlify

### Option A — Same Netlify team as haleappsllc.com

1. Netlify → **Add new site** → Import from Git
2. Pick this repo
3. **Base directory:** `catalog/web`
4. **Build command:** `npm run build`
5. **Publish directory:** `catalog/web/dist`
6. **Environment variables:**

   | Key | Value |
   | --- | --- |
   | `VITE_API_BASE` | `https://api.willbound.haleappsllc.com` |
   | `VITE_ADMIN_TOKEN` | Same as API `ADMIN_TOKEN` |

7. **Domain management** → Add custom domain → `willbound.haleappsllc.com`

### Option B — netlify.toml (already in `catalog/web/`)

Netlify reads `netlify.toml` automatically when base dir is `catalog/web`.

---

## 3. DNS (haleappsllc.com)

Wherever DNS is managed (Netlify DNS or registrar):

| Type | Name | Target |
| --- | --- | --- |
| CNAME | `willbound` | `<your-netlify-site>.netlify.app` |
| CNAME | `api.willbound` | `<your-railway-hostname>` |

If using Netlify DNS for the apex domain, add both records in the Netlify domain panel.

---

## 4. Link from main site

On [haleappsllc.com](https://haleappsllc.com), add under your apps list:

```html
<a href="https://willbound.haleappsllc.com">WILLBOUND Card Catalog</a>
```

---

## 5. Local dev workflow

```bash
# Terminal 1 — API + Supabase
cd catalog
npm run api

# Terminal 2 — Admin UI (proxies /api → localhost:8787)
cd catalog/web
cp .env.example .env
npm install
npm run dev
```

Open http://localhost:5173

---

## 6. Export to Unity after editing cards

```bash
cd catalog
npm run export:unity
```

---

## Security notes

- `VITE_ADMIN_TOKEN` is embedded in the built JS — fine for a small admin team; add Netlify password protection or OAuth later for public URLs.
- Never commit `.env` files.
- Rotate Supabase DB password if it was shared in chat.
