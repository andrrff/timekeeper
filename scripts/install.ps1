# Quick installer that automatically detects the best installation method

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    Timekeeper CLI Auto-Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if ($isAdmin) {
    Write-Host "Running with Administrator privileges." -ForegroundColor Green
    Write-Host "Installing system-wide (recommended)..." -ForegroundColor Yellow
    Write-Host ""
    
    $systemInstaller = Join-Path $ScriptDir "install-windows.ps1"
    if (Test-Path $systemInstaller) {
        & $systemInstaller
    } else {
        Write-Host "System installer not found!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Running without Administrator privileges." -ForegroundColor Yellow
    Write-Host "Installing to user directory..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "For system-wide installation, restart PowerShell as Administrator." -ForegroundColor Cyan
    Write-Host ""
    
    $userInstaller = Join-Path $ScriptDir "install-user.ps1"
    if (Test-Path $userInstaller) {
        & $userInstaller
    } else {
        Write-Host "User installer not found!" -ForegroundColor Red
        exit 1
    }
}
