@echo off
echo ========================================
echo DPO Manager - Build Script
echo ========================================
echo.

REM Check if dotnet is available
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 10.0 SDK or later from:
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Checking .NET version...
dotnet --version
echo.

echo ========================================
echo Build Options:
echo ========================================
echo 1. Build Debug Version
echo 2. Build Release Version
echo 3. Build Release (Single File)
echo 4. Clean Build Directory
echo 5. Exit
echo.

set /p choice="Enter your choice (1-5): "

if "%choice%"=="1" goto build_debug
if "%choice%"=="2" goto build_release
if "%choice%"=="3" goto build_single
if "%choice%"=="4" goto clean
if "%choice%"=="5" goto end
goto invalid

:build_debug
echo.
echo Building Debug version...
dotnet build DPOManager.csproj -c Debug
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo BUILD SUCCESSFUL!
    echo ========================================
    echo Output: bin\Debug\net10.0-windows\DPOManager.exe
    echo.
) else (
    echo.
    echo BUILD FAILED!
    echo.
)
pause
goto end

:build_release
echo.
echo Building Release version...
dotnet build DPOManager.csproj -c Release
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo BUILD SUCCESSFUL!
    echo ========================================
    echo Output: bin\Release\net10.0-windows\DPOManager.exe
    echo.
    echo You can now copy the contents of bin\Release\net10.0-windows\
    echo to your production directory.
    echo.
) else (
    echo.
    echo BUILD FAILED!
    echo.
)
pause
goto end

:build_single
echo.
echo Building Release version (Single File)...
echo This may take a few minutes...
dotnet publish DPOManager.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o published
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo PUBLISH SUCCESSFUL!
    echo ========================================
    echo Output: published\DPOManager.exe
    echo.
    echo This is a single-file executable that includes all dependencies.
    echo Copy DPOManager.exe to your production directory.
    echo.
) else (
    echo.
    echo PUBLISH FAILED!
    echo.
)
pause
goto end

:clean
echo.
echo Cleaning build directories...
if exist "bin" rmdir /s /q bin
if exist "obj" rmdir /s /q obj
if exist "published" rmdir /s /q published
echo.
echo Clean completed!
echo.
pause
goto end

:invalid
echo.
echo Invalid choice! Please select 1-5.
echo.
pause
goto end

:end
echo.
echo ========================================
echo Done!
echo ========================================
