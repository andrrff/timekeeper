# 🪟 Timekeeper Windows Installers

This directory contains all the necessary tools and scripts to build Windows installers for Timekeeper across multiple architectures.

## 📦 What's Available

### MSI Installers (.msi)
Professional Windows Installer packages with:
- ✅ Proper Windows integration
- ✅ Add/Remove Programs support  
- ✅ System PATH integration
- ✅ Start Menu shortcuts
- ✅ Desktop shortcuts (optional)
- ✅ Clean uninstall process
- ✅ Per-machine installation

### Portable Executables (.zip)
Self-contained portable packages with:
- ✅ No installation required
- ✅ Single executable + launcher
- ✅ Portable data storage
- ✅ Zero system footprint
- ✅ Perfect for USB drives

## 🏗️ Supported Architectures

| Architecture | Description | Compatible With |
|-------------|-------------|-----------------|
| **x64** | 64-bit Intel/AMD | Most modern Windows PCs |
| **x86** | 32-bit Intel/AMD | Legacy systems, compatibility |
| **arm64** | 64-bit ARM | Surface Pro X, ARM-based PCs |

## 🚀 Quick Start

### Prerequisites
1. **[.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** or higher
2. **[WiX Toolset](https://wixtoolset.org/)** (for MSI installers)
3. **PowerShell 5.1** or higher (built into Windows)

### Building Installers

```powershell
# Build everything (all architectures, all types)
.\Build.ps1

# Build specific architecture
.\Build.ps1 -Architecture x64

# Build only MSI installers
.\Build.ps1 -Type msi

# Build only portable packages
.\Build.ps1 -Type portable

# Build ARM64 portable only
.\Build.ps1 -Architecture arm64 -Type portable

# Clean build (removes previous builds)
.\Build.ps1 -Clean
```

### Advanced Building

```powershell
# Use the advanced script directly
.\scripts\Build-WindowsInstallers.ps1 -Architecture all -Type all -Verbose

# Skip application build (if already built)
.\scripts\Build-WindowsInstallers.ps1 -SkipBuild -Type msi
```

## 📁 Directory Structure

```
installers/windows/
├── 📄 Build.ps1                    # Simple build interface
├── 📄 build-config.json           # Build configuration
├── 📄 README.md                   # This file
├── 📂 scripts/
│   ├── 📄 Build-WindowsInstallers.ps1  # Main build script
│   └── 📄 Uninstall-Timekeeper.ps1     # Complete uninstaller
├── 📂 wix/                        # WiX source files (generated)
└── 📂 portable/                   # Portable package templates
```

## 📦 Output Files

After building, you'll find the installers in:

```
dist/windows/
├── 📂 msi/                        # MSI Installers
│   ├── 📄 Timekeeper-1.0.0-x64.msi
│   ├── 📄 Timekeeper-1.0.0-x86.msi
│   └── 📄 Timekeeper-1.0.0-arm64.msi
└── 📂 portable/                   # Portable Packages
    ├── 📄 Timekeeper-1.0.0-x64-portable.zip
    ├── 📄 Timekeeper-1.0.0-x86-portable.zip
    └── 📄 Timekeeper-1.0.0-arm64-portable.zip
```

## 🎯 Installation Options

### MSI Installer Features

When users install via MSI, they get:

- **System Integration**: Added to Windows Programs & Features
- **PATH Environment**: `timekeeper` command available globally
- **Start Menu**: Shortcuts in Start Menu with uninstaller
- **Desktop Shortcut**: Optional desktop shortcut
- **Auto-Updates**: Ready for future update mechanisms
- **Professional Uninstall**: Clean removal via Windows settings

### Portable Package Contents

Each portable ZIP contains:
- `timekeeper.exe` - Main application
- `timekeeper.bat` - Convenient launcher
- `README.md` - Usage instructions

## 🗑️ Complete Uninstallation

To completely remove Timekeeper from a system:

```powershell
# Remove application only (keep user data)
.\scripts\Uninstall-Timekeeper.ps1

# Remove everything including user data
.\scripts\Uninstall-Timekeeper.ps1 -RemoveAllData

# Remove with user confirmation prompts
.\scripts\Uninstall-Timekeeper.ps1 -RemoveUserData

# Silent removal (no prompts)
.\scripts\Uninstall-Timekeeper.ps1 -RemoveAllData -Force
```

The uninstaller removes:
- ✅ All installed versions (MSI and manual)
- ✅ Registry entries
- ✅ Environment PATH modifications  
- ✅ Shortcuts (Start Menu, Desktop)
- ✅ Windows Services (if any)
- ✅ Scheduled Tasks (if any)
- ✅ User data (optional)

## ⚙️ Configuration

Edit `build-config.json` to customize:

```json
{
  "product": {
    "name": "Timekeeper",
    "version": "1.0.0",
    "manufacturer": "Your Company"
  },
  "features": {
    "systemTray": true,
    "desktopShortcut": true,
    "addToPath": true
  }
}
```

## 🔧 Troubleshooting

### WiX Toolset Issues
```powershell
# Check if WiX is installed
Get-Command candle.exe -ErrorAction SilentlyContinue

# Install WiX via Chocolatey (if you have it)
choco install wixtoolset

# Or download from: https://wixtoolset.org/
```

### .NET SDK Issues  
```powershell
# Check .NET version
dotnet --version

# List installed SDKs
dotnet --list-sdks

# Download from: https://dotnet.microsoft.com/download
```

### Build Errors
```powershell
# Clean build with verbose output
.\Build.ps1 -Clean -Verbose

# Check build logs in dist/windows/build/
```

## 🛠️ Development

### Adding New Features

1. Edit `build-config.json` for new features
2. Modify WiX templates in the build script
3. Update uninstaller if needed
4. Test on clean Windows VM

### Supporting New Architectures

1. Add architecture to `build-config.json`
2. Ensure .NET runtime support
3. Test on target architecture

### Custom Installers

The build system is modular. You can:
- Create custom WiX templates
- Add post-build scripts
- Integrate with CI/CD pipelines
- Add code signing

## 📋 Best Practices

### For MSI Distribution
- Code sign your MSI files
- Test on clean Windows installations
- Provide uninstall instructions
- Document system requirements

### For Portable Distribution
- Include clear README files
- Test write permissions
- Document data storage locations
- Provide migration guides

## 🤝 Contributing

When contributing to the installer system:

1. Test on multiple Windows versions
2. Verify both install and uninstall processes
3. Check compatibility with existing installations
4. Update documentation

## 📄 License

This installer system is part of the Timekeeper project.
For license information, see the main project LICENSE file.
