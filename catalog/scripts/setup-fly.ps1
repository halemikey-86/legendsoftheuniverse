# WILLBOUND catalog API — Fly.io deploy
# Run from repo root after: flyctl auth login

$ErrorActionPreference = "Stop"
$Fly = Join-Path $env:USERPROFILE ".fly\bin\flyctl.exe"
if (-not (Test-Path $Fly)) { $Fly = "flyctl" }

$Catalog = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) "catalog"
$EnvFile = Join-Path $Catalog ".env"
$WebDomain = "https://willbound.haleappsllc.com"
$PagesDomain = "https://willbound-catalog.pages.dev"
$ApiDomain = "https://api.willbound.haleappsllc.com"

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

Write-Host "`n=== WILLBOUND API · Fly.io ===" -ForegroundColor Cyan

& $Fly auth whoami | Out-Null
if ($LASTEXITCODE -ne 0) {
  Write-Host "Run: flyctl auth login" -ForegroundColor Yellow
  exit 1
}

$local = Read-DotEnv $EnvFile
$dbUrl = $local["DATABASE_URL"]
if (-not $dbUrl) { throw "DATABASE_URL missing in catalog/.env" }

$adminToken = $local["ADMIN_TOKEN"]
if (-not $adminToken -or $adminToken -eq "change-me-before-sharing") {
  $bytes = New-Object byte[] 32
  [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
  $adminToken = [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
  Write-Host "Generated ADMIN_TOKEN — update Cloudflare Pages VITE_ADMIN_TOKEN to match." -ForegroundColor Yellow
}

Push-Location $Catalog
try {
  & $Fly apps list --json 2>$null | Out-Null
  $appExists = (& $Fly apps list --json | ConvertFrom-Json) | Where-Object { $_.Name -eq "willbound-catalog-api" }

  if (-not $appExists) {
    Write-Host "Creating app willbound-catalog-api..."
    & $Fly apps create willbound-catalog-api --org personal
  }

  $volList = & $Fly volumes list -a willbound-catalog-api --json 2>$null | ConvertFrom-Json
  if (-not $volList -or $volList.Count -eq 0) {
    Write-Host "Creating upload volume in iad..."
    & $Fly volumes create willbound_uploads --region iad --size 1 -a willbound-catalog-api -y
  }

  & $Fly secrets set `
    "DATABASE_URL=$dbUrl" `
    "PUBLIC_BASE_URL=$ApiDomain" `
    "CORS_ORIGIN=$WebDomain,$PagesDomain" `
    "ADMIN_TOKEN=$adminToken" `
    -a willbound-catalog-api

  & $Fly deploy -a willbound-catalog-api

  Write-Host "`nAdding custom domain (if zone is on Cloudflare or Fly DNS)..." -ForegroundColor Cyan
  & $Fly certs add api.willbound.haleappsllc.com -a willbound-catalog-api 2>$null

  $hostname = (& $Fly info -a willbound-catalog-api --json | ConvertFrom-Json).Hostname
  Write-Host "`n=== Done ===" -ForegroundColor Green
  Write-Host "API URL: https://$hostname"
  Write-Host "Custom:  $ApiDomain (after DNS CNAME)"
  Write-Host "`nDNS (Cloudflare or Namecheap):"
  Write-Host "  CNAME  api.willbound  ->  $hostname"
  Write-Host "`nCloudflare Pages env (must match):"
  Write-Host "  VITE_API_BASE=$ApiDomain"
  Write-Host "  VITE_ADMIN_TOKEN=$adminToken"
}
finally {
  Pop-Location
}
