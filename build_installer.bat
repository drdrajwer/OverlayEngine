@echo off
setlocal enabledelayedexpansion
title OverlayEngine — Velopack Installer Build

:: ── Wersja aplikacji ─────────────────────────────────────────────────────────
set "APP_VERSION=1.1.0"
set "APP_ID=OverlayEngine"

:: ── Sciezki ──────────────────────────────────────────────────────────────────
set "SCRIPT_DIR=%~dp0"

:: Jesli wywolany z build_windows.bat, uzyj przekazanego katalogu publish
if not "%~1"=="" (
    set "PUBLISH_DIR=%~1"
) else (
    set "PUBLISH_DIR=%SCRIPT_DIR%publish_windows"
)

set "RELEASES_DIR=%SCRIPT_DIR%releases"
set "CSPROJ=%SCRIPT_DIR%src\OverlayEngine.UI\OverlayEngine.UI.csproj"

echo.
echo  ============================================
echo   OverlayEngine  ^|  Velopack Installer Build
echo   Wersja: %APP_VERSION%
echo  ============================================
echo.

:: ── Sprawdz vpk ──────────────────────────────────────────────────────────────
vpk --version >nul 2>&1
if errorlevel 1 (
    echo  [INFO] Narzedzie vpk nie znalezione. Instaluje...
    dotnet tool install -g vpk
    if errorlevel 1 (
        echo  [BLAD] Nie udalo sie zainstalowac vpk.
        echo  Sprobuj recznie: dotnet tool install -g vpk
        pause
        exit /b 1
    )
    echo  [OK] vpk zainstalowany.
    echo.
)

:: ── Sprawdz katalog publish ───────────────────────────────────────────────────
if not exist "%PUBLISH_DIR%\OverlayEngine.UI.exe" (
    echo  [INFO] Brak buildu w %PUBLISH_DIR%
    echo  Uruchamiam dotnet publish...
    echo.

    dotnet publish "%CSPROJ%" ^
        -f net8.0-windows10.0.19041.0 ^
        --configuration Release ^
        --runtime win-x64 ^
        --self-contained true ^
        --output "%PUBLISH_DIR%" ^
        -p:PublishSingleFile=true ^
        -p:IncludeNativeLibrariesForSelfExtract=true

    if errorlevel 1 (
        echo  [BLAD] Build nie powiodl sie.
        pause
        exit /b 1
    )
)

:: ── Pakowanie z Velopack ─────────────────────────────────────────────────────
echo.
echo  Pakowanie z Velopack...
echo  Wersja:    %APP_VERSION%
echo  Zrodlo:    %PUBLISH_DIR%
echo  Cel:       %RELEASES_DIR%
echo.

if exist "%RELEASES_DIR%" (
    echo  Czyszczenie starego releases...
    rmdir /s /q "%RELEASES_DIR%"
)

vpk windows pack ^
    --packId "%APP_ID%" ^
    --packVersion "%APP_VERSION%" ^
    --packDir "%PUBLISH_DIR%" ^
    --mainExe "OverlayEngine.UI.exe" ^
    --outputDir "%RELEASES_DIR%"

if errorlevel 1 (
    echo.
    echo  [BLAD] Pakowanie Velopack nie powiodlo sie.
    echo  Sprawdz czy URL GitHub w UpdateService.cs jest poprawny.
    pause
    exit /b 1
)

:: ── Gotowe ───────────────────────────────────────────────────────────────────
echo.
echo  ============================================
echo   Installer gotowy!
echo  ============================================
echo.
echo  Pliki w: %RELEASES_DIR%
echo.
echo  Co dalej:
echo  1. Utw rz release na GitHub (tag: v%APP_VERSION%)
echo  2. Wgraj WSZYSTKIE pliki z folderu releases\ na GitHub Release
echo  3. W UpdateService.cs ustaw poprawny URL repozytorium GitHub
echo.
echo  Instalator dla uzytkownikow:
echo  %RELEASES_DIR%\%APP_ID%-Setup.exe
echo.

set /p OPEN="Otworzyc folder releases\? [T/N]: "
if /i "%OPEN%"=="T" (
    explorer "%RELEASES_DIR%"
)

pause
