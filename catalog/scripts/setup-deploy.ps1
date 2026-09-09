# WILLBOUND catalog — Netlify + Railway one-time setup
# Run from repo root:  .\catalog\scripts\setup-deploy.ps1

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Catalog = Join-Path $RepoRoot "catalog"
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

Write-Host "`n=== WILLBOUND deploy setup ===" -ForegroundColor Cyan

$local = Read-DotEnv $EnvFile
$adminToken = $local["ADMIN_TOKEN"]
if (-not $adminToken -or $adminToken -eq "change-me-before-sharing") {
  $adminToken = New-AdminToken
  Write-Host "Generated new ADMIN_TOKEN (save this for Netlify + Railway)." -ForegroundColor Yellow
}

$apiDomain = "api.willbound.haleappsllc.com"
$webDomain = "willbound.haleappsllc.com"
$databaseUrl = $local["DATABASE_URL"]
if (-not $databaseUrl) {
  throw "DATABASE_URL missing in catalog/.env"
}

# ── Railway ─────────────────────────────────────────────────────────────
Write-Host "`n--- Railway (API) ---" -ForegroundColor Green
railway whoami 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
  Write-Host "Opening Railway login..."
  railway login
}

Push-Location $Catalog
try {
  if (-not (Test-Path ".railway")) {
    railway init --name "willbound-catalog-api"
  }

  railway variables set `
    "DATABASE_URL=$databaseUrl" `
    "DATABASE_SSL=true" `
    "PUBLIC_BASE_URL=https://$apiDomain" `
    "CORS_ORIGIN=https://$webDomain" `
    "ADMIN_TOKEN=$adminToken" `
    "UPLOAD_DIR=/tmp/uploads"

  railway up --detach
  Write-Host "Railway deploy triggered. Add custom domain in dashboard: $apiDomain" -ForegroundColor Yellow
}
finally {
  Pop-Location
}

# ── Netlify ─────────────────────────────────────────────────────────────
Write-Host "`n--- Netlify (Admin UI) ---" -ForegroundColor Green
netlify status 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
  Write-Host "Opening Netlify login..."
  netlify login
}

Push-Location $Web
try {
  if (-not (Test-Path ".netlify/state.json")) {
    netlify sites:create --name "willbound-catalog" --account-slug ""
  }

  netlify env:set VITE_API_BASE "https://$apiDomain"
  netlify env:set VITE_ADMIN_TOKEN $adminToken

  netlify deploy --build --prod

  Write-Host "Add custom domain in Netlify: $webDomain" -ForegroundColor Yellow
}
finally {
  Pop-Location
}

Write-Host "`n=== DNS (haleappsllc.com) ===" -ForegroundColor Cyan
Write-Host "CNAME  willbound      -> <your-netlify-site>.netlify.app"
Write-Host "CNAME  api.willbound  -> <your-railway-hostname>"

Write-Host "`n=== Secrets summary (store safely) ===" -ForegroundColor Cyan
Write-Host "ADMIN_TOKEN=$adminToken"
Write-Host "VITE_API_BASE=https://$apiDomain"
