# WILLBOUND — Cloudflare Pages + DNS setup
# Run: .\catalog\scripts\setup-cloudflare.ps1

$ErrorActionPreference = "Stop"
$Catalog = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) "catalog"
$Web = Join-Path $Catalog "web"
$EnvFile = Join-Path $Catalog ".env"

function Read-DotEnv([string]$Path) {
  $vars = @{}
  if (-not (Test-Path $Path)) { return $vars }
  Get-Content $Path | ForEach-Object {
    if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
    $k, $v = $_ -split '=', 2
    $vars[$k.Trim()] = $v.Trim()
  }
  return $vars
}

function New-AdminToken {
  $bytes = New-Object byte[] 32
  [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
  return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$webDomain = "willbound.haleappsllc.com"
$apiDomain = "willbound-catalog-api.fly.dev"
$apiDomainCustom = "api.willbound.haleappsllc.com"
$pagesProject = "willbound-catalog"
$apex = "haleappsllc.com"

Write-Host "`n=== WILLBOUND · Cloudflare setup ===" -ForegroundColor Cyan

wrangler whoami 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
  Write-Host "Opening Cloudflare login..."
  wrangler login
}

$local = Read-DotEnv $EnvFile
$adminToken = $local["ADMIN_TOKEN"]
if (-not $adminToken -or $adminToken -eq "change-me-before-sharing") {
  $adminToken = New-AdminToken
  Write-Host "Generated ADMIN_TOKEN — save for API + Pages build." -ForegroundColor Yellow
}

# ── Cloudflare Pages (admin UI) ─────────────────────────────────────────
Push-Location $Web
try {
  npm install | Out-Null
  $env:VITE_API_BASE = "https://$apiDomain"
  # When api.willbound DNS is live, switch to: https://$apiDomainCustom
  $env:VITE_ADMIN_TOKEN = $adminToken
  npm run build

  wrangler pages project create $pagesProject --production-branch main 2>$null
  wrangler pages deploy dist --project-name $pagesProject --branch main --commit-dirty=true

  Write-Host "Pages deployed. Default URL: https://${pagesProject}.pages.dev" -ForegroundColor Green
}
finally {
  Pop-Location
}

# ── Cloudflare DNS (requires API token with Zone:DNS:Edit) ────────────────
if (-not $env:CLOUDFLARE_API_TOKEN) {
  Write-Host "`nSet CLOUDFLARE_API_TOKEN to auto-create DNS records." -ForegroundColor Yellow
  Write-Host "Dashboard → My Profile → API Tokens → Edit zone DNS"
} else {
  node (Join-Path $Catalog "scripts\cloudflare-dns.mjs") `
    --zone $apex `
    --willbound $pagesProject `
    --api-host "REPLACE_WITH_API_HOSTNAME"
}

Write-Host "`n=== DNS records (Cloudflare dashboard) ===" -ForegroundColor Cyan
Write-Host "CNAME  willbound       -> ${pagesProject}.pages.dev   (Proxied)"
Write-Host "CNAME  api.willbound   -> <your-api-host>             (Proxied)"
Write-Host "`nPages custom domain: $webDomain"
Write-Host "Pages build env (Settings -> Environment variables):"
Write-Host "  VITE_API_BASE=https://$apiDomain"
Write-Host "  VITE_ADMIN_TOKEN=$adminToken"
Write-Host "`nAPI env (container host — Fly/Render/Railway):"
Write-Host "  DATABASE_URL=<supabase pooler :6543>"
Write-Host "  PUBLIC_BASE_URL=https://$apiDomain"
Write-Host "  CORS_ORIGIN=https://$webDomain"
Write-Host "  ADMIN_TOKEN=$adminToken"
Write-Host "  DATABASE_SSL=true"
