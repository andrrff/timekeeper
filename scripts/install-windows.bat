@echo off
echo ========================================
echo    Timekeeper CLI Installer v1.0
echo ========================================
echo.

:: Check for administrator privileges
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with administrator privileges...
) else (
    echo This installer requires administrator privileges.
    echo Please run as administrator.
    pause
    exit /b 1
)

:: Set installation directory
set "INSTALL_DIR=%ProgramFiles%\Timekeeper"
set "TIMEKEEPER_EXE=%INSTALL_DIR%\Timekeeper.CLI.exe"

echo Installing Timekeeper CLI...
echo Installation directory: %INSTALL_DIR%
echo.

:: Create installation directory
if not exist "%INSTALL_DIR%" (
    echo Creating installation directory...
    mkdir "%INSTALL_DIR%"
)

:: Build the project in Release mode
echo Building Timekeeper CLI...
cd /d "%~dp0.."
dotnet build src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj -c Release
if %errorLevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

:: Publish the application
echo Publishing application...
dotnet publish src\CLI\Timekeeper.CLI\Timekeeper.CLI.csproj -c Release -o "%INSTALL_DIR%" --self-contained false
if %errorLevel% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

:: Copy additional files
echo Copying additional files...
if exist "res\TimeKeeper.ico" copy "res\TimeKeeper.ico" "%INSTALL_DIR%\"
if exist "README.md" copy "README.md" "%INSTALL_DIR%\"

:: Create batch wrapper for the tk command
echo Creating tk command wrapper...
echo @echo off > "%INSTALL_DIR%\tk.bat"
echo chcp 65001 ^>nul 2^>^&1 >> "%INSTALL_DIR%\tk.bat"
echo "%INSTALL_DIR%\Timekeeper.CLI.exe" %%* >> "%INSTALL_DIR%\tk.bat"

:: Add to PATH environment variable
echo Adding Timekeeper to PATH...
for /f "tokens=2*" %%A in ('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v PATH 2^>nul') do set "CURRENT_PATH=%%B"

:: Check if already in PATH
echo %CURRENT_PATH% | findstr /C:"%INSTALL_DIR%" >nul
if %errorLevel% neq 0 (
    echo Adding %INSTALL_DIR% to system PATH...
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v PATH /t REG_EXPAND_SZ /d "%CURRENT_PATH%;%INSTALL_DIR%" /f
    if %errorLevel% == 0 (
        echo Successfully added to PATH.
    ) else (
        echo Failed to add to PATH.
    )
) else (
    echo Timekeeper is already in PATH.
)

:: Create desktop shortcut (optional)
set /p CREATE_SHORTCUT="Create desktop shortcut? (y/n): "
if /i "%CREATE_SHORTCUT%"=="y" (
    echo Creating desktop shortcut...
    powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\Timekeeper.lnk'); $Shortcut.TargetPath = '%TIMEKEEPER_EXE%'; $Shortcut.IconLocation = '%INSTALL_DIR%\TimeKeeper.ico'; $Shortcut.Save()"
)

:: Create start menu shortcut
echo Creating Start Menu shortcut...
if not exist "%ProgramData%\Microsoft\Windows\Start Menu\Programs\Timekeeper" (
    mkdir "%ProgramData%\Microsoft\Windows\Start Menu\Programs\Timekeeper"
)
powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%ProgramData%\Microsoft\Windows\Start Menu\Programs\Timekeeper\Timekeeper.lnk'); $Shortcut.TargetPath = '%TIMEKEEPER_EXE%'; $Shortcut.IconLocation = '%INSTALL_DIR%\TimeKeeper.ico'; $Shortcut.Save()"

:: Create uninstaller
echo Creating uninstaller...
call :CREATE_UNINSTALLER

:: Register in Add/Remove Programs
echo Registering in Add/Remove Programs...
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" /v "DisplayName" /t REG_SZ /d "Timekeeper CLI" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" /v "DisplayVersion" /t REG_SZ /d "1.0.0" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" /v "Publisher" /t REG_SZ /d "Timekeeper" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" /v "InstallLocation" /t REG_SZ /d "%INSTALL_DIR%" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" /v "UninstallString" /t REG_SZ /d "%INSTALL_DIR%\uninstall.bat" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" /v "NoModify" /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" /v "NoRepair" /t REG_DWORD /d 1 /f

echo.
echo ========================================
echo    Installation completed successfully!
echo ========================================
echo.
echo You can now use 'tk' from any command prompt.
echo.
echo To get started, open a new command prompt and type:
echo   tk
echo.
echo Note: You may need to restart your command prompt
echo       or log out and back in for the PATH changes
echo       to take effect.
echo.
pause
goto :eof

:CREATE_UNINSTALLER
(
echo @echo off
echo echo ========================================
echo echo    Timekeeper CLI Uninstaller
echo echo ========================================
echo echo.
echo.
echo :: Check for administrator privileges
echo net session ^>nul 2^>^&1
echo if %%errorLevel%% == 0 ^(
echo     echo Running with administrator privileges...
echo ^) else ^(
echo     echo This uninstaller requires administrator privileges.
echo     echo Please run as administrator.
echo     pause
echo     exit /b 1
echo ^)
echo.
echo set /p CONFIRM="Are you sure you want to uninstall Timekeeper? (y/n): "
echo if /i not "%%CONFIRM%%"=="y" exit /b 0
echo.
echo echo Removing Timekeeper...
echo.
echo :: Remove from PATH
echo echo Removing from PATH...
echo for /f "tokens=2*" %%%%A in ^('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v PATH 2^^^^^>nul'^) do set "CURRENT_PATH=%%%%B"
echo set "NEW_PATH=%%CURRENT_PATH:;%INSTALL_DIR%=%%"
echo set "NEW_PATH=%%NEW_PATH:%INSTALL_DIR%;=%%"
echo reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v PATH /t REG_EXPAND_SZ /d "%%NEW_PATH%%" /f
echo.
echo :: Remove shortcuts
echo echo Removing shortcuts...
echo if exist "%%USERPROFILE%%\Desktop\Timekeeper.lnk" del "%%USERPROFILE%%\Desktop\Timekeeper.lnk"
echo if exist "%%ProgramData%%\Microsoft\Windows\Start Menu\Programs\Timekeeper" rmdir /s /q "%%ProgramData%%\Microsoft\Windows\Start Menu\Programs\Timekeeper"
echo.
echo :: Remove from Add/Remove Programs
echo echo Removing from Add/Remove Programs...
echo reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Timekeeper" /f ^>nul 2^>^&1
echo.
echo :: Remove installation directory
echo echo Removing installation files...
echo cd /d "%%TEMP%%"
echo rmdir /s /q "%INSTALL_DIR%"
echo.
echo echo Uninstallation completed successfully!
echo pause
) > "%INSTALL_DIR%\uninstall.bat"
goto :eof
