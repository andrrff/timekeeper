# Simple installer for development/testing purposes
# This script installs Timekeeper without requiring administrator privileges

param(
    [string]$InstallPath = "$env:LOCALAPPDATA\Timekeeper",
    [switch]$Force
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Timekeeper CLI User Installer v1.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installing to user directory (no admin required)" -ForegroundColor Yellow
Write-Host ""

# Variables
$TimekeeperExe = Join-Path $InstallPath "Timekeeper.CLI.exe"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$CliProject = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj"

Write-Host "Installing Timekeeper CLI..." -ForegroundColor Green
Write-Host "Installation directory: $InstallPath" -ForegroundColor Yellow
Write-Host ""

# Create installation directory
if (Test-Path $InstallPath) {
    if ($Force) {
        Write-Host "Removing existing installation..." -ForegroundColor Yellow
        Remove-Item $InstallPath -Recurse -Force
    } else {
        $overwrite = Read-Host "Installation directory exists. Overwrite? (y/n)"
        if ($overwrite -ne "y" -and $overwrite -ne "Y") {
            Write-Host "Installation cancelled." -ForegroundColor Yellow
            exit 0
        }
        Remove-Item $InstallPath -Recurse -Force
    }
}

Write-Host "Creating installation directory..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# Build and publish the project
Write-Host "Building Timekeeper CLI..." -ForegroundColor Yellow
Set-Location $ProjectRoot

# Check if .NET is available
try {
    $dotnetVersion = & dotnet --version
    Write-Host "Using .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host ".NET is not installed or not in PATH!" -ForegroundColor Red
    Write-Host "Please install .NET 9.0 or later from https://dotnet.microsoft.com/" -ForegroundColor Yellow
    exit 1
}

# Build the project
& dotnet build $CliProject -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Publish the application
Write-Host "Publishing application..." -ForegroundColor Yellow
& dotnet publish $CliProject -c Release -o $InstallPath --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

# Copy additional files
Write-Host "Copying additional files..." -ForegroundColor Yellow
$iconPath = Join-Path $ProjectRoot "res\TimeKeeper.ico"
if (Test-Path $iconPath) {
    Copy-Item $iconPath $InstallPath
}

$readmePath = Join-Path $ProjectRoot "README.md"
if (Test-Path $readmePath) {
    Copy-Item $readmePath $InstallPath
}

# Create batch wrapper for the tk command
Write-Host "Creating tk command wrapper..." -ForegroundColor Yellow
$tkBatPath = Join-Path $InstallPath "tk.bat"
$tkBatContent = @"
@echo off
chcp 65001 >nul 2>&1
"$TimekeeperExe" %*
"@
Set-Content -Path $tkBatPath -Value $tkBatContent

# Create PowerShell wrapper
$tkPs1Path = Join-Path $InstallPath "tk.ps1"
$tkPs1Content = @"
#!/usr/bin/env pwsh
& "$TimekeeperExe" @args
"@
Set-Content -Path $tkPs1Path -Value $tkPs1Content

# Add to user PATH environment variable
Write-Host "Adding Timekeeper to user PATH..." -ForegroundColor Yellow
$currentUserPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if (-not $currentUserPath) { $currentUserPath = "" }

if ($currentUserPath -notlike "*$InstallPath*") {
    Write-Host "Adding $InstallPath to user PATH..." -ForegroundColor Yellow
    if ($currentUserPath) {
        $newUserPath = "$currentUserPath;$InstallPath"
    } else {
        $newUserPath = $InstallPath
    }
    [Environment]::SetEnvironmentVariable("PATH", $newUserPath, "User")
    Write-Host "Successfully added to user PATH." -ForegroundColor Green
} else {
    Write-Host "Timekeeper is already in user PATH." -ForegroundColor Yellow
}

# Create desktop shortcut
$createShortcut = Read-Host "Create desktop shortcut? (y/n)"
if ($createShortcut -eq "y" -or $createShortcut -eq "Y") {
    Write-Host "Creating desktop shortcut..." -ForegroundColor Yellow
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "Timekeeper.lnk"
    
    $WshShell = New-Object -comObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($shortcutPath)
    $Shortcut.TargetPath = $TimekeeperExe
    $iconFile = Join-Path $InstallPath "TimeKeeper.ico"
    if (Test-Path $iconFile) {
        $Shortcut.IconLocation = $iconFile
    }
    $Shortcut.Save()
}

# Create Start Menu shortcut in user's start menu
Write-Host "Creating Start Menu shortcut..." -ForegroundColor Yellow
$userStartMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Timekeeper"
if (-not (Test-Path $userStartMenuPath)) {
    New-Item -ItemType Directory -Path $userStartMenuPath -Force | Out-Null
}

$startMenuShortcut = Join-Path $userStartMenuPath "Timekeeper.lnk"
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($startMenuShortcut)
$Shortcut.TargetPath = $TimekeeperExe
$iconFile = Join-Path $InstallPath "TimeKeeper.ico"
if (Test-Path $iconFile) {
    $Shortcut.IconLocation = $iconFile
}
$Shortcut.Save()

# Create uninstaller
Write-Host "Creating uninstaller..." -ForegroundColor Yellow
$uninstallerPath = Join-Path $InstallPath "uninstall.ps1"
$uninstallerContent = @"
# Timekeeper User Uninstaller
param([switch]`$Force)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    Timekeeper CLI Uninstaller" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not `$Force) {
    `$confirm = Read-Host "Are you sure you want to uninstall Timekeeper? (y/n)"
    if (`$confirm -ne "y" -and `$confirm -ne "Y") {
        exit 0
    }
}

Write-Host "Removing Timekeeper..." -ForegroundColor Yellow

# Remove from user PATH
Write-Host "Removing from user PATH..." -ForegroundColor Yellow
`$currentUserPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if (`$currentUserPath) {
    `$newUserPath = `$currentUserPath -replace [regex]::Escape(";$InstallPath"), "" -replace [regex]::Escape("$InstallPath;"), "" -replace [regex]::Escape("$InstallPath"), ""
    [Environment]::SetEnvironmentVariable("PATH", `$newUserPath, "User")
}

# Remove shortcuts
Write-Host "Removing shortcuts..." -ForegroundColor Yellow
`$desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "Timekeeper.lnk"
if (Test-Path `$desktopShortcut) { Remove-Item `$desktopShortcut -Force }

`$userStartMenuPath = "`$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Timekeeper"
if (Test-Path `$userStartMenuPath) { Remove-Item `$userStartMenuPath -Recurse -Force }

# Remove installation directory
Write-Host "Removing installation files..." -ForegroundColor Yellow
Set-Location `$env:TEMP
Remove-Item "$InstallPath" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Uninstallation completed successfully!" -ForegroundColor Green
Write-Host "You may need to restart your command prompt for PATH changes to take effect." -ForegroundColor Yellow
"@
Set-Content -Path $uninstallerPath -Value $uninstallerContent

# Create a simple launcher script
$launcherPath = Join-Path $InstallPath "launch-timekeeper.ps1"
$launcherContent = @"
# Timekeeper Launcher
# This script can be used to launch Timekeeper with specific parameters

param(
    [string[]]`$Arguments = @()
)

`$TimekeeperPath = "$TimekeeperExe"
if (`$Arguments.Count -gt 0) {
    & `$TimekeeperPath @Arguments
} else {
    & `$TimekeeperPath
}
"@
Set-Content -Path $launcherPath -Value $launcherContent

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    Installation completed successfully!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Timekeeper has been installed to your user directory." -ForegroundColor Green
Write-Host "You can now use 'tk' from any command prompt or PowerShell." -ForegroundColor Green
Write-Host ""
Write-Host "To get started, open a new command prompt and type:" -ForegroundColor Yellow
Write-Host "  tk" -ForegroundColor White
Write-Host ""
Write-Host "Installation details:" -ForegroundColor Yellow
Write-Host "  Installation path: $InstallPath" -ForegroundColor White
Write-Host "  Executable: $TimekeeperExe" -ForegroundColor White
Write-Host "  Uninstaller: $uninstallerPath" -ForegroundColor White
Write-Host ""
Write-Host "Note: You may need to restart your command prompt" -ForegroundColor Yellow
Write-Host "      for the PATH changes to take effect." -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
