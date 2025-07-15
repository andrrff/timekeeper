# Timekeeper Windows Build Manager
# Simplified interface for building Windows installers

param(
    [ValidateSet("x64", "x86", "arm64", "all")]
    [string]$Architecture = "x64",
    
    [ValidateSet("msi", "portable", "all")]
    [string]$Type = "all",
    
    [switch]$Clean,
    [switch]$Help
)

if ($Help) {
    Write-Host @"

Timekeeper Windows Build Manager
=====================================

USAGE:
    .\Build.ps1 [OPTIONS]

OPTIONS:
    -Architecture    Target architecture (x64, x86, arm64, all) [default: x64]
    -Type           Build type (msi, portable, all) [default: all]
    -Clean          Clean build directories before building
    -Help           Show this help message

EXAMPLES:
    .\Build.ps1                          # Build x64 MSI and portable
    .\Build.ps1 -Architecture all        # Build all architectures
    .\Build.ps1 -Type msi                # Build only MSI installer
    .\Build.ps1 -Architecture arm64 -Type portable  # Build ARM64 portable only
    .\Build.ps1 -Clean                   # Clean build and rebuild

REQUIREMENTS:
    - .NET 9.0 SDK or higher
    - WiX Toolset (for MSI installers)
    - PowerShell 5.1 or higher

UNINSTALL:
    To completely remove Timekeeper, run:
    .\Uninstall-Timekeeper.ps1 -RemoveAllData

"@ -ForegroundColor Cyan
    exit 0
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BuildScript = Join-Path $ScriptDir "scripts\Build-WindowsInstallers.ps1"

if (-not (Test-Path $BuildScript)) {
    Write-Error "Build script not found: $BuildScript"
    exit 1
}

$params = @{
    Architecture = $Architecture
    Type = $Type
    Verbose = $true
}

if ($Clean) {
    $params.Clean = $true
}

Write-Host "🚀 Starting Timekeeper Windows build..." -ForegroundColor Green
Write-Host "Architecture: $Architecture | Type: $Type" -ForegroundColor Yellow

& $BuildScript @params
