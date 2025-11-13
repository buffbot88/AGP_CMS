#!/bin/bash
# Quick Start Script for AGP CMS Reseller System

echo "╔═══════════════════════════════════════════════════════════════════════════╗"
echo "║                AGP CMS RESELLER SYSTEM - QUICK START                      ║"
echo "╚═══════════════════════════════════════════════════════════════════════════╝"
echo ""

# Check if Python is installed
if ! command -v python3 &> /dev/null; then
    echo "✗ Error: Python 3 is not installed"
    echo "  Please install Python 3.7 or higher"
    exit 1
fi

echo "✓ Python 3 is installed: $(python3 --version)"

# Check if pip is installed
if ! command -v pip3 &> /dev/null && ! command -v pip &> /dev/null; then
    echo "✗ Error: pip is not installed"
    echo "  Please install pip"
    exit 1
fi

echo "✓ pip is installed"

# Install dependencies
echo ""
echo "Installing dependencies..."
pip3 install -q -r requirements-reseller.txt

if [ $? -eq 0 ]; then
    echo "✓ Dependencies installed successfully"
else
    echo "✗ Error installing dependencies"
    exit 1
fi

# Show menu
echo ""
echo "═══════════════════════════════════════════════════════════════════════════"
echo "What would you like to do?"
echo "═══════════════════════════════════════════════════════════════════════════"
echo "1. Start Reseller System (Interactive Menu)"
echo "2. Start FTP Server Only"
echo "3. Create Test Account and Exit"
echo "4. View Documentation"
echo "5. Exit"
echo "═══════════════════════════════════════════════════════════════════════════"
read -p "Enter your choice (1-5): " choice

case $choice in
    1)
        echo ""
        echo "Starting Reseller System..."
        python3 reseller.py
        ;;
    2)
        echo ""
        echo "Starting FTP Server..."
        echo "Note: You may need sudo for port 21"
        echo ""
        read -p "Run with sudo? (y/n): " use_sudo
        if [ "$use_sudo" == "y" ] || [ "$use_sudo" == "Y" ]; then
            sudo python3 reseller.py ftp
        else
            python3 reseller.py ftp
        fi
        ;;
    3)
        echo ""
        echo "Creating test account..."
        python3 -c "
from reseller import ResellerManager
manager = ResellerManager()
account_id = manager.create_account(
    username='demouser',
    password='demo123',
    email='demo@example.com',
    site_name='Demo Website',
    package_type='4'
)
if account_id:
    print('\n✓ Test account created!')
    print('  Username: demouser')
    print('  Password: demo123')
    print('  Email: demo@example.com')
    print('\nYou can now start the FTP server to allow file access.')
else:
    print('✗ Failed to create test account (may already exist)')
"
        ;;
    4)
        echo ""
        if command -v less &> /dev/null; then
            less RESELLER_README.md
        else
            cat RESELLER_README.md
        fi
        ;;
    5)
        echo "Goodbye!"
        exit 0
        ;;
    *)
        echo "✗ Invalid choice"
        exit 1
        ;;
esac
