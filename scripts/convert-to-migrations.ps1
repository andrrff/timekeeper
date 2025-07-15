# Timekeeper Database Migration Converter
# This script converts a database created with EnsureCreated() to use proper EF migrations

param(
    [switch]$Force,
    [switch]$BackupOnly,
    [string]$BackupPath
)

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "   Database Migration Converter" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "This script converts databases created with EnsureCreated() to use proper migrations." -ForegroundColor Yellow
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$CliProject = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj"
$InfraProject = Join-Path $ProjectRoot "src\Infrastructure\Timekeeper.Infrastructure\Timekeeper.Infrastructure.csproj"

# Find existing database
$possiblePaths = @(
    (Join-Path $ProjectRoot "timekeeper.db"),
    (Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\timekeeper.db"),
    (Join-Path $env:LOCALAPPDATA "Timekeeper\timekeeper.db"),
    (Join-Path $env:APPDATA "Timekeeper\timekeeper.db")
)

$existingDb = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $existingDb = $path
        Write-Host "Found database: $path" -ForegroundColor Green
        break
    }
}

if (-not $existingDb) {
    Write-Host "No existing database found!" -ForegroundColor Red
    Write-Host "Searched in:" -ForegroundColor Yellow
    foreach ($path in $possiblePaths) {
        Write-Host "  - $path" -ForegroundColor Gray
    }
    exit 1
}

# Check if SQLite command line tool is available
$sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $sqliteCmd) {
    Write-Host "Warning: SQLite command line tool not found." -ForegroundColor Yellow
    Write-Host "Some operations may not be available." -ForegroundColor Yellow
    Write-Host ""
}

# Check database structure
Write-Host "Analyzing database structure..." -ForegroundColor Yellow
$hasMigrationHistory = $false

if ($sqliteCmd) {
    try {
        $tables = & sqlite3 $existingDb ".tables"
        if ($tables -like "*__EFMigrationsHistory*") {
            $hasMigrationHistory = $true
        }
        
        Write-Host "Tables found:" -ForegroundColor Cyan
        foreach ($table in $tables -split " " | Where-Object { $_ -ne "" }) {
            if ($table -eq "__EFMigrationsHistory") {
                Write-Host "  ✅ $table (Migration history)" -ForegroundColor Green
            } else {
                Write-Host "  📊 $table" -ForegroundColor White
            }
        }
    } catch {
        Write-Host "Could not analyze database structure: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "Cannot analyze database without SQLite tools" -ForegroundColor Yellow
}

Write-Host ""

if ($hasMigrationHistory) {
    Write-Host "✅ Database already has migration history!" -ForegroundColor Green
    Write-Host "No conversion needed. You can run migrate-database.ps1 normally." -ForegroundColor Yellow
    exit 0
}

# Create backup
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
if ($BackupPath) {
    $backupFile = $BackupPath
} else {
    $backupFile = "$existingDb.backup.$timestamp"
}

Write-Host "Creating backup..." -ForegroundColor Yellow
try {
    Copy-Item $existingDb $backupFile
    Write-Host "✅ Backup created: $backupFile" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to create backup: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

if ($BackupOnly) {
    Write-Host ""
    Write-Host "Backup completed. Original database unchanged." -ForegroundColor Green
    exit 0
}

# Export data if SQLite is available
$dataExportFile = $null
if ($sqliteCmd) {
    Write-Host ""
    Write-Host "Exporting data..." -ForegroundColor Yellow
    $dataExportFile = "$existingDb.data.$timestamp.sql"
    
    try {
        # Export data as INSERT statements
        & sqlite3 $existingDb ".dump" | Out-File -FilePath $dataExportFile -Encoding UTF8
        Write-Host "✅ Data exported to: $dataExportFile" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Warning: Could not export data: $($_.Exception.Message)" -ForegroundColor Yellow
        $dataExportFile = $null
    }
}

if (-not $Force) {
    Write-Host ""
    Write-Host "⚠️  WARNING: This operation will delete your current database!" -ForegroundColor Red
    Write-Host "Backup created at: $backupFile" -ForegroundColor Yellow
    if ($dataExportFile) {
        Write-Host "Data export created at: $dataExportFile" -ForegroundColor Yellow
    }
    Write-Host ""
    $confirm = Read-Host "Continue with database conversion? (y/N)"
    if ($confirm -ne "y" -and $confirm -ne "Y") {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Remove existing database and related files
Write-Host ""
Write-Host "Removing existing database and related files..." -ForegroundColor Yellow
try {
    # Remove all SQLite related files (main db, shared memory, and WAL files)
    $dbBaseName = [System.IO.Path]::GetFileNameWithoutExtension($existingDb)
    $dbDirectory = [System.IO.Path]::GetDirectoryName($existingDb)
    $dbExtension = [System.IO.Path]::GetExtension($existingDb)
    
    $filesToRemove = @(
        $existingDb,                                    # main database file
        "$dbDirectory\$dbBaseName$dbExtension-shm",     # shared memory file
        "$dbDirectory\$dbBaseName$dbExtension-wal"      # write-ahead log file
    )
    
    foreach ($file in $filesToRemove) {
        if (Test-Path $file) {
            Remove-Item $file -Force
            Write-Host "✅ Removed: $([System.IO.Path]::GetFileName($file))" -ForegroundColor Green
        }
    }
    
    Write-Host "✅ All SQLite files removed" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to remove database files: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Apply migrations to create new database
Write-Host ""
Write-Host "Creating new database with migrations..." -ForegroundColor Yellow
Set-Location $ProjectRoot

& dotnet ef database update --project $InfraProject --startup-project $CliProject

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to create database with migrations!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Restoring backup..." -ForegroundColor Yellow
    Copy-Item $backupFile $existingDb
    Write-Host "✅ Backup restored" -ForegroundColor Green
    exit 1
}

Write-Host "✅ New database created with migration support!" -ForegroundColor Green

# Suggest data import if export was successful
if ($dataExportFile) {
    Write-Host ""
    Write-Host "🔄 Data Import Options:" -ForegroundColor Cyan
    Write-Host "Your data has been exported to: $dataExportFile" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To import your data back:" -ForegroundColor White
    Write-Host "1. Review the export file to ensure it looks correct" -ForegroundColor Gray
    Write-Host "2. Use SQLite command line: sqlite3 $existingDb < $dataExportFile" -ForegroundColor Gray
    Write-Host "3. Or manually re-enter your important data through the application" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "   Conversion completed successfully!" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "✅ Database converted to use EF migrations" -ForegroundColor Green
Write-Host "✅ Backup available at: $backupFile" -ForegroundColor Green
if ($dataExportFile) {
    Write-Host "✅ Data export available at: $dataExportFile" -ForegroundColor Green
}
Write-Host ""
Write-Host "You can now use migrate-database.ps1 for future updates!" -ForegroundColor Yellow
