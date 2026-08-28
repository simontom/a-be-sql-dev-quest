# ============================================================
# Local Test Runner for Banner Campaign DB (PowerShell)
# Runs all SQL scripts and outputs analytical query results.
# ============================================================

[CmdletBinding()]
param (
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$Database = "BanneryDB",
    [switch]$UseSqlAuth,
    [string]$Username = "sa",
    [string]$Password = "BanneryQuest#2026"
)

$ErrorActionPreference = "Stop"

# Set console to UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Banner Campaign DB - Local Test Suite" -ForegroundColor Cyan
Write-Host " Server: $ServerInstance | DB: $Database" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# If using LocalDB, make sure the instance is started
if ($ServerInstance -like "*(localdb)*") {
    $instanceName = $ServerInstance.Replace("(localdb)\", "")
    Write-Host "==> Ensuring LocalDB instance '$instanceName' is running..." -ForegroundColor Gray
    try {
        sqllocaldb start $instanceName | Out-Null
    } catch {
        Write-Warning "Could not start LocalDB automatically. Proceeding anyway..."
    }
}

# Build common sqlcmd arguments
$baseArgs = @("-S", $ServerInstance, "-b", "-f", "65001", "-I")
if ($UseSqlAuth) {
    $baseArgs += @("-U", $Username, "-P", $Password)
} else {
    $baseArgs += @("-E")
}

function Invoke-SqlScript {
    param (
        [string]$Label,
        [string]$FilePath,
        [string]$TargetDb = $Database
    )
    Write-Host ""
    Write-Host "==> [$Label] $FilePath ..." -ForegroundColor Yellow
    & sqlcmd @baseArgs -d $TargetDb -i $FilePath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: $FilePath failed with exit code $LASTEXITCODE." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Step 1: Create / Recreate Database
Write-Host ""
Write-Host "==> Creating / Resetting database '$Database'..." -ForegroundColor Yellow
$createDbSql = "IF EXISTS (SELECT name FROM sys.databases WHERE name = '$Database') BEGIN ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database]; END; CREATE DATABASE [$Database];"

& sqlcmd @baseArgs -Q $createDbSql
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create database '$Database'." -ForegroundColor Red
    exit $LASTEXITCODE
}

# Step 2: Run Schema, Constraints, Indexes
Invoke-SqlScript "1/6 Schema"      "01-schema.sql"
Invoke-SqlScript "2/6 Constraints" "02-constraints.sql"
Invoke-SqlScript "3/6 Indexes"     "03-indexes.sql"

# Step 3: Run Seed Data
Invoke-SqlScript "4/6 Seed Data"   "docker-init\07-seed-data.sql"

# Step 4: Run Analytical Queries
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " [5/6] Query b: Campaign Balance (04-query-campaign-balance.sql)" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
& sqlcmd @baseArgs -d $Database -i "04-query-campaign-balance.sql"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " [6/6] Query c: Daily Balance for FIX Banners (05-query-daily-balance.sql)" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
& sqlcmd @baseArgs -d $Database -i "05-query-daily-balance.sql"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " ALL TESTS PASSED SUCCESSFULLY! Database is ready." -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
