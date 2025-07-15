# WiX Toolset Installer Configuration for Timekeeper CLI
# This creates a proper Windows MSI installer

Write-Host "Building MSI Installer for Timekeeper CLI..." -ForegroundColor Cyan

# Check if WiX is installed
try {
    $wixPath = (Get-Command "heat.exe" -ErrorAction Stop).Source
    Write-Host "WiX Toolset found at: $wixPath" -ForegroundColor Green
} catch {
    Write-Host "WiX Toolset not found. Please install WiX Toolset from https://wixtoolset.org/" -ForegroundColor Red
    Write-Host "After installation, restart PowerShell and try again." -ForegroundColor Yellow
    exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$OutputDir = Join-Path $ProjectRoot "dist"
$InstallerDir = Join-Path $ScriptDir "installer"

# Create directories
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }
if (-not (Test-Path $InstallerDir)) { New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null }

# Build the CLI project first
Write-Host "Building Timekeeper CLI..." -ForegroundColor Yellow
Set-Location $ProjectRoot
$CliProject = Join-Path $ProjectRoot "src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj"
& dotnet publish $CliProject -c Release -o "$OutputDir\app" --self-contained true -r win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create WiX source file
$wxsContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" 
           Name="Timekeeper CLI" 
           Language="1033" 
           Version="1.0.0.0" 
           Manufacturer="Timekeeper" 
           UpgradeCode="12345678-1234-1234-1234-123456789012">
    
    <Package InstallerVersion="200" 
             Compressed="yes" 
             InstallScope="perMachine" 
             Description="Timekeeper - Advanced Time Management System" />

    <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed." />
    <MediaTemplate EmbedCab="yes" />

    <Feature Id="ProductFeature" Title="Timekeeper CLI" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
      <ComponentRef Id="EnvironmentComponent" />
    </Feature>

    <!-- Installation directory structure -->
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="Timekeeper">
          <Component Id="TimekeeperCLI" Guid="*">
            <File Id="TimekeeperCLIExe" 
                  Source="`$(var.SourceDir)\app\Timekeeper.CLI.exe" 
                  KeyPath="yes" />
          </Component>
          
          <Component Id="TkBatchFile" Guid="*">
            <File Id="TkBat" 
                  Source="`$(var.SourceDir)\tk.bat" 
                  KeyPath="yes" />
          </Component>
        </Directory>
      </Directory>
      
      <!-- Start Menu -->
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="Timekeeper">
          <Component Id="ApplicationShortcut" Guid="*">
            <Shortcut Id="ApplicationStartMenuShortcut"
                      Name="Timekeeper"
                      Description="Timekeeper - Advanced Time Management System"
                      Target="[INSTALLFOLDER]Timekeeper.CLI.exe"
                      WorkingDirectory="INSTALLFOLDER" />
            <RemoveFolder Id="ApplicationProgramsFolder" On="uninstall" />
            <RegistryValue Root="HKCU" 
                          Key="Software\Timekeeper" 
                          Name="installed" 
                          Type="integer" 
                          Value="1" 
                          KeyPath="yes" />
          </Component>
        </Directory>
      </Directory>
      
      <!-- Desktop Shortcut -->
      <Directory Id="DesktopFolder" Name="Desktop">
        <Component Id="DesktopShortcut" Guid="*">
          <Shortcut Id="DesktopShortcut"
                    Name="Timekeeper"
                    Description="Timekeeper - Advanced Time Management System"
                    Target="[INSTALLFOLDER]Timekeeper.CLI.exe"
                    WorkingDirectory="INSTALLFOLDER" />
          <RegistryValue Root="HKCU" 
                        Key="Software\Timekeeper" 
                        Name="desktop" 
                        Type="integer" 
                        Value="1" 
                        KeyPath="yes" />
        </Component>
      </Directory>
    </Directory>

    <!-- Environment Variable Component -->
    <DirectoryRef Id="TARGETDIR">
      <Component Id="EnvironmentComponent" Guid="*">
        <Environment Id="PATH" 
                     Name="PATH" 
                     Value="[INSTALLFOLDER]" 
                     Permanent="no" 
                     Part="last" 
                     Action="set" 
                     System="yes" />
        <RegistryValue Root="HKLM" 
                      Key="Software\Timekeeper" 
                      Name="InstallDir" 
                      Type="string" 
                      Value="[INSTALLFOLDER]" 
                      KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <!-- Component Group for all files -->
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <ComponentRef Id="TimekeeperCLI" />
      <ComponentRef Id="TkBatchFile" />
    </ComponentGroup>

    <!-- UI Configuration -->
    <UI Id="WixUI_InstallDir">
      <UIRef Id="WixUI_InstallDir" />
    </UI>
    
    <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />
    
    <!-- License agreement -->
    <WixVariable Id="WixUILicenseRtf" Value="License.rtf" />
    
  </Product>
</Wix>
"@

$wxsPath = Join-Path $InstallerDir "Timekeeper.wxs"
Set-Content -Path $wxsPath -Value $wxsContent

# Create tk.bat file for the installer
$tkBatContent = @"
@echo off
"%~dp0Timekeeper.CLI.exe" %*
"@
$tkBatPath = Join-Path $OutputDir "tk.bat"
Set-Content -Path $tkBatPath -Value $tkBatContent

# Create a simple license file
$licenseContent = @"
{\rtf1\ansi\deff0 {\fonttbl {\f0 Times New Roman;}}
\f0\fs24 Timekeeper CLI License Agreement\par
\par
This software is provided "as is" without warranty of any kind.\par
\par
You may use this software for personal and commercial purposes.\par
}
"@
$licensePath = Join-Path $InstallerDir "License.rtf"
Set-Content -Path $licensePath -Value $licenseContent

Write-Host "WiX source files created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To build the MSI installer, run the following commands:" -ForegroundColor Yellow
Write-Host "1. candle.exe -dSourceDir=`"$OutputDir`" `"$wxsPath`" -out `"$InstallerDir\Timekeeper.wixobj`"" -ForegroundColor White
Write-Host "2. light.exe `"$InstallerDir\Timekeeper.wixobj`" -out `"$OutputDir\TimekeeperInstaller.msi`" -ext WixUIExtension" -ForegroundColor White
Write-Host ""
Write-Host "Or run the build-msi.ps1 script to automate this process." -ForegroundColor Cyan
