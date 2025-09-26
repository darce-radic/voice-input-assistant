@echo off
echo ========================================
echo Voice Input Assistant - Project Setup
echo ========================================

echo.
echo [1/5] Checking prerequisites...
echo.

REM Check for Node.js
node --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Node.js not found. Please install Node.js 18+ from https://nodejs.org/
    pause
    exit /b 1
)

REM Check for .NET 8
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ❌ .NET not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/
    pause
    exit /b 1
)

echo ✅ Node.js found: 
node --version

echo ✅ .NET found: 
dotnet --version

echo.
echo [2/5] Installing root dependencies...
echo.
npm install

echo.
echo [3/5] Setting up desktop application...
echo.
cd apps\desktop
dotnet restore
if errorlevel 1 (
    echo ❌ Failed to restore .NET packages
    pause
    exit /b 1
)
cd ..\..

echo.
echo [4/5] Setting up web dashboard...
echo.
cd apps\web-dashboard
npm install
if errorlevel 1 (
    echo ❌ Failed to install web dashboard dependencies
    pause
    exit /b 1
)
cd ..\..

echo.
echo [5/5] Setting up shared packages...
echo.
cd packages\common
npm install
if errorlevel 1 (
    echo ❌ Failed to install common package dependencies
    pause
    exit /b 1
)
npm run build
cd ..\..

echo.
echo ========================================
echo ✅ Setup Complete!
echo ========================================
echo.
echo Available commands:
echo.
echo   Development:
echo   • npm run dev              - Start all development servers
echo   • npm run desktop:dev      - Start desktop app only
echo   • npm run web:dev          - Start web dashboard only
echo.
echo   Build:
echo   • npm run build            - Build all projects
echo   • npm run test             - Run all tests
echo.
echo   Desktop app will be available at: Desktop Application
echo   Web dashboard will be available at: http://localhost:3001
echo.
echo Next Steps:
echo 1. Copy .env.example to .env and configure your environment variables
echo 2. Set up your database (if using cloud sync)
echo 3. Configure API keys for cloud speech services (optional)
echo 4. Run 'npm run dev' to start development
echo.
pause