# Development installer for testing purposes
# This script creates a development installation that points to the build output

param(
    [string]$BuildConfiguration = "Debug"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Timekeeper CLI Development Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$CliProject = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj"
$BuildOutput = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\bin\$BuildConfiguration\net9.0"
$DevInstallPath = "$env:LOCALAPPDATA\Timekeeper-Dev"

Write-Host "Installing Timekeeper CLI for development..." -ForegroundColor Green
Write-Host "Build Configuration: $BuildConfiguration" -ForegroundColor Yellow
Write-Host "Installation directory: $DevInstallPath" -ForegroundColor Yellow
Write-Host ""

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
Set-Location $ProjectRoot
& dotnet build $CliProject -c $BuildConfiguration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create dev installation directory
if (Test-Path $DevInstallPath) {
    Remove-Item $DevInstallPath -Recurse -Force
}
New-Item -ItemType Directory -Path $DevInstallPath -Force | Out-Null

# Create a launcher script that points to the build output
$TimekeeperExe = Join-Path $BuildOutput "Timekeeper.CLI.exe"
$tkDevBatPath = Join-Path $DevInstallPath "tk-dev.bat"
$tkDevBatContent = @"
@echo off
REM Development launcher for Timekeeper CLI
REM Points to build output: $TimekeeperExe
"$TimekeeperExe" %*
"@
Set-Content -Path $tkDevBatPath -Value $tkDevBatContent

# Create PowerShell version
$tkDevPs1Path = Join-Path $DevInstallPath "tk-dev.ps1"
$tkDevPs1Content = @"
#!/usr/bin/env pwsh
# Development launcher for Timekeeper CLI
# Points to build output: $TimekeeperExe
& "$TimekeeperExe" @args
"@
Set-Content -Path $tkDevPs1Path -Value $tkDevPs1Content

# Create an alias script for 'tk' command in development
$tkBatPath = Join-Path $DevInstallPath "tk.bat"
Set-Content -Path $tkBatPath -Value $tkDevBatContent

$tkPs1Path = Join-Path $DevInstallPath "tk.ps1"
Set-Content -Path $tkPs1Path -Value $tkDevPs1Content

# Add to user PATH for development
Write-Host "Adding development installation to user PATH..." -ForegroundColor Yellow
$currentUserPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if (-not $currentUserPath) { $currentUserPath = "" }

if ($currentUserPath -notlike "*$DevInstallPath*") {
    if ($currentUserPath) {
        $newUserPath = "$DevInstallPath;$currentUserPath"
    } else {
        $newUserPath = $DevInstallPath
    }
    [Environment]::SetEnvironmentVariable("PATH", $newUserPath, "User")
    Write-Host "Added to user PATH (development installation has priority)" -ForegroundColor Green
} else {
    Write-Host "Development installation already in PATH" -ForegroundColor Yellow
}

# Create a rebuild script
$rebuildPath = Join-Path $DevInstallPath "rebuild.ps1"
$rebuildContent = @"
# Rebuild Timekeeper CLI for development
Write-Host "Rebuilding Timekeeper CLI..." -ForegroundColor Yellow
Set-Location "$ProjectRoot"
& dotnet build "$CliProject" -c $BuildConfiguration
if (`$LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}
"@
Set-Content -Path $rebuildPath -Value $rebuildContent

# Create an uninstaller for dev environment
$uninstallDevPath = Join-Path $DevInstallPath "uninstall-dev.ps1"
$uninstallDevContent = @"
# Remove Timekeeper CLI development installation
Write-Host "Removing development installation..." -ForegroundColor Yellow

# Remove from user PATH
`$currentUserPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if (`$currentUserPath) {
    `$newUserPath = `$currentUserPath -replace [regex]::Escape(";$DevInstallPath"), "" -replace [regex]::Escape("$DevInstallPath;"), "" -replace [regex]::Escape("$DevInstallPath"), ""
    [Environment]::SetEnvironmentVariable("PATH", `$newUserPath, "User")
}

# Remove installation directory
Set-Location `$env:TEMP
Remove-Item "$DevInstallPath" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Development installation removed!" -ForegroundColor Green
"@
Set-Content -Path $uninstallDevPath -Value $uninstallDevContent

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Development installation completed!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Development installation details:" -ForegroundColor Green
Write-Host "  Installation path: $DevInstallPath" -ForegroundColor White
Write-Host "  Points to build: $BuildOutput" -ForegroundColor White
Write-Host "  Rebuild script: $rebuildPath" -ForegroundColor White
Write-Host "  Uninstaller: $uninstallDevPath" -ForegroundColor White
Write-Host ""
Write-Host "Usage:" -ForegroundColor Yellow
Write-Host "  tk          - Run Timekeeper CLI" -ForegroundColor White
Write-Host "  tk-dev      - Run Timekeeper CLI (explicit dev version)" -ForegroundColor White
Write-Host ""
Write-Host "Development commands:" -ForegroundColor Yellow
Write-Host "  & '$rebuildPath'    - Rebuild the project" -ForegroundColor White
Write-Host "  & '$uninstallDevPath'  - Remove dev installation" -ForegroundColor White
Write-Host ""
Write-Host "Note: Any changes to the source code will require rebuilding." -ForegroundColor Cyan
Write-Host "      Restart your command prompt for PATH changes to take effect." -ForegroundColor Yellow
