# Timekeeper CLI Installer for PowerShell
# Run this script with Administrator privileges

param(
    [switch]$Force,
    [string]$InstallPath = "$env:ProgramFiles\Timekeeper"
)

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges." -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    Timekeeper CLI Installer v1.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
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
if (-not (Test-Path $InstallPath)) {
    Write-Host "Creating installation directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

# Build the project
Write-Host "Building Timekeeper CLI..." -ForegroundColor Yellow
Set-Location $ProjectRoot
$buildResult = & dotnet build $CliProject -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Publish the application
Write-Host "Publishing application..." -ForegroundColor Yellow
$publishResult = & dotnet publish $CliProject -c Release -o $InstallPath --self-contained false
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

# Add to PATH environment variable
Write-Host "Adding Timekeeper to PATH..." -ForegroundColor Yellow
$currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
if ($currentPath -notlike "*$InstallPath*") {
    Write-Host "Adding $InstallPath to system PATH..." -ForegroundColor Yellow
    $newPath = "$currentPath;$InstallPath"
    [Environment]::SetEnvironmentVariable("PATH", $newPath, "Machine")
    Write-Host "Successfully added to PATH." -ForegroundColor Green
} else {
    Write-Host "Timekeeper is already in PATH." -ForegroundColor Yellow
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

# Create Start Menu shortcut
Write-Host "Creating Start Menu shortcut..." -ForegroundColor Yellow
$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Timekeeper"
if (-not (Test-Path $startMenuPath)) {
    New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null
}

$startMenuShortcut = Join-Path $startMenuPath "Timekeeper.lnk"
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
# Timekeeper Uninstaller
param([switch]`$Force)

if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges." -ForegroundColor Red
    exit 1
}

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

# Remove from PATH
Write-Host "Removing from PATH..." -ForegroundColor Yellow
`$currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
`$newPath = `$currentPath -replace [regex]::Escape(";$InstallPath"), "" -replace [regex]::Escape("$InstallPath;"), ""
[Environment]::SetEnvironmentVariable("PATH", `$newPath, "Machine")

# Remove shortcuts
Write-Host "Removing shortcuts..." -ForegroundColor Yellow
`$desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "Timekeeper.lnk"
if (Test-Path `$desktopShortcut) { Remove-Item `$desktopShortcut -Force }

`$startMenuPath = "`$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Timekeeper"
if (Test-Path `$startMenuPath) { Remove-Item `$startMenuPath -Recurse -Force }

# Remove from Add/Remove Programs
Write-Host "Removing from Add/Remove Programs..." -ForegroundColor Yellow
Remove-Item "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" -Recurse -Force -ErrorAction SilentlyContinue

# Remove installation directory
Write-Host "Removing installation files..." -ForegroundColor Yellow
Set-Location `$env:TEMP
Remove-Item "$InstallPath" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Uninstallation completed successfully!" -ForegroundColor Green
"@
Set-Content -Path $uninstallerPath -Value $uninstallerContent

# Register in Add/Remove Programs
Write-Host "Registering in Add/Remove Programs..." -ForegroundColor Yellow
$uninstallKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper"
New-Item -Path $uninstallKey -Force | Out-Null
Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "Timekeeper CLI"
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "Timekeeper"
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value $InstallPath
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$uninstallerPath`""
Set-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1 -Type DWord

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    Installation completed successfully!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now use 'tk' from any command prompt or PowerShell." -ForegroundColor Green
Write-Host ""
Write-Host "To get started, open a new command prompt and type:" -ForegroundColor Yellow
Write-Host "  tk" -ForegroundColor White
Write-Host ""
Write-Host "Note: You may need to restart your command prompt" -ForegroundColor Yellow
Write-Host "      or log out and back in for the PATH changes" -ForegroundColor Yellow
Write-Host "      to take effect." -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
