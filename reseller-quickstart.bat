@echo off
REM Quick Start Script for AGP CMS Reseller System (Windows)

echo ========================================================================
echo             AGP CMS RESELLER SYSTEM - QUICK START
echo ========================================================================
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo [X] Error: Python is not installed
    echo     Please install Python 3.7 or higher from python.org
    pause
    exit /b 1
)

echo [OK] Python is installed
python --version

REM Check if pip is installed
pip --version >nul 2>&1
if errorlevel 1 (
    echo [X] Error: pip is not installed
    echo     Please install pip
    pause
    exit /b 1
)

echo [OK] pip is installed
echo.

REM Install dependencies
echo Installing dependencies...
pip install -q -r requirements-reseller.txt

if errorlevel 1 (
    echo [X] Error installing dependencies
    pause
    exit /b 1
)

echo [OK] Dependencies installed successfully
echo.

REM Show menu
echo ========================================================================
echo What would you like to do?
echo ========================================================================
echo 1. Start Reseller System (Interactive Menu)
echo 2. Start FTP Server Only
echo 3. Create Test Account and Exit
echo 4. View Documentation
echo 5. Exit
echo ========================================================================
set /p choice="Enter your choice (1-5): "

if "%choice%"=="1" (
    echo.
    echo Starting Reseller System...
    python reseller.py
) else if "%choice%"=="2" (
    echo.
    echo Starting FTP Server...
    echo Note: You may need administrator privileges for port 21
    python reseller.py ftp
) else if "%choice%"=="3" (
    echo.
    echo Creating test account...
    python -c "from reseller import ResellerManager; manager = ResellerManager(); account_id = manager.create_account(username='demouser', password='demo123', email='demo@example.com', site_name='Demo Website', package_type='4'); print('\n[OK] Test account created!' if account_id else '\n[X] Failed to create test account')"
) else if "%choice%"=="4" (
    echo.
    type RESELLER_README.md | more
) else if "%choice%"=="5" (
    echo Goodbye!
    exit /b 0
) else (
    echo [X] Invalid choice
    pause
    exit /b 1
)

pause
