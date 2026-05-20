@echo off
setlocal enabledelayedexpansion
title OverlayEngine — Windows Build

echo.
echo  ============================================
echo   OverlayEngine  ^|  Windows Build Script
echo  ============================================
echo.

:: ── Sprawdz .NET 8 SDK ──────────────────────────────────────────────────────
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo  [BLAD] Nie znaleziono .NET SDK.
    echo.
    echo  Pobierz .NET 8 SDK ze strony:
    echo  https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VER=%%v
echo  Znaleziono .NET SDK: %DOTNET_VER%

set "VER_MAJOR=%DOTNET_VER:~0,1%"
if "%VER_MAJOR%" LSS "8" (
    echo.
    echo  [BLAD] Wymagany .NET 8 lub nowszy. Masz: %DOTNET_VER%
    echo  Pobierz ze: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

:: ── Ustal sciezki ───────────────────────────────────────────────────────────
set "SCRIPT_DIR=%~dp0"
set "CSPROJ=%SCRIPT_DIR%src\OverlayEngine.UI\OverlayEngine.UI.csproj"
set "OUTPUT=%SCRIPT_DIR%publish_windows"

if not exist "%CSPROJ%" (
    echo.
    echo  [BLAD] Nie znaleziono pliku projektu:
    echo  %CSPROJ%
    echo.
    echo  Upewnij sie ze skrypt jest w glownym folderze projektu OverlayEngine.
    echo.
    pause
    exit /b 1
)

:: ── Buduj ───────────────────────────────────────────────────────────────────
echo.
echo  Budowanie projektu dla Windows x64...
echo  Output: %OUTPUT%
echo.

if exist "%OUTPUT%" (
    echo  Czyszczenie starego buildu...
    rmdir /s /q "%OUTPUT%"
)

dotnet publish "%CSPROJ%" ^
    -f net8.0-windows10.0.19041.0 ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "%OUTPUT%" ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true

if errorlevel 1 (
    echo.
    echo  [BLAD] Build nie powiodl sie. Sprawdz bledy powyzej.
    echo.
    pause
    exit /b 1
)

:: ── Sukces ──────────────────────────────────────────────────────────────────
echo.
echo  ============================================
echo   Build zakonczony pomyslnie!
echo  ============================================
echo.
echo  Plik wykonywalny:
echo  %OUTPUT%\OverlayEngine.UI.exe
echo.
echo  WAZNE:
echo  - Uruchom jako Administrator (wymagane przez LibreHardwareMonitor
echo    do odczytu temperatur CPU/GPU przez sterownik ring0)
echo.

:: ── Velopack — pakiet instalacyjny (opcjonalnie) ─────────────────────────────
echo.
set /p PACK="Spakować instalator z Velopack (wymaga GitHub repo)? [T/N]: "
if /i "%PACK%"=="T" (
    call "%SCRIPT_DIR%build_installer.bat" "%OUTPUT%"
    goto :end
)

set /p LAUNCH="Uruchomic teraz jako Administrator? [T/N]: "
if /i "%LAUNCH%"=="T" (
    echo.
    echo  Uruchamianie z prawami administratora...
    powershell -Command "Start-Process '%OUTPUT%\OverlayEngine.UI.exe' -Verb RunAs"
)

:end
echo.
pause
