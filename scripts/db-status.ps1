# Timekeeper Database Status Checker
# This script shows the current status of the database and migrations

param(
    [switch]$Detailed,
    [switch]$ShowSchema
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Timekeeper Database Status" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$CliProject = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj"
$InfraProject = Join-Path $ProjectRoot "src\Infrastructure\Timekeeper.Infrastructure\Timekeeper.Infrastructure.csproj"

# Check if projects exist
if (-not (Test-Path $CliProject)) {
    Write-Host "CLI project not found: $CliProject" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $InfraProject)) {
    Write-Host "Infrastructure project not found: $InfraProject" -ForegroundColor Red
    exit 1
}

# Check .NET installation
Write-Host "🔧 Environment Check:" -ForegroundColor Yellow
try {
    $dotnetVersion = & dotnet --version
    Write-Host "  ✅ .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  ❌ .NET not found!" -ForegroundColor Red
    exit 1
}

# Check EF Core tools
try {
    $efVersion = & dotnet ef --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ EF Core tools: $efVersion" -ForegroundColor Green
    } else {
        Write-Host "  ❌ EF Core tools not installed" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ EF Core tools not installed" -ForegroundColor Red
}

Write-Host ""

# Find database files
Write-Host "💾 Database Files:" -ForegroundColor Yellow
$dbFiles = @()
$possiblePaths = @(
    @{Path = (Join-Path $ProjectRoot "timekeeper.db"); Location = "Project Root"},
    @{Path = (Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\timekeeper.db"); Location = "CLI Project"},
    @{Path = (Join-Path $env:LOCALAPPDATA "Timekeeper\timekeeper.db"); Location = "User AppData"},
    @{Path = (Join-Path $env:APPDATA "Timekeeper\timekeeper.db"); Location = "User Roaming"},
    @{Path = (Join-Path $env:ProgramFiles "Timekeeper\timekeeper.db"); Location = "Program Files"}
)

foreach ($pathInfo in $possiblePaths) {
    if (Test-Path $pathInfo.Path) {
        $fileInfo = Get-Item $pathInfo.Path
        $sizeKB = [math]::Round($fileInfo.Length / 1024, 2)
        $lastModified = $fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        Write-Host "  ✅ $($pathInfo.Location): $sizeKB KB (Modified: $lastModified)" -ForegroundColor Green
        $dbFiles += $pathInfo.Path
    } else {
        Write-Host "  ❌ $($pathInfo.Location): Not found" -ForegroundColor Gray
    }
}

if ($dbFiles.Count -eq 0) {
    Write-Host "  ⚠️  No database files found" -ForegroundColor Yellow
}

Write-Host ""

# Check migrations
Write-Host "🔄 Migration Status:" -ForegroundColor Yellow
Set-Location $ProjectRoot

try {
    Write-Host "  Available migrations:" -ForegroundColor Cyan
    $migrations = & dotnet ef migrations list --project $InfraProject --startup-project $CliProject --no-build 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        $migrationLines = $migrations -split "`n" | Where-Object { $_ -match "^\d{14}_" }
        
        if ($migrationLines.Count -gt 0) {
            foreach ($migration in $migrationLines | Select-Object -Last 5) {
                if ($migration -match "^\d{14}_(.+)$") {
                    $migrationName = $matches[1]
                    $timestamp = $migration.Substring(0, 14)
                    $date = [DateTime]::ParseExact($timestamp, "yyyyMMddHHmmss", $null)
                    Write-Host "    📋 $migrationName ($($date.ToString('yyyy-MM-dd HH:mm')))" -ForegroundColor White
                }
            }
            
            if ($migrationLines.Count -gt 5) {
                Write-Host "    ... and $($migrationLines.Count - 5) more migrations" -ForegroundColor Gray
            }
        } else {
            Write-Host "    ❌ No migrations found" -ForegroundColor Red
        }
    } else {
        Write-Host "    ❌ Could not retrieve migration list" -ForegroundColor Red
    }
} catch {
    Write-Host "    ❌ Error checking migrations: $($_.Exception.Message)" -ForegroundColor Red
}

# Check database schema if requested
if ($ShowSchema -and $dbFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "🏗️  Database Schema:" -ForegroundColor Yellow
    
    $primaryDb = $dbFiles[0]
    $sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
    
    if ($sqliteCmd) {
        try {
            Write-Host "  Tables:" -ForegroundColor Cyan
            $tables = & sqlite3 $primaryDb ".tables"
            foreach ($table in $tables -split " " | Where-Object { $_ -ne "" }) {
                Write-Host "    📊 $table" -ForegroundColor White
                
                if ($Detailed) {
                    $schema = & sqlite3 $primaryDb ".schema $table"
                    Write-Host "      $schema" -ForegroundColor Gray
                }
            }
            
            Write-Host ""
            Write-Host "  Database Info:" -ForegroundColor Cyan
            $dbInfo = & sqlite3 $primaryDb "PRAGMA database_list;"
            Write-Host "    $dbInfo" -ForegroundColor White
            
        } catch {
            Write-Host "    ❌ Error reading schema: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "    ⚠️  SQLite command line tool not available" -ForegroundColor Yellow
        Write-Host "    Install SQLite to view schema details" -ForegroundColor Gray
    }
}

# Check application status
Write-Host ""
Write-Host "🚀 Application Status:" -ForegroundColor Yellow

try {
    # Try to get version to test if app is working
    $version = & dotnet run --project $CliProject -- --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ Application is working" -ForegroundColor Green
        if ($version) {
            Write-Host "    Version: $version" -ForegroundColor White
        }
    } else {
        Write-Host "  ❌ Application test failed" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Could not test application: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Status check completed!" -ForegroundColor Cyan

if ($dbFiles.Count -eq 0) {
    Write-Host ""
    Write-Host "💡 Tip: Run 'migrate-database.ps1' to initialize the database" -ForegroundColor Yellow
}

Write-Host "========================================" -ForegroundColor Cyan
