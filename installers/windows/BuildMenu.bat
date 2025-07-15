@echo off
REM Timekeeper Windows Installer Builder
REM Simple batch wrapper for PowerShell scripts

setlocal EnableDelayedExpansion

echo.
echo ===============================================
echo  Timekeeper Windows Installer Builder
echo ===============================================
echo.

REM Check for PowerShell
where powershell >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: PowerShell not found!
    echo Please install PowerShell and try again.
    pause
    exit /b 1
)

REM Get script directory
set "SCRIPT_DIR=%~dp0"
set "BUILD_SCRIPT=%SCRIPT_DIR%Build.ps1"

REM Check if build script exists
if not exist "%BUILD_SCRIPT%" (
    echo ERROR: Build script not found!
    echo Expected: %BUILD_SCRIPT%
    pause
    exit /b 1
)

echo Available Options:
echo.
echo 1. Build x64 installers (MSI + Portable)
echo 2. Build all architectures (x64, x86, ARM64)
echo 3. Build MSI installers only
echo 4. Build portable packages only
echo 5. Clean build and rebuild all
echo 6. Test installation
echo 7. Uninstall Timekeeper
echo 8. Show help
echo 9. Exit
echo.

set /p choice="Enter your choice (1-9): "

if "%choice%"=="1" (
    echo Building x64 installers...
    powershell -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -Architecture x64
) else if "%choice%"=="2" (
    echo Building all architectures...
    powershell -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -Architecture all
) else if "%choice%"=="3" (
    echo Building MSI installers only...
    powershell -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -Type msi
) else if "%choice%"=="4" (
    echo Building portable packages only...
    powershell -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -Type portable
) else if "%choice%"=="5" (
    echo Clean building all...
    powershell -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -Clean
) else if "%choice%"=="6" (
    echo Testing installation...
    powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\Test-Installation.ps1"
) else if "%choice%"=="7" (
    echo.
    echo WARNING: This will remove Timekeeper from your system!
    set /p confirm="Are you sure? (y/N): "
    if /i "!confirm!"=="y" (
        powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\Uninstall-Timekeeper.ps1"
    ) else (
        echo Uninstall cancelled.
    )
) else if "%choice%"=="8" (
    powershell -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -Help
) else if "%choice%"=="9" (
    echo Goodbye!
    exit /b 0
) else (
    echo Invalid choice. Please try again.
    pause
    goto :start
)

echo.
echo Done! Check the dist\windows\ folder for output files.
pause
