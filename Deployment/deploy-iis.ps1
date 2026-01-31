<#
.SYNOPSIS
    Deploy Funtime-Shared to IIS (frontend + backend).

.DESCRIPTION
    Stops the app pool, copies frontend dist + published backend,
    restarts the app pool, and runs a health check.
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$SiteName,

    [string]$AppPoolName,

    [string]$FrontendPath = "F:\New_WWW\FuntimeShared\WWW",

    [string]$BackendPath = "F:\New_WWW\FuntimeShared\API",

    [Parameter(Mandatory=$true)]
    [string]$FrontendArtifact,

    [Parameter(Mandatory=$true)]
    [string]$BackendArtifact,

    [string]$BackupRoot = "F:\deploy-backups",

    [string]$HealthCheckUrl = "",

    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"

if (-not $AppPoolName) { $AppPoolName = $SiteName }

Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host "  DEPLOYING: $SiteName" -ForegroundColor Cyan
Write-Host "===========================================================" -ForegroundColor Cyan

Write-Host "[INFO] Frontend path: $FrontendPath"
Write-Host "[INFO] Backend path:  $BackendPath"

# Verify artifacts exist
if (-not (Test-Path $FrontendArtifact)) {
    throw "Frontend artifact not found: $FrontendArtifact"
}
if (-not (Test-Path $BackendArtifact)) {
    throw "Backend artifact not found: $BackendArtifact"
}

# Create target paths if they don't exist
if (-not (Test-Path $FrontendPath)) {
    Write-Host "[INFO] Creating frontend path: $FrontendPath"
    New-Item -ItemType Directory -Path $FrontendPath -Force | Out-Null
}
if (-not (Test-Path $BackendPath)) {
    Write-Host "[INFO] Creating backend path: $BackendPath"
    New-Item -ItemType Directory -Path $BackendPath -Force | Out-Null
}

# -- Backup current deployment --
if (-not $SkipBackup) {
    $timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
    $backupDir = Join-Path $BackupRoot "$SiteName\$timestamp"

    Write-Host "`n[BACKUP] Creating backup at $backupDir" -ForegroundColor Yellow

    New-Item -ItemType Directory -Path "$backupDir\frontend" -Force | Out-Null
    New-Item -ItemType Directory -Path "$backupDir\backend" -Force | Out-Null

    if ((Get-ChildItem $FrontendPath -ErrorAction SilentlyContinue | Measure-Object).Count -gt 0) {
        Get-ChildItem $FrontendPath -Exclude "api" | Copy-Item -Destination "$backupDir\frontend" -Recurse -Force
    }
    if ((Get-ChildItem $BackendPath -ErrorAction SilentlyContinue | Measure-Object).Count -gt 0) {
        Copy-Item -Path "$BackendPath\*" -Destination "$backupDir\backend" -Recurse -Force
    }

    Write-Host "[BACKUP] Done" -ForegroundColor Green

    # Keep only last 5 backups
    $backupSiteDir = Join-Path $BackupRoot $SiteName
    if (Test-Path $backupSiteDir) {
        $allBackups = Get-ChildItem $backupSiteDir -Directory | Sort-Object Name -Descending
        if ($allBackups.Count -gt 5) {
            $allBackups | Select-Object -Skip 5 | ForEach-Object {
                Write-Host "[BACKUP] Removing old backup: $($_.Name)" -ForegroundColor DarkGray
                Remove-Item $_.FullName -Recurse -Force
            }
        }
    }
}

# -- Stop app pool --
Write-Host "`n[DEPLOY] Stopping app pool: $AppPoolName" -ForegroundColor Yellow
$appcmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"

$ErrorActionPreference = "Continue"
& $appcmd stop apppool $AppPoolName 2>&1 | ForEach-Object { Write-Host "  $_" }
Start-Sleep -Seconds 3
Write-Host "[DEPLOY] App pool stop requested" -ForegroundColor Green
$ErrorActionPreference = "Stop"

# -- Deploy frontend --
Write-Host "`n[DEPLOY] Copying frontend..." -ForegroundColor Yellow

Get-ChildItem $FrontendPath -Exclude "api", "web.config", "uploads", "wwwroot" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Copy-Item -Path "$FrontendArtifact\*" -Destination $FrontendPath -Recurse -Force
Write-Host "[DEPLOY] Frontend deployed" -ForegroundColor Green

# -- Deploy backend --
Write-Host "`n[DEPLOY] Copying backend..." -ForegroundColor Yellow

$preserveFiles = @("appsettings.Production.json", "appsettings.production.json", "web.config", "uploads", "wwwroot", "logs")
Get-ChildItem $BackendPath -Exclude $preserveFiles | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Backup preserved files before copy
$preservedBackups = @{}
foreach ($pf in $preserveFiles) {
    $pfPath = Join-Path $BackendPath $pf
    if (Test-Path $pfPath -PathType Leaf) {
        $tempPath = Join-Path $env:TEMP "deploy_preserve_$pf"
        Copy-Item $pfPath $tempPath -Force
        $preservedBackups[$pf] = $tempPath
        Write-Host "[DEPLOY] Preserving $pf"
    }
}

Copy-Item -Path "$BackendArtifact\*" -Destination $BackendPath -Recurse -Force

# Restore preserved files after copy
foreach ($entry in $preservedBackups.GetEnumerator()) {
    $destPath = Join-Path $BackendPath $entry.Key
    Copy-Item $entry.Value $destPath -Force
    Remove-Item $entry.Value -Force
    Write-Host "[DEPLOY] Restored $($entry.Key)"
}
Write-Host "[DEPLOY] Backend deployed" -ForegroundColor Green

# -- Start app pool --
Write-Host "`n[DEPLOY] Starting app pool: $AppPoolName" -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
& $appcmd start apppool $AppPoolName 2>&1 | ForEach-Object { Write-Host "  $_" }
Start-Sleep -Seconds 2
Write-Host "[DEPLOY] App pool start requested" -ForegroundColor Green
$ErrorActionPreference = "Stop"

# -- Health check --
if ($HealthCheckUrl) {
    Write-Host "`n[HEALTH] Checking $HealthCheckUrl..." -ForegroundColor Yellow
    Start-Sleep -Seconds 3

    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $response = Invoke-WebRequest -Uri $HealthCheckUrl -UseBasicParsing -TimeoutSec 15
        if ($response.StatusCode -eq 200) {
            Write-Host "[HEALTH] OK (HTTP $($response.StatusCode))" -ForegroundColor Green
        } else {
            Write-Host "[HEALTH] WARN - Unexpected status: $($response.StatusCode)" -ForegroundColor Red
            exit 1
        }
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401) {
            Write-Host "[HEALTH] OK - API responding (HTTP 401 = auth required, expected)" -ForegroundColor Green
        } else {
            Write-Host "[HEALTH] FAIL - $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    }
}

Write-Host "`n===========================================================" -ForegroundColor Green
Write-Host "  DEPLOYMENT COMPLETE: $SiteName" -ForegroundColor Green
Write-Host "===========================================================" -ForegroundColor Green
