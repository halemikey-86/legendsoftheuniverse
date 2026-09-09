# Deploy WILLBOUND Catalog — Cloudflare + Supabase

| Host | What |
| --- | --- |
| **willbound.haleappsllc.com** | Admin UI (Cloudflare Pages) |
| **api.willbound.haleappsllc.com** | Catalog API (Fly.io) |
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
6. **Environment variables** — set for **Production and Preview** (branch `.pages.dev` links use Preview):

   | Key | Value |
   | --- | --- |
   | `VITE_API_BASE` | `https://willbound-catalog-api.fly.dev` (required — never use `localhost` for Production/Preview) |

   The UI also falls back to `willbound-catalog-api.fly.dev` when hosted on `*.pages.dev` or `willbound.haleappsllc.com`, but set this env var anyway so builds are explicit.
   | `VITE_ADMIN_TOKEN` | Same as API `ADMIN_TOKEN` (optional — teammates can paste via **Admin access**) |

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

## 2. Catalog API — Fly.io

The API is a Node server with Postgres and file uploads. `catalog/fly.toml` and `catalog/Dockerfile` are ready.

```powershell
# From repo root (legendsoftheuniverse/)
& "$env:USERPROFILE\.fly\bin\flyctl.exe" auth login
.\catalog\scripts\setup-fly.ps1

# Or from catalog/
& "$env:USERPROFILE\.fly\bin\flyctl.exe" auth login
.\scripts\setup-fly.ps1
```

Or manually:

```powershell
cd catalog
flyctl apps create willbound-catalog-api
flyctl volumes create willbound_uploads --region iad --size 1 -a willbound-catalog-api
flyctl secrets set DATABASE_URL="..." PUBLIC_BASE_URL="https://api.willbound.haleappsllc.com" `
  CORS_ORIGIN="https://willbound.haleappsllc.com,https://willbound-catalog.pages.dev" `
  ADMIN_TOKEN="your-secret" -a willbound-catalog-api
flyctl deploy
flyctl certs add api.willbound.haleappsllc.com -a willbound-catalog-api
```

Note the Fly hostname (e.g. `willbound-catalog-api.fly.dev`) for DNS.

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
