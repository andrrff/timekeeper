# Timekeeper Database Migration Script
# This script applies database migrations and optimizes the database

param(
    [switch]$Force,
    [switch]$SkipOptimize,
    [switch]$Verbose
)

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "   Timekeeper Database Migration Tool" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$CliProject = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj"
$InfraProject = Join-Path $ProjectRoot "src\Infrastructure\Timekeeper.Infrastructure\Timekeeper.Infrastructure.csproj"

# Verify project files exist
if (-not (Test-Path $CliProject)) {
    Write-Host "CLI project not found: $CliProject" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $InfraProject)) {
    Write-Host "Infrastructure project not found: $InfraProject" -ForegroundColor Red
    exit 1
}

# Check if .NET is available
Write-Host "Checking .NET installation..." -ForegroundColor Yellow
try {
    $dotnetVersion = & dotnet --version
    Write-Host "Using .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host ".NET is not installed or not in PATH!" -ForegroundColor Red
    Write-Host "Please install .NET 9.0 or later from https://dotnet.microsoft.com/" -ForegroundColor Yellow
    exit 1
}

# Check if EF Core tools are installed
Write-Host "Checking EF Core tools..." -ForegroundColor Yellow
try {
    $efVersion = & dotnet ef --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
        & dotnet tool install --global dotnet-ef
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to install EF Core tools!" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "EF Core tools found: $efVersion" -ForegroundColor Green
    }
} catch {
    Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
    & dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install EF Core tools!" -ForegroundColor Red
        exit 1
    }
}

Set-Location $ProjectRoot

# Show current migration status
Write-Host ""
Write-Host "Checking current migration status..." -ForegroundColor Yellow
if ($Verbose) {
    & dotnet ef migrations list --project $InfraProject --startup-project $CliProject
} else {
    & dotnet ef migrations list --project $InfraProject --startup-project $CliProject --no-build | Select-Object -Last 5
}

# Check for pending migrations
Write-Host ""
Write-Host "Checking for pending migrations..." -ForegroundColor Yellow

# First, check if database exists and has tables
$dbFiles = @()
$possiblePaths = @(
    (Join-Path $ProjectRoot "timekeeper.db"),
    (Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\timekeeper.db")
)

$existingDb = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $existingDb = $path
        break
    }
}

if ($existingDb) {
    Write-Host "Found existing database: $existingDb" -ForegroundColor Yellow
    
    # Check if database has migration history table using .NET instead of SQLite CLI
    $hasMigrationHistory = $false
    
    try {
        # Use EF Core to check if migration history exists
        $migrationCheck = & dotnet ef migrations list --project $InfraProject --startup-project $CliProject --no-build 2>&1
        $migrationOutput = $migrationCheck | Out-String
        
        if ($LASTEXITCODE -eq 0 -and $migrationOutput -notlike "*No migrations*") {
            # Check if we actually have applied migrations vs just pending ones
            $appliedMigrations = $migrationCheck | Where-Object { $_ -notlike "*Pending*" -and $_ -match "^\d{15}_" }
            if ($appliedMigrations) {
                $hasMigrationHistory = $true
                Write-Host "Database has migration history - proceeding with normal migration" -ForegroundColor Green
            } else {
                # All migrations are pending, which suggests the database exists but has no migration history
                $hasMigrationHistory = $false
                Write-Host "All migrations are pending - database likely created without migrations" -ForegroundColor Yellow
            }
        } else {
            # Error occurred or no migrations found
            if ($migrationOutput -like "*no such table*" -or $migrationOutput -like "*__EFMigrationsHistory*") {
                Write-Host "Database exists but has no migration history table" -ForegroundColor Yellow
                $hasMigrationHistory = $false
            } else {
                Write-Host "Could not determine migration status - assuming no migration history" -ForegroundColor Yellow
                $hasMigrationHistory = $false
            }
        }
    } catch {
        Write-Host "Could not check migration history - assuming no migration history" -ForegroundColor Yellow
        $hasMigrationHistory = $false
    }
    
    if (-not $hasMigrationHistory) {
        Write-Host ""
        Write-Host "⚠️  DETECTED: Database without migration history!" -ForegroundColor Red
        Write-Host "This database was likely created using EnsureCreated() method." -ForegroundColor Yellow
        Write-Host ""
        
        if ($Force) {
            Write-Host "Force flag specified - backing up and recreating database..." -ForegroundColor Yellow
            $backupPath = "$existingDb.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            Copy-Item $existingDb $backupPath
            Write-Host "Backup created: $backupPath" -ForegroundColor Green
            Remove-Item $existingDb
            Write-Host "Existing database removed" -ForegroundColor Yellow
        } else {
            Write-Host "SOLUTIONS:" -ForegroundColor Cyan
            Write-Host "1. Use -Force to backup and recreate the database with proper migrations" -ForegroundColor White
            Write-Host "   Example: .\migrate-database.ps1 -Force" -ForegroundColor Gray
            Write-Host ""
            Write-Host "2. Use the conversion script to preserve your data:" -ForegroundColor White
            Write-Host "   .\convert-to-migrations.ps1 -Force" -ForegroundColor Gray
            Write-Host ""
            Write-Host "3. Manually backup your data and delete the database file" -ForegroundColor White
            Write-Host "   Then run this script again" -ForegroundColor Gray
            Write-Host ""
            $choice = Read-Host "Continue anyway and risk errors? (y/N)"
            if ($choice -ne "y" -and $choice -ne "Y") {
                Write-Host "Migration cancelled by user" -ForegroundColor Yellow
                Write-Host ""
                Write-Host "Recommended next steps:" -ForegroundColor Cyan
                Write-Host "  .\convert-to-migrations.ps1 -Force" -ForegroundColor Yellow
                exit 0
            }
        }
    }
}

# Apply migrations
Write-Host ""
Write-Host "Applying database migrations..." -ForegroundColor Yellow

# First attempt - try normal migration
if ($Verbose) {
    & dotnet ef database update --project $InfraProject --startup-project $CliProject --verbose
} else {
    & dotnet ef database update --project $InfraProject --startup-project $CliProject
}

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Database migration failed!" -ForegroundColor Red
    
    # Check if this is the "table already exists" error
    $errorOutput = & dotnet ef database update --project $InfraProject --startup-project $CliProject 2>&1 | Out-String
    
    if ($errorOutput -like "*already exists*") {
        Write-Host "This appears to be a database that was created without migrations." -ForegroundColor Yellow
        Write-Host ""
        
        if ($Force) {
            Write-Host "Force flag specified - will recreate database with proper migrations" -ForegroundColor Yellow
            
            # Find and backup existing database
            $dbToBackup = $null
            foreach ($path in $possiblePaths) {
                if (Test-Path $path) {
                    $dbToBackup = $path
                    break
                }
            }
            
            if ($dbToBackup) {
                $backupPath = "$dbToBackup.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
                Copy-Item $dbToBackup $backupPath
                Write-Host "Database backed up to: $backupPath" -ForegroundColor Green
                
                # Remove all SQLite related files (main db, shared memory, and WAL files)
                $dbBaseName = [System.IO.Path]::GetFileNameWithoutExtension($dbToBackup)
                $dbDirectory = [System.IO.Path]::GetDirectoryName($dbToBackup)
                $dbExtension = [System.IO.Path]::GetExtension($dbToBackup)
                
                $filesToRemove = @(
                    $dbToBackup,                                    # main database file
                    "$dbDirectory\$dbBaseName$dbExtension-shm",     # shared memory file
                    "$dbDirectory\$dbBaseName$dbExtension-wal"      # write-ahead log file
                )
                
                foreach ($file in $filesToRemove) {
                    if (Test-Path $file) {
                        Remove-Item $file -Force
                        Write-Host "Removed: $([System.IO.Path]::GetFileName($file))" -ForegroundColor Yellow
                    }
                }
                
                Write-Host "All SQLite files removed" -ForegroundColor Yellow
                
                # Try migration again
                Write-Host "Applying migrations to new database..." -ForegroundColor Yellow
                & dotnet ef database update --project $InfraProject --startup-project $CliProject
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Database migration completed successfully!" -ForegroundColor Green
                } else {
                    Write-Host "Migration failed even after recreating database!" -ForegroundColor Red
                    exit 1
                }
            }
        } else {
            Write-Host "To fix this issue, you can:" -ForegroundColor Cyan
            Write-Host "1. Run this script with -Force to backup and recreate the database" -ForegroundColor White
            Write-Host "2. Manually delete the database file and run migrations again" -ForegroundColor White
            Write-Host "3. Export your data, delete the database, and import after migration" -ForegroundColor White
            Write-Host ""
            Write-Host "Example: .\migrate-database.ps1 -Force" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "Migration failed with unexpected error." -ForegroundColor Red
        Write-Host "Error details:" -ForegroundColor Yellow
        Write-Host $errorOutput -ForegroundColor Gray
        
        # Try to initialize database first
        Write-Host ""
        Write-Host "Attempting to initialize database..." -ForegroundColor Yellow
        & dotnet run --project $CliProject -- --version > $null 2>&1
        
        # Try migration again
        Write-Host "Retrying migration..." -ForegroundColor Yellow
        & dotnet ef database update --project $InfraProject --startup-project $CliProject
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Database migration failed!" -ForegroundColor Red
            Write-Host "The application will attempt to create the database on first run." -ForegroundColor Yellow
            exit 1
        } else {
            Write-Host "Database migration completed successfully on retry!" -ForegroundColor Green
        }
    }
} else {
    Write-Host "Database migration completed successfully!" -ForegroundColor Green
}

# Optimize database if requested
if (-not $SkipOptimize) {
    Write-Host ""
    Write-Host "Optimizing database..." -ForegroundColor Yellow
    
    # Find database files
    $dbFiles = @()
    $possiblePaths = @(
        (Join-Path $ProjectRoot "timekeeper.db"),
        (Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\timekeeper.db"),
        (Join-Path $env:LOCALAPPDATA "Timekeeper\timekeeper.db"),
        (Join-Path $env:APPDATA "Timekeeper\timekeeper.db")
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $dbFiles += $path
        }
    }
    
    if ($dbFiles.Count -eq 0) {
        Write-Host "No database files found to optimize." -ForegroundColor Yellow
    } else {
        foreach ($dbFile in $dbFiles) {
            Write-Host "Optimizing: $dbFile" -ForegroundColor Yellow
            
            try {
                # Check if SQLite command line tool is available
                $sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
                if ($sqliteCmd) {
                    $sqlCommands = @(
                        "PRAGMA integrity_check;",
                        "VACUUM;",
                        "ANALYZE;",
                        "PRAGMA optimize;",
                        "PRAGMA wal_checkpoint(TRUNCATE);"
                    )
                    
                    foreach ($cmd in $sqlCommands) {
                        if ($Verbose) {
                            Write-Host "Executing: $cmd" -ForegroundColor Gray
                        }
                        & sqlite3 $dbFile $cmd
                    }
                    
                    # Get database file size
                    $fileInfo = Get-Item $dbFile
                    $sizeKB = [math]::Round($fileInfo.Length / 1024, 2)
                    Write-Host "Database optimized: $sizeKB KB" -ForegroundColor Green
                } else {
                    Write-Host "SQLite command line tool not found." -ForegroundColor Yellow
                    Write-Host "Database will be optimized automatically by the application." -ForegroundColor Yellow
                }
            } catch {
                Write-Host "Database optimization failed: $($_.Exception.Message)" -ForegroundColor Yellow
                Write-Host "This is usually not a problem." -ForegroundColor Gray
            }
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "   Database migration completed!" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# Show final status
Write-Host "Final migration status:" -ForegroundColor Yellow
& dotnet ef migrations list --project $InfraProject --startup-project $CliProject --no-build | Select-Object -Last 3

Write-Host ""
Write-Host "Database is ready for use!" -ForegroundColor Green
