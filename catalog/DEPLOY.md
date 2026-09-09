# Deploy WILLBOUND Catalog — Cloudflare + Supabase

| Host | What |
| --- | --- |
| **willbound.haleappsllc.com** | Admin UI (Cloudflare Pages) |
| **api.willbound.haleappsllc.com** | Catalog API (container host, DNS via Cloudflare) |
| **Supabase** | Postgres (already configured) |

DNS for **haleappsllc.com** lives in **Cloudflare**. Pages serves the static admin UI; the Node API runs in a container (Fly.io, Render, or Railway) and is reached through a Cloudflare CNAME.

---

## Quick setup (local CLI)

```powershell
# 1. Log in to Cloudflare
wrangler login

# 2. Deploy admin UI + print DNS/env checklist
.\catalog\scripts\setup-cloudflare.ps1
```

Optional — auto-create DNS records:

```powershell
$env:CLOUDFLARE_API_TOKEN = "your-token"
node catalog/scripts/cloudflare-dns.mjs `
  --zone haleappsllc.com `
  --willbound willbound-catalog `
  --api-host your-api.fly.dev
```

---

## 1. Admin UI — Cloudflare Pages

### Connect GitHub (recommended)

1. [Cloudflare Dashboard](https://dash.cloudflare.com) → **Workers & Pages** → **Create** → **Pages** → Connect to Git
2. Repository: `halemikey-86/legendsoftheuniverse`
3. **Root directory:** `catalog/web`
4. **Build command:** `npm run build`
5. **Build output:** `dist`
6. **Environment variables** (Production):

   | Key | Value |
   | --- | --- |
   | `VITE_API_BASE` | `https://api.willbound.haleappsllc.com` |
   | `VITE_ADMIN_TOKEN` | Same as API `ADMIN_TOKEN` |

7. **Custom domains** → Add `willbound.haleappsllc.com`

### CLI deploy (manual)

```powershell
cd catalog/web
$env:VITE_API_BASE = "https://api.willbound.haleappsllc.com"
$env:VITE_ADMIN_TOKEN = "your-secret"
npm run build
wrangler pages deploy dist --project-name willbound-catalog --branch main
```

SPA routing uses `public/_redirects`.

---

## 2. Catalog API — container host

The API is a Node server (`npm run api`) with Postgres and file uploads. It does not run on Cloudflare Workers without a rewrite. Deploy the Docker image from `catalog/Dockerfile` to any container host.

### Fly.io (example)

```powershell
cd catalog
fly launch --name willbound-catalog-api --no-deploy
fly secrets set DATABASE_URL="..." DATABASE_SSL=true `
  PUBLIC_BASE_URL="https://api.willbound.haleappsllc.com" `
  CORS_ORIGIN="https://willbound.haleappsllc.com" `
  ADMIN_TOKEN="your-secret"
fly deploy
```

Note the Fly hostname (e.g. `willbound-catalog-api.fly.dev`).

### Render / Railway

- Root directory: `catalog`
- Dockerfile: `catalog/Dockerfile`
- Port: `8787` (or set `PORT` env var)
- Same env vars as above

---

## 3. DNS — Cloudflare

In **Cloudflare → haleappsllc.com → DNS**:

| Type | Name | Target | Proxy |
| --- | --- | --- | --- |
| CNAME | `willbound` | `willbound-catalog.pages.dev` | Proxied |
| CNAME | `api.willbound` | `<your-api-host>` | Proxied |

If Pages custom domain is configured in the dashboard, Cloudflare may add the `willbound` record automatically.

---

## 4. Link from main site

On [haleappsllc.com](https://haleappsllc.com):

```html
<a href="https://willbound.haleappsllc.com">WILLBOUND Card Catalog</a>
```

---

## 5. Local dev

```powershell
# Terminal 1 — API + Supabase
cd catalog
npm run api

# Terminal 2 — Admin UI (proxies /api → localhost:8787)
cd catalog/web
npm install
npm run dev
```

Open http://localhost:5173

---

## 6. Export to Unity

```powershell
cd catalog
npm run export:unity
```

---

## Security

- `VITE_ADMIN_TOKEN` is embedded in the built JS — fine for a small admin team.
- Never commit `.env` files.
- Rotate Supabase DB password if it was shared in chat.
- Use Cloudflare Access on `willbound.haleappsllc.com` for extra protection if needed.
