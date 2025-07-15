# Build script for creating distributable packages

param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    
    [ValidateSet("win-x64", "win-x86", "win-arm64", "linux-x64", "osx-x64")]
    [string[]]$RuntimeIdentifiers = @("win-x64"),
    
    [switch]$SelfContained,
    [switch]$CreateZip,
    [string]$OutputDir
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Timekeeper CLI Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$CliProject = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj"

if (-not $OutputDir) {
    $OutputDir = Join-Path $ProjectRoot "dist"
}

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime Identifiers: $($RuntimeIdentifiers -join ', ')" -ForegroundColor Yellow
Write-Host "Self-contained: $($SelfContained.IsPresent)" -ForegroundColor Yellow
Write-Host "Output directory: $OutputDir" -ForegroundColor Yellow
Write-Host ""

foreach ($rid in $RuntimeIdentifiers) {
    Write-Host "Building for $rid..." -ForegroundColor Green
    
    $targetDir = Join-Path $OutputDir "$rid-$Configuration"
    if (Test-Path $targetDir) {
        Remove-Item $targetDir -Recurse -Force
    }
    
    # Build arguments
    $publishArgs = @(
        "publish",
        $CliProject,
        "-c", $Configuration,
        "-r", $rid,
        "-o", $targetDir,
        "--self-contained", $SelfContained.IsPresent.ToString().ToLower()
    )
    
    if ($rid.StartsWith("win-")) {
        $publishArgs += "-p:PublishSingleFile=true"
        $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
    }
    
    Write-Host "  Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Build failed for $rid!" -ForegroundColor Red
        continue
    }
    
    # Copy additional files
    $iconPath = Join-Path $ProjectRoot "res\TimeKeeper.ico"
    if (Test-Path $iconPath) {
        Copy-Item $iconPath $targetDir
    }
    
    $readmePath = Join-Path $ProjectRoot "README.md"
    if (Test-Path $readmePath) {
        Copy-Item $readmePath $targetDir
    }
    
    # Create platform-specific launcher
    if ($rid.StartsWith("win-")) {
        $tkBatPath = Join-Path $targetDir "tk.bat"
        $exeName = if ($SelfContained.IsPresent) { "Timekeeper.CLI.exe" } else { "Timekeeper.CLI.exe" }
        $tkBatContent = @"
@echo off
"%~dp0$exeName" %*
"@
        Set-Content -Path $tkBatPath -Value $tkBatContent
        
        # Create PowerShell launcher
        $tkPs1Path = Join-Path $targetDir "tk.ps1"
        $tkPs1Content = @"
#!/usr/bin/env pwsh
& "`$PSScriptRoot\$exeName" @args
"@
        Set-Content -Path $tkPs1Path -Value $tkPs1Content
        
    } else {
        # Linux/macOS launcher
        $tkShPath = Join-Path $targetDir "tk"
        $exeName = if ($SelfContained.IsPresent) { "Timekeeper.CLI" } else { "Timekeeper.CLI" }
        $tkShContent = @"
#!/bin/bash
DIR="`$(cd "`$(dirname "`${BASH_SOURCE[0]}")" &> /dev/null && pwd)"
"`$DIR/$exeName" "`$@"
"@
        Set-Content -Path $tkShPath -Value $tkShContent
        
        # Make executable (if running on Unix-like system)
        if ($IsLinux -or $IsMacOS) {
            chmod +x $tkShPath
            chmod +x (Join-Path $targetDir $exeName)
        }
    }
    
    Write-Host "  Build completed: $targetDir" -ForegroundColor Green
    
    # Create ZIP package if requested
    if ($CreateZip.IsPresent) {
        $zipPath = Join-Path $OutputDir "timekeeper-$rid-$Configuration.zip"
        if (Test-Path $zipPath) {
            Remove-Item $zipPath -Force
        }
        
        Write-Host "  Creating ZIP package: $zipPath" -ForegroundColor Yellow
        Compress-Archive -Path "$targetDir\*" -DestinationPath $zipPath
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "         Build completed!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output directory: $OutputDir" -ForegroundColor Green
Write-Host ""

# List created packages
$packages = Get-ChildItem $OutputDir -Directory | Where-Object { $_.Name -match "-$Configuration$" }
foreach ($package in $packages) {
    Write-Host "  📦 $($package.Name)" -ForegroundColor White
    
    $zipFile = Join-Path $OutputDir "$($package.Name.Replace(`"-$Configuration`", ``)).zip"
    if (Test-Path $zipFile) {
        $zipSize = [math]::Round((Get-Item $zipFile).Length / 1MB, 2)
        Write-Host "     📎 $(Split-Path $zipFile -Leaf) ($zipSize MB)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "To install, extract a package and run the appropriate installer script." -ForegroundColor Yellow
