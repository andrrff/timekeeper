# Timekeeper Complete Uninstaller
# Removes all traces of Timekeeper from the system including user data

param(
    [switch]$RemoveUserData,    # Remove user data and settings
    [switch]$RemoveAllData,     # Remove ALL data including databases
    [switch]$Force,             # Skip confirmation prompts
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Configuration
$ProductName = "Timekeeper"
$Manufacturer = "Timekeeper Team"

# Data directories
$AppDataDir = Join-Path $env:APPDATA $ProductName
$LocalAppDataDir = Join-Path $env:LOCALAPPDATA $ProductName
$ProgramDataDir = Join-Path $env:PROGRAMDATA $ProductName

# Registry keys
$RegistryKeys = @(
    "HKLM:\Software\$ProductName",
    "HKCU:\Software\$ProductName",
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*"
)

# Functions
function Write-Info($message) {
    Write-Host "✓ $message" -ForegroundColor Green
}

function Write-Warning($message) {
    Write-Host "⚠ $message" -ForegroundColor Yellow
}

function Write-Error($message) {
    Write-Host "✗ $message" -ForegroundColor Red
}

function Write-Step($message) {
    Write-Host "`n🗑️ $message" -ForegroundColor Cyan
    Write-Host "═" * ($message.Length + 4) -ForegroundColor Cyan
}

function Get-InstalledVersions {
    $versions = @()
    
    # Check MSI installations
    $uninstallKeys = Get-ChildItem "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\" -ErrorAction SilentlyContinue
    foreach ($key in $uninstallKeys) {
        $app = Get-ItemProperty $key.PSPath -ErrorAction SilentlyContinue
        if ($app.DisplayName -like "*$ProductName*") {
            $versions += @{
                Name = $app.DisplayName
                Version = $app.DisplayVersion
                UninstallString = $app.UninstallString
                InstallLocation = $app.InstallLocation
                Type = "MSI"
                ProductCode = $key.PSChildName
            }
        }
    }
    
    # Check 32-bit installations on 64-bit systems
    if ([Environment]::Is64BitOperatingSystem) {
        $uninstallKeys32 = Get-ChildItem "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" -ErrorAction SilentlyContinue
        foreach ($key in $uninstallKeys32) {
            $app = Get-ItemProperty $key.PSPath -ErrorAction SilentlyContinue
            if ($app.DisplayName -like "*$ProductName*") {
                $versions += @{
                    Name = $app.DisplayName
                    Version = $app.DisplayVersion
                    UninstallString = $app.UninstallString
                    InstallLocation = $app.InstallLocation
                    Type = "MSI (32-bit)"
                    ProductCode = $key.PSChildName
                }
            }
        }
    }
    
    return $versions
}

function Remove-InstalledVersions($versions) {
    foreach ($version in $versions) {
        Write-Info "Uninstalling: $($version.Name) ($($version.Type))"
        
        if ($version.UninstallString) {
            try {
                if ($version.UninstallString -match "msiexec") {
                    # MSI uninstall
                    $productCode = $version.ProductCode
                    if ($productCode -match "^\{.*\}$") {
                        Write-Info "Running MSI uninstaller for $($version.Name)..."
                        Start-Process -FilePath "msiexec.exe" -ArgumentList "/x", $productCode, "/quiet", "/norestart" -Wait -NoNewWindow
                    }
                } else {
                    # Custom uninstaller
                    Write-Info "Running custom uninstaller: $($version.UninstallString)"
                    Invoke-Expression $version.UninstallString
                }
                Write-Info "Successfully uninstalled: $($version.Name)"
            } catch {
                Write-Warning "Failed to uninstall $($version.Name): $($_.Exception.Message)"
            }
        }
        
        # Remove installation directory if it still exists
        if ($version.InstallLocation -and (Test-Path $version.InstallLocation)) {
            try {
                Remove-Item -Path $version.InstallLocation -Recurse -Force
                Write-Info "Removed installation directory: $($version.InstallLocation)"
            } catch {
                Write-Warning "Could not remove installation directory: $($version.InstallLocation)"
            }
        }
    }
}

function Remove-RegistryEntries {
    Write-Step "Removing Registry Entries"
    
    foreach ($keyPath in $RegistryKeys) {
        try {
            if ($keyPath -like "*Uninstall*") {
                # Special handling for uninstall entries
                $uninstallKeys = Get-ChildItem $keyPath.Replace("*", "") -ErrorAction SilentlyContinue
                foreach ($key in $uninstallKeys) {
                    $app = Get-ItemProperty $key.PSPath -ErrorAction SilentlyContinue
                    if ($app.DisplayName -like "*$ProductName*") {
                        Remove-Item $key.PSPath -Recurse -Force
                        Write-Info "Removed uninstall entry: $($app.DisplayName)"
                    }
                }
            } else {
                if (Test-Path $keyPath) {
                    Remove-Item -Path $keyPath -Recurse -Force
                    Write-Info "Removed registry key: $keyPath"
                }
            }
        } catch {
            Write-Warning "Could not remove registry key $keyPath`: $($_.Exception.Message)"
        }
    }
}

function Remove-Shortcuts {
    Write-Step "Removing Shortcuts"
    
    $shortcutPaths = @(
        [System.IO.Path]::Combine([Environment]::GetFolderPath("Desktop"), "$ProductName.lnk"),
        [System.IO.Path]::Combine([Environment]::GetFolderPath("StartMenu"), "Programs", $ProductName),
        [System.IO.Path]::Combine([Environment]::GetFolderPath("CommonDesktopDirectory"), "$ProductName.lnk"),
        [System.IO.Path]::Combine([Environment]::GetFolderPath("CommonStartMenu"), "Programs", $ProductName)
    )
    
    foreach ($path in $shortcutPaths) {
        try {
            if (Test-Path $path) {
                if ((Get-Item $path).PSIsContainer) {
                    Remove-Item -Path $path -Recurse -Force
                } else {
                    Remove-Item -Path $path -Force
                }
                Write-Info "Removed shortcut: $path"
            }
        } catch {
            Write-Warning "Could not remove shortcut $path`: $($_.Exception.Message)"
        }
    }
}

function Remove-EnvironmentPath {
    Write-Step "Removing from System PATH"
    
    try {
        $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
        $pathEntries = $currentPath -split ";"
        $filteredEntries = $pathEntries | Where-Object { $_ -notlike "*$ProductName*" }
        
        if ($filteredEntries.Count -lt $pathEntries.Count) {
            $newPath = $filteredEntries -join ";"
            [Environment]::SetEnvironmentVariable("PATH", $newPath, "Machine")
            Write-Info "Removed $ProductName from system PATH"
        }
    } catch {
        Write-Warning "Could not modify system PATH: $($_.Exception.Message)"
    }
}

function Remove-UserData {
    Write-Step "Removing User Data"
    
    $dataDirs = @($AppDataDir, $LocalAppDataDir, $ProgramDataDir)
    
    foreach ($dir in $dataDirs) {
        if (Test-Path $dir) {
            try {
                $size = (Get-ChildItem $dir -Recurse | Measure-Object -Property Length -Sum).Sum
                $sizeFormatted = "{0:N2} MB" -f ($size / 1MB)
                
                Remove-Item -Path $dir -Recurse -Force
                Write-Info "Removed data directory: $dir [$sizeFormatted]"
            } catch {
                Write-Warning "Could not remove data directory $dir`: $($_.Exception.Message)"
            }
        }
    }
}

function Remove-Services {
    Write-Step "Removing Windows Services"
    
    $services = Get-Service | Where-Object { $_.Name -like "*$ProductName*" -or $_.DisplayName -like "*$ProductName*" }
    
    foreach ($service in $services) {
        try {
            Write-Info "Stopping service: $($service.Name)"
            Stop-Service -Name $service.Name -Force -ErrorAction SilentlyContinue
            
            Write-Info "Removing service: $($service.Name)"
            & sc.exe delete $service.Name
            
            Write-Info "Removed service: $($service.Name)"
        } catch {
            Write-Warning "Could not remove service $($service.Name): $($_.Exception.Message)"
        }
    }
}

function Remove-ScheduledTasks {
    Write-Step "Removing Scheduled Tasks"
    
    try {
        $tasks = Get-ScheduledTask | Where-Object { $_.TaskName -like "*$ProductName*" }
        
        foreach ($task in $tasks) {
            Unregister-ScheduledTask -TaskName $task.TaskName -Confirm:$false
            Write-Info "Removed scheduled task: $($task.TaskName)"
        }
    } catch {
        Write-Warning "Could not remove scheduled tasks: $($_.Exception.Message)"
    }
}

function Get-DataSizeInfo {
    $totalSize = 0
    $directories = @()
    
    foreach ($dir in @($AppDataDir, $LocalAppDataDir, $ProgramDataDir)) {
        if (Test-Path $dir) {
            $size = (Get-ChildItem $dir -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
            $totalSize += $size
            $directories += @{
                Path = $dir
                Size = $size
                SizeFormatted = "{0:N2} MB" -f ($size / 1MB)
            }
        }
    }
    
    return @{
        TotalSize = $totalSize
        TotalSizeFormatted = "{0:N2} MB" -f ($totalSize / 1MB)
        Directories = $directories
    }
}

# Main execution
try {
    Write-Step "Timekeeper Complete Uninstaller"
    
    # Check for admin privileges
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    
    if (-not $isAdmin) {
        Write-Warning "Running without administrator privileges. Some operations may fail."
        if (-not $Force) {
            $response = Read-Host "Continue anyway? (y/N)"
            if ($response -ne 'y' -and $response -ne 'Y') {
                Write-Info "Uninstall cancelled."
                exit 0
            }
        }
    }
    
    # Discover installed versions
    Write-Step "Discovering Installed Versions"
    $installedVersions = Get-InstalledVersions
    
    if ($installedVersions.Count -eq 0) {
        Write-Warning "No installed versions of $ProductName found."
    } else {
        Write-Info "Found $($installedVersions.Count) installed version(s):"
        foreach ($version in $installedVersions) {
            Write-Host "  • $($version.Name) - $($version.Type)" -ForegroundColor Yellow
        }
    }
    
    # Check user data
    $dataInfo = Get-DataSizeInfo
    if ($dataInfo.Directories.Count -gt 0) {
        Write-Info "Found user data [$($dataInfo.TotalSizeFormatted)]:"
        foreach ($dir in $dataInfo.Directories) {
            Write-Host "  • $($dir.Path) [$($dir.SizeFormatted)]" -ForegroundColor Yellow
        }
    }
    
    # Confirmation
    if (-not $Force) {
        Write-Host "`nThis will remove:" -ForegroundColor Yellow
        Write-Host "• All installed versions of $ProductName" -ForegroundColor Yellow
        Write-Host "• All shortcuts and registry entries" -ForegroundColor Yellow
        Write-Host "• Environment PATH modifications" -ForegroundColor Yellow
        Write-Host "• Windows services and scheduled tasks" -ForegroundColor Yellow
        
        if ($RemoveUserData -or $RemoveAllData) {
            Write-Host "• User data and settings [$($dataInfo.TotalSizeFormatted)]" -ForegroundColor Red
        }
        
        Write-Host ""
        $response = Read-Host "Are you sure you want to continue? (y/N)"
        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Info "Uninstall cancelled."
            exit 0
        }
    }
    
    # Stop any running processes
    Write-Step "Stopping Running Processes"
    $processes = Get-Process | Where-Object { $_.ProcessName -like "*timekeeper*" -or $_.ProcessName -like "*Timekeeper*" }
    foreach ($process in $processes) {
        try {
            Write-Info "Stopping process: $($process.ProcessName) (PID: $($process.Id))"
            $process.Kill()
            $process.WaitForExit(5000)
        } catch {
            Write-Warning "Could not stop process $($process.ProcessName): $($_.Exception.Message)"
        }
    }
    
    # Remove services first
    Remove-Services
    
    # Remove scheduled tasks
    Remove-ScheduledTasks
    
    # Remove installed versions
    if ($installedVersions.Count -gt 0) {
        Write-Step "Uninstalling Applications"
        Remove-InstalledVersions $installedVersions
    }
    
    # Remove registry entries
    Remove-RegistryEntries
    
    # Remove shortcuts
    Remove-Shortcuts
    
    # Remove from PATH
    Remove-EnvironmentPath
    
    # Remove user data if requested
    if ($RemoveUserData -or $RemoveAllData) {
        Remove-UserData
    }
    
    # Final cleanup - remove any remaining installation directories
    Write-Step "Final Cleanup"
    $commonInstallPaths = @(
        "${env:ProgramFiles}\$ProductName",
        "${env:ProgramFiles(x86)}\$ProductName",
        "${env:LOCALAPPDATA}\Programs\$ProductName"
    )
    
    foreach ($path in $commonInstallPaths) {
        if (Test-Path $path) {
            try {
                Remove-Item -Path $path -Recurse -Force
                Write-Info "Removed remaining directory: $path"
            } catch {
                Write-Warning "Could not remove directory $path`: $($_.Exception.Message)"
            }
        }
    }
    
    Write-Step "Uninstall Summary"
    Write-Info "✅ $ProductName has been completely removed from this system"
    
    if (-not ($RemoveUserData -or $RemoveAllData) -and $dataInfo.Directories.Count -gt 0) {
        Write-Warning "User data was preserved [$($dataInfo.TotalSizeFormatted)]"
        Write-Host "To remove user data, run with -RemoveUserData or -RemoveAllData" -ForegroundColor Yellow
    }
    
    Write-Host "`n🔄 A system restart is recommended to complete the removal process." -ForegroundColor Cyan
    
    if (-not $Force) {
        $response = Read-Host "Restart now? (y/N)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            Restart-Computer -Force
        }
    }
    
} catch {
    Write-Error "Uninstall failed: $($_.Exception.Message)"
    if ($Verbose) {
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
    }
    exit 1
}
