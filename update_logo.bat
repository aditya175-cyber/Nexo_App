@echo off
echo ============================================
echo   Updating Nexo Logo and Building App
echo ============================================

taskkill /F /IM Nexo.exe /T >nul 2>&1
timeout /t 1 /nobreak >nul

d:\Nexo\src\WinIsland.Backend\.venv\Scripts\python.exe d:\Nexo\convert_logo.py
if %ERRORLEVEL% neq 0 (
    echo Gagal mengonversi logo! Pastikan d:\Nexo\src\WinIsland.UI\Assets\logo.png ada.
    pause
    exit /b %ERRORLEVEL%
)

dotnet build d:\Nexo\src\WinIsland.UI --nologo
if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUKSES] Logo berhasil diperbarui dan aplikasi siap dijalankan!
) else (
    echo.
    echo [ERROR] Gagal melakukan build.
)

pause
