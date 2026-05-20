@echo off
setlocal enabledelayedexpansion
title OverlayEngine — Installer Build

set "APP_VERSION=1.1.0"
set "SCRIPT_DIR=%~dp0"
set "PUBLISH_DIR=%SCRIPT_DIR%publish_installer"
set "RELEASES_DIR=%SCRIPT_DIR%releases"
set "CSPROJ=%SCRIPT_DIR%src\OverlayEngine.UI\OverlayEngine.UI.csproj"
set "ISS=%SCRIPT_DIR%setup.iss"
set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo.
echo  ============================================
echo   OverlayEngine  ^|  Installer Build v%APP_VERSION%
echo  ============================================
echo.

:: ── Sprawdz .NET SDK ─────────────────────────────────────────────────────────
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo  [BLAD] Brak .NET SDK. Pobierz ze: https://dotnet.microsoft.com/download/dotnet/8.0
    pause & exit /b 1
)

:: ── Sprawdz / zainstaluj Inno Setup ──────────────────────────────────────────
if not exist "%ISCC%" (
    echo  [INFO] Inno Setup nie znaleziony. Pobieranie i instalacja...
    set "IS_INSTALLER=%TEMP%\innosetup.exe"
    curl -L --progress-bar "https://files.jrsoftware.org/is/6/innosetup-6.3.3.exe" -o "!IS_INSTALLER!"
    if errorlevel 1 (
        echo  [BLAD] Nie udalo sie pobrac Inno Setup.
        echo  Pobierz recznie ze: https://jrsoftware.org/isdl.php
        pause & exit /b 1
    )
    "!IS_INSTALLER!" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
    if not exist "%ISCC%" (
        echo  [BLAD] Inno Setup nie zainstalowal sie poprawnie.
        pause & exit /b 1
    )
    echo  [OK] Inno Setup zainstalowany.
    echo.
)

:: ── Buduj projekt ────────────────────────────────────────────────────────────
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
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -nologo

if errorlevel 1 (
    echo  [BLAD] Kompilacja nie powiodla sie.
    pause & exit /b 1
)

:: ── Kompiluj instalator ───────────────────────────────────────────────────────
echo.
echo  Tworzenie instalatora...
echo.

if exist "%RELEASES_DIR%" rmdir /s /q "%RELEASES_DIR%"

"%ISCC%" "%ISS%"

if not exist "%RELEASES_DIR%\OverlayEngine-Setup.exe" (
    echo.
    echo  [BLAD] Instalator nie zostal utworzony.
    pause & exit /b 1
)

:: ── Gotowe ───────────────────────────────────────────────────────────────────
echo.
echo  ============================================
echo   Gotowe!  OverlayEngine-Setup.exe utworzony
echo  ============================================
echo.
echo  Plik: %RELEASES_DIR%\OverlayEngine-Setup.exe
echo.
echo  Co dalej — wgraj na GitHub Release:
echo  1. https://github.com/drdrajwer/OverlayEngine/releases/new
echo  2. Tag: v%APP_VERSION%
echo  3. Wgraj: %RELEASES_DIR%\OverlayEngine-Setup.exe
echo.

set /p OPEN="Otworzyc folder releases\? [T/N]: "
if /i "%OPEN%"=="T" explorer "%RELEASES_DIR%"

echo.
pause
