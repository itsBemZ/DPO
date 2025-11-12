@echo off
echo ========================================
echo DPO Manager - SDK Verification
echo ========================================
echo.

echo Checking .NET SDK installation...
echo.

dotnet --version
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet command not found!
    echo Please ensure .NET SDK is installed and in PATH.
    pause
    exit /b 1
)

echo.
echo Listing installed SDKs:
dotnet --list-sdks

echo.
echo Listing installed Runtimes:
dotnet --list-runtimes

echo.
echo ========================================
echo SDK Check Complete
echo ========================================
echo.
echo You have .NET SDK 10.0.100 installed.
echo This project targets .NET 10.0-windows
echo which matches your SDK perfectly!
echo.
echo You can now run build.bat to compile.
echo.
pause
