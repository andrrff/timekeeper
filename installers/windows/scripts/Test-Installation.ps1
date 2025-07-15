# Timekeeper Installation Validator
# Tests that Timekeeper is properly installed and working

param(
    [switch]$Detailed,  # Show detailed test results
    [switch]$Silent     # Minimal output
)

$ErrorActionPreference = "Continue"

# Test configuration
$ProductName = "Timekeeper"
$ExpectedExecutables = @("timekeeper.exe", "Timekeeper.CLI.exe")
$ExpectedCommands = @("timekeeper", "tk")

# Functions
function Write-TestResult($test, $passed, $message = "") {
    if ($Silent) { return }
    
    $status = if ($passed) { "✓" } else { "✗" }
    $color = if ($passed) { "Green" } else { "Red" }
    
    Write-Host "$status $test" -ForegroundColor $color
    
    if ($message -and ($Detailed -or -not $passed)) {
        Write-Host "   $message" -ForegroundColor Gray
    }
}

function Test-InstallationPresence {
    Write-Host "`n🔍 Testing Installation Presence" -ForegroundColor Cyan
    
    # Test registry entries
    $registryExists = Test-Path "HKLM:\Software\$ProductName"
    Write-TestResult "Registry entries" $registryExists "HKLM:\Software\$ProductName"
    
    # Test uninstall entries
    $uninstallExists = $false
    $uninstallKeys = Get-ChildItem "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\" -ErrorAction SilentlyContinue
    foreach ($key in $uninstallKeys) {
        $app = Get-ItemProperty $key.PSPath -ErrorAction SilentlyContinue
        if ($app.DisplayName -like "*$ProductName*") {
            $uninstallExists = $true
            Write-TestResult "Uninstall entry" $true $app.DisplayName
            break
        }
    }
    if (-not $uninstallExists) {
        Write-TestResult "Uninstall entry" $false "Not found in Add/Remove Programs"
    }
    
    return @{
        Registry = $registryExists
        Uninstall = $uninstallExists
    }
}

function Test-ExecutableFiles {
    Write-Host "`n🔍 Testing Executable Files" -ForegroundColor Cyan
    
    $results = @{}
    
    # Get install location from registry
    $installLocation = $null
    try {
        $installLocation = (Get-ItemProperty "HKLM:\Software\$ProductName" -ErrorAction SilentlyContinue).InstallDir
    } catch { }
    
    # Test in install location
    if ($installLocation -and (Test-Path $installLocation)) {
        Write-TestResult "Install directory" $true $installLocation
        
        foreach ($exe in $ExpectedExecutables) {
            $exePath = Join-Path $installLocation $exe
            $exists = Test-Path $exePath
            $results[$exe] = $exePath
            Write-TestResult "Executable: $exe" $exists $exePath
        }
    } else {
        Write-TestResult "Install directory" $false "Not found or not accessible"
    }
    
    # Test in PATH
    foreach ($cmd in $ExpectedCommands) {
        try {
            $cmdPath = (Get-Command $cmd -ErrorAction Stop).Source
            Write-TestResult "Command in PATH: $cmd" $true $cmdPath
        } catch {
            Write-TestResult "Command in PATH: $cmd" $false "Not found in PATH"
        }
    }
    
    return $results
}

function Test-Shortcuts {
    Write-Host "`n🔍 Testing Shortcuts" -ForegroundColor Cyan
    
    $shortcuts = @(
        @{
            Name = "Desktop shortcut"
            Path = [System.IO.Path]::Combine([Environment]::GetFolderPath("Desktop"), "$ProductName.lnk")
        },
        @{
            Name = "Start Menu shortcut"
            Path = [System.IO.Path]::Combine([Environment]::GetFolderPath("StartMenu"), "Programs", $ProductName, "$ProductName.lnk")
        },
        @{
            Name = "Common Desktop shortcut"
            Path = [System.IO.Path]::Combine([Environment]::GetFolderPath("CommonDesktopDirectory"), "$ProductName.lnk")
        }
    )
    
    $results = @{}
    foreach ($shortcut in $shortcuts) {
        $exists = Test-Path $shortcut.Path
        $results[$shortcut.Name] = $exists
        Write-TestResult $shortcut.Name $exists $shortcut.Path
    }
    
    return $results
}

function Test-PathEnvironment {
    Write-Host "`n🔍 Testing Environment PATH" -ForegroundColor Cyan
    
    $systemPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    $currentPath = $env:PATH
    
    $inSystemPath = $systemPath -like "*$ProductName*"
    $inUserPath = $userPath -like "*$ProductName*"
    $inCurrentPath = $currentPath -like "*$ProductName*"
    
    Write-TestResult "System PATH" $inSystemPath
    Write-TestResult "User PATH" $inUserPath  
    Write-TestResult "Current session PATH" $inCurrentPath
    
    return @{
        System = $inSystemPath
        User = $inUserPath
        Current = $inCurrentPath
    }
}

function Test-ApplicationLaunch {
    Write-Host "`n🔍 Testing Application Launch" -ForegroundColor Cyan
    
    $results = @{}
    
    # Test timekeeper command
    try {
        $output = & timekeeper --version 2>&1
        $success = $LASTEXITCODE -eq 0
        Write-TestResult "Command execution: timekeeper --version" $success $output
        $results["timekeeper"] = $success
    } catch {
        Write-TestResult "Command execution: timekeeper --version" $false $_.Exception.Message
        $results["timekeeper"] = $false
    }
    
    # Test help command
    try {
        $output = & timekeeper --help 2>&1
        $success = $LASTEXITCODE -eq 0
        Write-TestResult "Help command: timekeeper --help" $success
        $results["help"] = $success
    } catch {
        Write-TestResult "Help command: timekeeper --help" $false $_.Exception.Message
        $results["help"] = $false
    }
    
    return $results
}

function Test-DataDirectories {
    Write-Host "`n🔍 Testing Data Directories" -ForegroundColor Cyan
    
    $dataDirs = @(
        @{
            Name = "AppData"
            Path = Join-Path $env:APPDATA $ProductName
        },
        @{
            Name = "LocalAppData"  
            Path = Join-Path $env:LOCALAPPDATA $ProductName
        }
    )
    
    $results = @{}
    foreach ($dir in $dataDirs) {
        $exists = Test-Path $dir.Path
        $results[$dir.Name] = $exists
        
        if ($exists) {
            $files = Get-ChildItem $dir.Path -Recurse -ErrorAction SilentlyContinue
            $fileCount = $files.Count
            $message = "$($dir.Path) [$fileCount files]"
        } else {
            $message = $dir.Path
        }
        
        Write-TestResult "$($dir.Name) directory" $exists $message
    }
    
    return $results
}

function Get-OverallStatus($testResults) {
    $criticalTests = @(
        $testResults.Installation.Registry,
        $testResults.Executables.Count -gt 0,
        $testResults.Launch["timekeeper"]
    )
    
    $passed = ($criticalTests | Where-Object { $_ -eq $true }).Count
    $total = $criticalTests.Count
    
    return @{
        Passed = $passed
        Total = $total
        Success = $passed -eq $total
    }
}

# Main execution
try {
    if (-not $Silent) {
        Write-Host @"
╔══════════════════════════════════════════════════════════════╗
║                 Timekeeper Installation Validator            ║
╚══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan
    }
    
    # Run tests
    $testResults = @{
        Installation = Test-InstallationPresence
        Executables = Test-ExecutableFiles
        Shortcuts = Test-Shortcuts
        Environment = Test-PathEnvironment
        Launch = Test-ApplicationLaunch
        Data = Test-DataDirectories
    }
    
    # Overall status
    $overall = Get-OverallStatus $testResults
    
    if (-not $Silent) {
        Write-Host "`n📊 Test Summary" -ForegroundColor Cyan
        Write-Host "═══════════════" -ForegroundColor Cyan
        
        if ($overall.Success) {
            Write-Host "✅ Installation is working correctly!" -ForegroundColor Green
            Write-Host "   All critical tests passed ($($overall.Passed)/$($overall.Total))" -ForegroundColor Green
        } else {
            Write-Host "❌ Installation has issues!" -ForegroundColor Red
            Write-Host "   Critical tests failed ($($overall.Passed)/$($overall.Total) passed)" -ForegroundColor Red
        }
        
        if ($Detailed) {
            Write-Host "`nDetailed Results:" -ForegroundColor Yellow
            $testResults | ConvertTo-Json -Depth 3 | Write-Host
        }
        
        Write-Host "`n💡 Troubleshooting:" -ForegroundColor Yellow
        Write-Host "   • Run as Administrator for system-wide tests" -ForegroundColor Gray
        Write-Host "   • Restart terminal/PowerShell after installation" -ForegroundColor Gray  
        Write-Host "   • Check Windows Event Log for errors" -ForegroundColor Gray
        Write-Host "   • Run: Get-EventLog -LogName Application -Source 'MsiInstaller'" -ForegroundColor Gray
    }
    
    # Return appropriate exit code
    exit (if ($overall.Success) { 0 } else { 1 })
    
} catch {
    Write-Error "Validation failed: $($_.Exception.Message)"
    exit 2
}
