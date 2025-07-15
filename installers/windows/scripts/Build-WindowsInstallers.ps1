# Timekeeper Windows Installer Builder
# Builds MSI installers and portable executables for multiple architectures
# Uses WiX Toolset for MSI generation

param(
    [string]$Architecture = "all",  # x64, x86, arm64, or all
    [string]$Type = "all",          # msi, portable, or all
    [switch]$Clean,                 # Clean build directories
    [switch]$SkipBuild,            # Skip building the application
    [switch]$Verbose
)

# Script configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$WindowsInstallersDir = Split-Path -Parent $ScriptDir
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $WindowsInstallersDir)
$ConfigPath = Join-Path $WindowsInstallersDir "build-config.json"
$OutputDir = Join-Path $ProjectRoot "dist\windows"
$CliProject = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj"

# Load configuration
if (-not (Test-Path $ConfigPath)) {
    Write-Error "Configuration file not found: $ConfigPath"
    exit 1
}

$Config = Get-Content $ConfigPath | ConvertFrom-Json
$SupportedArchitectures = $Config.architectures.name

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
    Write-Host "`n📦 $message" -ForegroundColor Cyan
    Write-Host "═" * ($message.Length + 4) -ForegroundColor Cyan
}

function Test-WixToolset {
    # Try WiX 4.0+ first (new dotnet tool)
    try {
        $wixVersion = & wix --version 2>&1
        if ($wixVersion -and $wixVersion -notlike "*not found*" -and $wixVersion -notlike "*not recognized*") {
            return @{ Version = "4.0+"; Command = "wix" }
        }
    } catch { }
    
    # Try WiX 3.x (classic toolset)
    try {
        $null = Get-Command "heat.exe" -ErrorAction Stop
        $null = Get-Command "candle.exe" -ErrorAction Stop
        $null = Get-Command "light.exe" -ErrorAction Stop
        return @{ Version = "3.x"; Command = "classic" }
    } catch { }
    
    return $null
}

function Test-DotNetVersion {
    try {
        $dotnetVersion = & dotnet --version
        if ($dotnetVersion -lt "9.0") {
            Write-Warning "Recommended .NET version is 9.0 or higher. Current: $dotnetVersion"
        }
        return $true
    } catch {
        return $false
    }
}

function New-DirectoryIfNotExists($path) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        if ($Verbose) { Write-Host "Created directory: $path" -ForegroundColor Gray }
    }
}

function Remove-DirectoryIfExists($path) {
    if (Test-Path $path) {
        Remove-Item -Path $path -Recurse -Force
        if ($Verbose) { Write-Host "Removed directory: $path" -ForegroundColor Gray }
    }
}

function Build-Application($arch, $outputPath) {
    $archConfig = $Config.architectures | Where-Object { $_.name -eq $arch }
    
    Write-Info "Building Timekeeper CLI for $($archConfig.displayName)..."
    
    $publishArgs = @(
        "publish", $CliProject,
        "-c", "Release",
        "-o", $outputPath,
        "--self-contained", "true",
        "-r", $archConfig.runtime,
        "/p:PublishSingleFile=true",
        "/p:PublishTrimmed=false",
        "/p:IncludeNativeLibrariesForSelfExtract=true",
        "/p:IncludeAllContentForSelfExtract=true"
    )
    
    if ($Verbose) {
        $publishArgs += "--verbosity", "detailed"
    } else {
        $publishArgs += "--verbosity", "minimal"
    }
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for architecture: $arch"
    }
    
    # Copy icon file to build directory
    $iconSource = Join-Path $ProjectRoot "res\TimeKeeper.ico"
    $iconDest = Join-Path $outputPath "TimeKeeper.ico"
    if (Test-Path $iconSource) {
        Copy-Item -Path $iconSource -Destination $iconDest -Force
        Write-Info "Icon copied to build directory"
    } else {
        Write-Warning "Icon file not found: $iconSource"
    }
    
    Write-Info "Build completed for $($archConfig.displayName)"
}

function New-PortablePackage($arch) {
    $archConfig = $Config.architectures | Where-Object { $_.name -eq $arch }
    $buildPath = Join-Path $OutputDir "build\$arch"
    $portablePath = Join-Path $OutputDir "portable"
    
    New-DirectoryIfNotExists $portablePath
    
    $packageName = "Timekeeper-$($Config.product.version)-$arch-portable"
    $packagePath = Join-Path $portablePath $packageName
    
    New-DirectoryIfNotExists $packagePath
    
    # Copy main executable
    Copy-Item -Path (Join-Path $buildPath "Timekeeper.CLI.exe") -Destination (Join-Path $packagePath "timekeeper.exe")
    
    # Copy icon if available
    $iconPath = Join-Path $buildPath "TimeKeeper.ico"
    if (Test-Path $iconPath) {
        Copy-Item -Path $iconPath -Destination (Join-Path $packagePath "TimeKeeper.ico")
    }
    
    # Create launcher batch file
    $batchContent = @"
@echo off
REM Timekeeper Portable Launcher
REM Version: $($Config.product.version)
REM Architecture: $($archConfig.displayName)

setlocal EnableDelayedExpansion

REM Set application directory
set "APP_DIR=%~dp0"
set "DATA_DIR=%APPDATA%\Timekeeper"

REM Create data directory if it doesn't exist
if not exist "%DATA_DIR%" mkdir "%DATA_DIR%"

REM Launch Timekeeper with all arguments
"%APP_DIR%timekeeper.exe" %*

endlocal
"@
    
    Set-Content -Path (Join-Path $packagePath "timekeeper.bat") -Value $batchContent -Encoding UTF8
    
    # Create tk alias batch file
    $tkBatchContent = @"
@echo off
REM Timekeeper Quick Launcher (tk)
REM Version: $($Config.product.version)

"%~dp0timekeeper.exe" %*
"@
    
    Set-Content -Path (Join-Path $packagePath "tk.bat") -Value $tkBatchContent -Encoding UTF8
    
    # Create README
    $readmeContent = @"
# Timekeeper Portable - $($archConfig.displayName)

## Quick Start
1. Run `timekeeper.bat` to start the application
2. Use `tk.bat` for quick access (short alias)
3. Or run `timekeeper.exe` directly

## Available Commands
- `timekeeper.bat [command] [options]` - Full launcher
- `tk.bat [command] [options]` - Quick alias
- `timekeeper.exe [command] [options]` - Direct execution

## Features
- No installation required
- Self-contained executable
- Portable data storage in %APPDATA%\Timekeeper
- Includes application icon

## Version Information
- Version: $($Config.product.version)
- Architecture: $($archConfig.displayName)
- Runtime: $($archConfig.runtime)

## Support
- Website: $($Config.product.website)
- Issues: $($Config.product.supportUrl)

## Usage Examples
```
tk --help
tk time start "Working on project"
tk todo add "Review documentation"
tk report daily
```

For complete help: `tk --help`
"@
    
    Set-Content -Path (Join-Path $packagePath "README.md") -Value $readmeContent -Encoding UTF8
    
    # Create ZIP package
    $zipPath = Join-Path $portablePath "$packageName.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($packagePath, $zipPath)
    
    # Remove temporary directory
    Remove-Item $packagePath -Recurse -Force
    
    Write-Info "Portable package created: $zipPath"
    return $zipPath
}

function New-WixSourceFile($arch) {
    $archConfig = $Config.architectures | Where-Object { $_.name -eq $arch }
    $wixDir = Join-Path $WindowsInstallersDir "wix"
    $wxsPath = Join-Path $wixDir "Timekeeper-$arch.wxs"
    
    New-DirectoryIfNotExists $wixDir
    
    # Generate unique GUIDs for each architecture
    $productGuid = [System.Guid]::NewGuid().ToString().ToUpper()
    
    # Get the build path for source files
    $buildPath = Join-Path $OutputDir "build\$arch"
    
    # Use correct namespace based on WiX version
    $wixNamespace = if ($wixInfo.Version -eq "4.0+") {
        "http://wixtoolset.org/schemas/v4/wxs"
    } else {
        "http://schemas.microsoft.com/wix/2006/wi"
    }
    
    if ($wixInfo.Version -eq "4.0+") {
        # WiX 4.0+ uses Package as root element with different attributes
        $wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="$wixNamespace">
  <Package Name="$($Config.product.name) ($($archConfig.displayName))" 
           Language="1033" 
           Version="$($Config.product.version).0" 
           Manufacturer="$($Config.product.manufacturer)"
           InstallerVersion="500"
           Compressed="yes">

    <MediaTemplate EmbedCab="yes" />

    <!-- UI Configuration -->
    <!-- Note: WiX 4.0+ uses different UI setup -->

    <!-- Properties -->
    <Property Id="ARPPRODUCTICON" Value="TimekeeperIcon.ico" />
    <Property Id="ARPHELPLINK" Value="$($Config.product.supportUrl)" />
    <Property Id="ARPURLINFOABOUT" Value="$($Config.product.website)" />
    <Property Id="ARPNOMODIFY" Value="1" />
    <Property Id="ARPNOREPAIR" Value="1" />

    <!-- Features -->
    <Feature Id="MainFeature" Title="$($Config.product.name)" Level="1" Description="Core application files">
      <ComponentGroupRef Id="ApplicationFiles" />
      <ComponentRef Id="RegistryEntries" />
"@
    } else {
        # WiX 3.x uses Product as root element
        $wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="$wixNamespace">
  <Product Id="{$productGuid}" 
           Name="$($Config.product.name) ($($archConfig.displayName))" 
           Language="1033" 
           Version="$($Config.product.version).0" 
           Manufacturer="$($Config.product.manufacturer)">
    
    <Package InstallerVersion="200" 
             Compressed="yes" 
             InstallScope="perMachine" 
             Platform="$($archConfig.platformId)"
             Description="$($Config.product.description) - $($archConfig.displayName)" />

    <MediaTemplate EmbedCab="yes" />

    <!-- UI Configuration -->
    <UI>
      <UIRef Id="WixUI_InstallDir" />
      <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />
    </UI>

    <!-- Properties -->
    <Property Id="ARPPRODUCTICON" Value="TimekeeperIcon.ico" />
    <Property Id="ARPHELPLINK" Value="$($Config.product.supportUrl)" />
    <Property Id="ARPURLINFOABOUT" Value="$($Config.product.website)" />
    <Property Id="ARPNOMODIFY" Value="1" />
    <Property Id="ARPNOREPAIR" Value="1" />

    <!-- Features -->
    <Feature Id="MainFeature" Title="$($Config.product.name)" Level="1" Description="Core application files">
      <ComponentGroupRef Id="ApplicationFiles" />
      <ComponentRef Id="RegistryEntries" />
"@
    }

    if ($Config.features.addToPath) {
        $wxsContent += "`n      <ComponentRef Id=`"EnvironmentPath`" />"
    }
    
    if ($Config.features.startMenuShortcut) {
        $wxsContent += "`n      <ComponentRef Id=`"StartMenuShortcut`" />"
    }
    
    if ($Config.features.desktopShortcut) {
        $wxsContent += "`n      <ComponentRef Id=`"DesktopShortcut`" />"
    }

    $wxsContent += @"

    </Feature>

    <!-- Use StandardDirectories for WiX 4.0+ -->
    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="$($Config.product.name)">
        <Component Id="MainExecutable" Guid="*">
          <File Id="TimekeeperEXE" 
                Source="$buildPath\Timekeeper.CLI.exe" 
                KeyPath="yes" 
                Name="timekeeper.exe"/>
        </Component>
        
        <Component Id="LauncherBat" Guid="*">
          <File Id="TimekeeperBAT" 
                Source="$buildPath\timekeeper.bat" 
                KeyPath="yes" />
        </Component>
        
        <Component Id="TkAliasBat" Guid="*">
          <File Id="TkBAT" 
                Source="$buildPath\tk.bat" 
                KeyPath="yes" />
        </Component>
        
        <Component Id="ApplicationIcon" Guid="*">
          <File Id="TimekeeperICO" 
                Source="$buildPath\TimeKeeper.ico" 
                KeyPath="yes" 
                Name="TimekeeperIcon.ico"/>
        </Component>
      </Directory>
    </StandardDirectory>
"@

    if ($Config.features.startMenuShortcut) {
        $wxsContent += @"
      
    <!-- Start Menu using StandardDirectory -->
    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="$($Config.product.name)">
        <Component Id="StartMenuShortcut" Guid="*">
          <Shortcut Id="ApplicationStartMenuShortcut"
                    Name="$($Config.product.name)"
                    Description="$($Config.product.description)"
                    Target="[INSTALLFOLDER]timekeeper.exe"
                    WorkingDirectory="INSTALLFOLDER" />
          <Shortcut Id="UninstallShortcut"
                    Name="Uninstall $($Config.product.name)"
                    Description="Uninstalls $($Config.product.name)"
                    Target="[System64Folder]msiexec.exe"
                    Arguments="/x [ProductCode]" />
          <RemoveFolder Id="ApplicationProgramsFolder" On="uninstall" />
          <RegistryValue Root="HKCU" 
                        Key="Software\$($Config.product.name)" 
                        Name="StartMenuInstalled" 
                        Type="integer" 
                        Value="1" 
                        KeyPath="yes" />
        </Component>
      </Directory>
    </StandardDirectory>
"@
    }

    if ($Config.features.desktopShortcut) {
        $wxsContent += @"
      
    <!-- Desktop Shortcut using StandardDirectory -->
    <StandardDirectory Id="DesktopFolder">
      <Component Id="DesktopShortcut" Guid="*">
        <Shortcut Id="DesktopShortcut"
                  Name="$($Config.product.name)"
                  Description="$($Config.product.description)"
                  Target="[INSTALLFOLDER]timekeeper.exe"
                  WorkingDirectory="INSTALLFOLDER" />
        <RegistryValue Root="HKCU" 
                      Key="Software\$($Config.product.name)" 
                      Name="DesktopShortcutInstalled" 
                      Type="integer" 
                      Value="1" 
                      KeyPath="yes" />
      </Component>
    </StandardDirectory>
"@
    }

    $wxsContent += @"

    <!-- Registry Entries -->
    <Component Id="RegistryEntries" Guid="*" Directory="INSTALLFOLDER">
      <RegistryKey Root="HKLM" Key="Software\$($Config.product.name)">
        <RegistryValue Name="InstallDir" Type="string" Value="[INSTALLFOLDER]" />
        <RegistryValue Name="Version" Type="string" Value="$($Config.product.version)" />
        <RegistryValue Name="Architecture" Type="string" Value="$($archConfig.name)" />
        <RegistryValue Name="DisplayName" Type="string" Value="$($Config.product.name) ($($archConfig.displayName))" />
      </RegistryKey>
      <RegistryKey Root="HKLM" Key="Software\Microsoft\Windows\CurrentVersion\Uninstall\{$productGuid}">
        <RegistryValue Name="DisplayName" Type="string" Value="$($Config.product.name) ($($archConfig.displayName))" />
        <RegistryValue Name="DisplayVersion" Type="string" Value="$($Config.product.version)" />
        <RegistryValue Name="Publisher" Type="string" Value="$($Config.product.manufacturer)" />
        <RegistryValue Name="InstallLocation" Type="string" Value="[INSTALLFOLDER]" />
        <RegistryValue Name="HelpLink" Type="string" Value="$($Config.product.supportUrl)" />
        <RegistryValue Name="URLInfoAbout" Type="string" Value="$($Config.product.website)" />
        <RegistryValue Name="NoModify" Type="integer" Value="1" />
        <RegistryValue Name="NoRepair" Type="integer" Value="1" />
      </RegistryKey>
    </Component>
"@

    if ($Config.features.addToPath) {
        $wxsContent += @"
      
    <Component Id="EnvironmentPath" Guid="{A1B2C3D4-E5F6-7890-ABCD-123456789ABC}" Directory="INSTALLFOLDER">
      <Environment Id="PATH" 
                   Name="PATH" 
                   Value="[INSTALLFOLDER]" 
                   Permanent="no" 
                   Part="last" 
                   Action="set" 
                   System="yes" />
    </Component>
"@
    }

    $wxsContent += @"

    <!-- Component Groups -->
    <ComponentGroup Id="ApplicationFiles" Directory="INSTALLFOLDER">
      <ComponentRef Id="MainExecutable" />
      <ComponentRef Id="LauncherBat" />
      <ComponentRef Id="TkAliasBat" />
      <ComponentRef Id="ApplicationIcon" />
    </ComponentGroup>

    <!-- Simple Custom Actions for Cleanup -->
    <CustomAction Id="CleanupUserData" 
                  Directory="INSTALLFOLDER" 
                  ExeCommand="cmd.exe /c echo Cleanup completed" 
                  Execute="deferred" 
                  Impersonate="no" />
    
    <InstallExecuteSequence>
      <Custom Action="CleanupUserData" Before="RemoveFiles" Condition="REMOVE=&quot;ALL&quot;" />
    </InstallExecuteSequence>
"@

    # Close with appropriate element based on WiX version
    if ($wixInfo.Version -eq "4.0+") {
        $wxsContent += "`n  </Package>`n</Wix>"
    } else {
        $wxsContent += "`n  </Product>`n</Wix>"
    }

    $wxsContent += "`n"

    Set-Content -Path $wxsPath -Value $wxsContent -Encoding UTF8
    Write-Info "WiX source file created: $wxsPath"
    return $wxsPath
}

function New-MSIInstaller($arch, $wixInfo) {
    $archConfig = $Config.architectures | Where-Object { $_.name -eq $arch }
    $buildPath = Join-Path $OutputDir "build\$arch"
    $wixDir = Join-Path $WindowsInstallersDir "wix"
    $msiDir = Join-Path $OutputDir "msi"
    
    New-DirectoryIfNotExists $msiDir
    
    # Create launcher batch file for MSI
    $batchContent = @"
@echo off
REM Timekeeper Launcher
"%~dp0timekeeper.exe" %*
"@
    
    $batchPath = Join-Path $buildPath "timekeeper.bat"
    Set-Content -Path $batchPath -Value $batchContent -Encoding UTF8
    
    # Create tk alias batch file for MSI
    $tkBatchContent = @"
@echo off
REM Timekeeper Quick Launcher (tk)
"%~dp0timekeeper.exe" %*
"@
    
    $tkBatchPath = Join-Path $buildPath "tk.bat"
    Set-Content -Path $tkBatchPath -Value $tkBatchContent -Encoding UTF8
    
    # Create license file
    $licenseContent = @"
{\rtf1\ansi\deff0 {\fonttbl {\f0 Times New Roman;}}
\f0\fs24 $($Config.product.name) License Agreement\par
\par
This software is provided "as is" without warranty of any kind.\par
\par
You may use this software for personal and commercial purposes.\par
\par
For support and updates, visit: $($Config.product.website)\par
}
"@
    
    $licensePath = Join-Path $wixDir "License.rtf"
    Set-Content -Path $licensePath -Value $licenseContent -Encoding UTF8
    
    # Generate WiX source
    $wxsPath = New-WixSourceFile $arch
    
    $msiPath = Join-Path $msiDir "Timekeeper-$($Config.product.version)-$arch.msi"
    
    if ($wixInfo.Version -eq "4.0+") {
        # WiX 4.0+ (dotnet tool)
        Write-Info "Building MSI with WiX 4.0+ for $($archConfig.displayName)..."
        
        # Build directly from WXS file
        $buildArgs = @(
            "build",
            $wxsPath,
            "-arch", $archConfig.platformId,
            "-define", "SourceDir=$buildPath",
            "-ext", "WixToolset.UI.wixext",
            "-o", $msiPath
        )
        
        $output = & wix @buildArgs 2>&1
        $wixExitCode = $LASTEXITCODE
        
        # Show only errors (not warnings)
        if ($output) {
            $errorLines = $output | Where-Object { $_ -like "*error*" -and $_ -notlike "*warning*" }
            if ($errorLines) {
                Write-Host $errorLines -ForegroundColor Red
            }
        }
        
    } else {
        # WiX 3.x (classic toolset)
        Write-Info "Building MSI with WiX 3.x for $($archConfig.displayName)..."
        
        $wixObjPath = Join-Path $wixDir "Timekeeper-$arch.wixobj"
        
        Write-Info "Compiling WiX source for $($archConfig.displayName)..."
        & candle.exe -dSourceDir="$buildPath" "$wxsPath" -out "$wixObjPath"
        
        if ($LASTEXITCODE -ne 0) {
            throw "WiX compilation failed for architecture: $arch"
        }
        
        Write-Info "Linking MSI installer for $($archConfig.displayName)..."
        & light.exe "$wixObjPath" -out "$msiPath" -ext WixUIExtension
        $wixExitCode = $LASTEXITCODE
    }
    
    if ($wixExitCode -ne 0) {
        throw "MSI creation failed for architecture: $arch"
    }
    
    # Verify MSI was created successfully
    if (-not (Test-Path $msiPath)) {
        throw "MSI file was not created: $msiPath"
    }
    
    Write-Info "MSI installer created: $msiPath"
    return $msiPath
}

# Main execution
try {
    Write-Step "Timekeeper Windows Installer Builder"
    Write-Host "Building for: $Architecture | Type: $Type" -ForegroundColor Yellow
    
    # Validate prerequisites
    Write-Step "Validating Prerequisites"
    
    if (-not (Test-DotNetVersion)) {
        Write-Error ".NET SDK not found. Please install .NET 9.0 or higher."
        exit 1
    }
    Write-Info ".NET SDK found"
    
    $wixInfo = $null
    if ($Type -eq "msi" -or $Type -eq "all") {
        $wixInfo = Test-WixToolset
        if (-not $wixInfo) {
            Write-Error "WiX Toolset not found. Please install from https://wixtoolset.org/"
            Write-Host "For WiX 4.0+: dotnet tool install -g wix" -ForegroundColor Yellow
            Write-Host "For WiX 3.x: Download from https://wixtoolset.org/releases/" -ForegroundColor Yellow
            exit 1
        }
        Write-Info "WiX Toolset found (Version: $($wixInfo.Version))"
    }
    
    # Clean if requested
    if ($Clean) {
        Write-Step "Cleaning Build Directories"
        Remove-DirectoryIfExists $OutputDir
    }
    
    # Create output directories
    New-DirectoryIfNotExists $OutputDir
    New-DirectoryIfNotExists (Join-Path $OutputDir "build")
    
    # Determine architectures to build
    $archsToBuild = if ($Architecture -eq "all") { $SupportedArchitectures } else { @($Architecture) }
    
    # Validate requested architectures
    foreach ($arch in $archsToBuild) {
        if ($arch -notin $SupportedArchitectures) {
            Write-Error "Unsupported architecture: $arch. Supported: $($SupportedArchitectures -join ', ')"
            exit 1
        }
    }
    
    $results = @()
    
    foreach ($arch in $archsToBuild) {
        $archConfig = $Config.architectures | Where-Object { $_.name -eq $arch }
        
        Write-Step "Building for $($archConfig.displayName)"
        
        if (-not $SkipBuild) {
            $buildPath = Join-Path $OutputDir "build\$arch"
            New-DirectoryIfNotExists $buildPath
            Build-Application $arch $buildPath
        }
        
        if ($Type -eq "portable" -or $Type -eq "all") {
            Write-Host "`n📦 Creating portable package for $($archConfig.displayName)..." -ForegroundColor Cyan
            $portablePath = New-PortablePackage $arch
            $results += @{ Type = "Portable"; Architecture = $arch; Path = $portablePath }
        }
        
        if ($Type -eq "msi" -or $Type -eq "all") {
            Write-Host "`n📦 Creating MSI installer for $($archConfig.displayName)..." -ForegroundColor Cyan
            $msiPath = New-MSIInstaller $arch $wixInfo
            $results += @{ Type = "MSI"; Architecture = $arch; Path = $msiPath }
        }
    }
    
    # Summary
    Write-Step "Build Summary"
    foreach ($result in $results) {
        $size = if (Test-Path $result.Path) { 
            "{0:N2} MB" -f ((Get-Item $result.Path).Length / 1MB)
        } else { 
            "Unknown" 
        }
        Write-Info "$($result.Type) ($($result.Architecture)): $($result.Path) [$size]"
    }
    
    Write-Host "`n✅ Build completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    if ($Verbose) {
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
    }
    exit 1
}
