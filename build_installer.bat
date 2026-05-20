@echo off
setlocal enabledelayedexpansion
title OverlayEngine — Velopack Installer Build

:: ── Wersja aplikacji ─────────────────────────────────────────────────────────
set "APP_VERSION=1.1.0"
set "APP_ID=OverlayEngine"

:: ── Sciezki ──────────────────────────────────────────────────────────────────
set "SCRIPT_DIR=%~dp0"
set "PUBLISH_DIR=%SCRIPT_DIR%publish_installer"
set "RELEASES_DIR=%SCRIPT_DIR%releases"
set "CSPROJ=%SCRIPT_DIR%src\OverlayEngine.UI\OverlayEngine.UI.csproj"

echo.
echo  ============================================
echo   OverlayEngine  ^|  Velopack Installer Build
echo   Wersja: %APP_VERSION%
echo  ============================================
echo.

:: ── Sprawdz .NET SDK ──────────────────────────────────────────────────────────
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo  [BLAD] Brak .NET SDK. Pobierz ze: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

:: ── Sprawdz vpk ──────────────────────────────────────────────────────────────
vpk --version >nul 2>&1
if errorlevel 1 (
    echo  [INFO] Instaluje narzedzie vpk...
    dotnet tool install -g vpk
    if errorlevel 1 (
        echo  [BLAD] Nie udalo sie zainstalowac vpk.
        pause
        exit /b 1
    )
    echo  [OK] vpk zainstalowany.
    echo.
)

:: ── Zawsze buduj od zera (bez PublishSingleFile) ─────────────────────────────
echo  Czyszczenie starego buildu...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"

echo  Kompilowanie projektu...
echo.

dotnet publish "%CSPROJ%" ^
    -f net8.0-windows10.0.19041.0 ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "%PUBLISH_DIR%" ^
    -nologo

if errorlevel 1 (
    echo.
    echo  [BLAD] Kompilacja nie powiodla sie.
    pause
    exit /b 1
)

:: ── Pakowanie z Velopack ─────────────────────────────────────────────────────
echo.
echo  Pakowanie instalatora...
echo  Wersja: %APP_VERSION%
echo.

if exist "%RELEASES_DIR%" rmdir /s /q "%RELEASES_DIR%"

vpk pack ^
    --packId "%APP_ID%" ^
    --packVersion "%APP_VERSION%" ^
    --packDir "%PUBLISH_DIR%" ^
    --mainExe "OverlayEngine.UI.exe" ^
    --outputDir "%RELEASES_DIR%"

:: Sprawdz czy instalator faktycznie powstal
if not exist "%RELEASES_DIR%\%APP_ID%-Setup.exe" (
    echo.
    echo  [BLAD] Instalator nie zostal utworzony. Sprawdz bledy powyzej.
    pause
    exit /b 1
)

:: ── Gotowe ───────────────────────────────────────────────────────────────────
echo.
echo  ============================================
echo   Installer gotowy!
echo  ============================================
echo.
echo  Instalator: %RELEASES_DIR%\%APP_ID%-Setup.exe
echo.
echo  Co dalej:
echo  1. Wejdz na: https://github.com/drdrajwer/OverlayEngine/releases/new
echo  2. Tag: v%APP_VERSION%
echo  3. Wgraj WSZYSTKIE pliki z folderu:
echo     %RELEASES_DIR%
echo.

set /p OPEN="Otworzyc folder releases\? [T/N]: "
if /i "%OPEN%"=="T" explorer "%RELEASES_DIR%"

echo.
pause
