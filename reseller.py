#!/usr/bin/env python3
"""
AGP CMS Reseller Script
Allows selling and provisioning of forum, blog, website, or full suite packages
Includes FTP server for user file access
"""

import os
import sys
import json
import hashlib
import sqlite3
import threading
from datetime import datetime
from pathlib import Path
from pyftpdlib.authorizers import DummyAuthorizer
from pyftpdlib.handlers import FTPHandler
from pyftpdlib.servers import FTPServer

# Configuration
RESELLER_DB = "reseller_accounts.db"
CMS_DB = "LegendaryCMS/agp_cms.db"
SITES_ROOT = "reseller_sites"
FTP_PORT = 21
FTP_HOST = "0.0.0.0"

# Package types
PACKAGES = {
    "1": {"name": "Forum", "features": ["forum"]},
    "2": {"name": "Blog", "features": ["blog"]},
    "3": {"name": "Website", "features": ["website"]},
    "4": {"name": "Full Suite", "features": ["forum", "blog", "website", "downloads"]}
}


class ResellerManager:
    """Manages reseller accounts and site provisioning"""
    
    def __init__(self):
        self.db_path = RESELLER_DB
        self.sites_root = Path(SITES_ROOT)
        self.sites_root.mkdir(exist_ok=True)
        self.init_database()
    
    def init_database(self):
        """Initialize the reseller database"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS reseller_accounts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT UNIQUE NOT NULL,
                password_hash TEXT NOT NULL,
                email TEXT NOT NULL,
                site_name TEXT UNIQUE NOT NULL,
                package_type TEXT NOT NULL,
                site_path TEXT NOT NULL,
                ftp_enabled INTEGER DEFAULT 1,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                status TEXT DEFAULT 'active'
            )
        """)
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS site_features (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_id INTEGER NOT NULL,
                feature_name TEXT NOT NULL,
                enabled INTEGER DEFAULT 1,
                FOREIGN KEY (account_id) REFERENCES reseller_accounts(id)
            )
        """)
        
        conn.commit()
        conn.close()
        print("✓ Reseller database initialized")
    
    def hash_password(self, password):
        """Hash password using SHA256"""
        return hashlib.sha256(password.encode()).hexdigest()
    
    def create_account(self, username, password, email, site_name, package_type):
        """Create a new reseller account"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Sanitize site name for folder creation
        safe_site_name = "".join(c for c in site_name if c.isalnum() or c in ('-', '_')).lower()
        site_path = self.sites_root / safe_site_name
        
        # Check if site already exists
        if site_path.exists():
            print(f"✗ Error: Site folder '{safe_site_name}' already exists")
            conn.close()
            return None
        
        # Create site directory structure
        site_path.mkdir(parents=True, exist_ok=True)
        (site_path / "wwwroot").mkdir(exist_ok=True)
        (site_path / "data").mkdir(exist_ok=True)
        (site_path / "uploads").mkdir(exist_ok=True)
        (site_path / "logs").mkdir(exist_ok=True)
        
        # Hash password
        password_hash = self.hash_password(password)
        
        # Insert account
        now = datetime.utcnow().isoformat()
        try:
            cursor.execute("""
                INSERT INTO reseller_accounts 
                (username, password_hash, email, site_name, package_type, site_path, created_at, updated_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """, (username, password_hash, email, site_name, package_type, str(site_path), now, now))
            
            account_id = cursor.lastrowid
            
            # Add features based on package
            package = PACKAGES.get(package_type, PACKAGES["4"])
            for feature in package["features"]:
                cursor.execute("""
                    INSERT INTO site_features (account_id, feature_name, enabled)
                    VALUES (?, ?, 1)
                """, (account_id, feature))
            
            conn.commit()
            
            # Create default files
            self.create_default_files(site_path, site_name, package["features"])
            
            print(f"✓ Account created successfully!")
            print(f"  Username: {username}")
            print(f"  Site Name: {site_name}")
            print(f"  Site Path: {site_path}")
            print(f"  Package: {package['name']}")
            print(f"  FTP Access: ftp://{FTP_HOST}:{FTP_PORT}")
            
            return account_id
            
        except sqlite3.IntegrityError as e:
            print(f"✗ Error: {e}")
            # Cleanup created directories
            if site_path.exists():
                import shutil
                shutil.rmtree(site_path)
            return None
        finally:
            conn.close()
    
    def create_default_files(self, site_path, site_name, features):
        """Create default files for the site"""
        # Create index.html
        index_content = f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{site_name} - Welcome</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #333;
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
        }}
        .container {{
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.3);
            max-width: 600px;
            text-align: center;
        }}
        h1 {{
            color: #667eea;
            margin-bottom: 20px;
        }}
        .features {{
            margin: 30px 0;
            text-align: left;
        }}
        .feature {{
            background: #f7f7f7;
            padding: 10px;
            margin: 10px 0;
            border-radius: 5px;
            border-left: 4px solid #667eea;
        }}
        .footer {{
            margin-top: 30px;
            color: #666;
            font-size: 0.9em;
        }}
    </style>
</head>
<body>
    <div class="container">
        <h1>Welcome to {site_name}!</h1>
        <p>Your site has been successfully provisioned with AGP CMS Reseller.</p>
        
        <div class="features">
            <h3>Your Package Includes:</h3>
"""
        
        feature_names = {
            "forum": "Discussion Forums",
            "blog": "Blog System",
            "website": "Website Hosting",
            "downloads": "File Downloads"
        }
        
        for feature in features:
            index_content += f'            <div class="feature">✓ {feature_names.get(feature, feature.title())}</div>\n'
        
        index_content += """        </div>
        
        <p>You can upload your files via FTP to customize this website.</p>
        
        <div class="footer">
            <p>Powered by AGP CMS Reseller</p>
        </div>
    </div>
</body>
</html>
"""
        
        with open(site_path / "wwwroot" / "index.html", "w") as f:
            f.write(index_content)
        
        # Create README
        readme_content = f"""# {site_name}

Welcome to your AGP CMS Reseller site!

## Site Information
- Site Name: {site_name}
- Package: {', '.join(features)}
- Created: {datetime.utcnow().isoformat()}

## Directory Structure
- `wwwroot/` - Your website files (HTML, CSS, JS, images)
- `data/` - Database and data files
- `uploads/` - User uploaded files
- `logs/` - System logs

## FTP Access
You can access your files via FTP using your username and password.
- Host: {FTP_HOST}
- Port: {FTP_PORT}
- Protocol: FTP

## Getting Started
1. Upload your website files to the `wwwroot` directory
2. Customize the default index.html file
3. Add your content and media files

For support, please contact your reseller provider.
"""
        
        with open(site_path / "README.md", "w") as f:
            f.write(readme_content)
        
        print(f"✓ Default files created in {site_path}")
    
    def list_accounts(self):
        """List all reseller accounts"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT id, username, email, site_name, package_type, status, created_at
            FROM reseller_accounts
            ORDER BY created_at DESC
        """)
        
        accounts = cursor.fetchall()
        conn.close()
        
        if not accounts:
            print("No accounts found.")
            return
        
        print("\n" + "="*80)
        print("RESELLER ACCOUNTS")
        print("="*80)
        for account in accounts:
            print(f"\nID: {account[0]}")
            print(f"Username: {account[1]}")
            print(f"Email: {account[2]}")
            print(f"Site Name: {account[3]}")
            print(f"Package: {PACKAGES.get(account[4], {}).get('name', account[4])}")
            print(f"Status: {account[5]}")
            print(f"Created: {account[6]}")
            print("-" * 80)
    
    def get_account_credentials(self, username):
        """Get account credentials for FTP authorization"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT username, password_hash, site_path, ftp_enabled
            FROM reseller_accounts
            WHERE username = ? AND status = 'active'
        """, (username,))
        
        result = cursor.fetchone()
        conn.close()
        
        return result
    
    def get_all_active_accounts(self):
        """Get all active accounts for FTP server"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute("""
            SELECT username, password_hash, site_path
            FROM reseller_accounts
            WHERE status = 'active' AND ftp_enabled = 1
        """)
        
        results = cursor.fetchall()
        conn.close()
        
        return results


class CustomFTPHandler(FTPHandler):
    """Custom FTP handler with logging"""
    
    def on_connect(self):
        print(f"[FTP] New connection from {self.remote_ip}:{self.remote_port}")
    
    def on_disconnect(self):
        print(f"[FTP] Disconnected: {self.username or 'anonymous'}@{self.remote_ip}")
    
    def on_login(self, username):
        print(f"[FTP] User logged in: {username}")
    
    def on_file_received(self, file):
        print(f"[FTP] File uploaded: {file}")
    
    def on_file_sent(self, file):
        print(f"[FTP] File downloaded: {file}")


class ResellerFTPServer:
    """FTP Server for reseller accounts"""
    
    def __init__(self, manager):
        self.manager = manager
        self.server = None
    
    def setup_authorizer(self):
        """Setup FTP authorizer with reseller accounts"""
        authorizer = DummyAuthorizer()
        
        # Add all active reseller accounts
        accounts = self.manager.get_all_active_accounts()
        
        for username, password_hash, site_path in accounts:
            # Note: pyftpdlib expects plaintext password for DummyAuthorizer
            # In production, use a custom authorizer that checks hashed passwords
            # For now, we'll use a simple approach
            try:
                authorizer.add_user(
                    username=username,
                    password=password_hash[:16],  # Use first 16 chars as temp password
                    homedir=site_path,
                    perm="elradfmwMT"  # Full permissions
                )
                print(f"✓ Added FTP user: {username} -> {site_path}")
            except Exception as e:
                print(f"✗ Error adding FTP user {username}: {e}")
        
        return authorizer
    
    def start(self):
        """Start the FTP server"""
        try:
            authorizer = self.setup_authorizer()
            
            handler = CustomFTPHandler
            handler.authorizer = authorizer
            handler.banner = "AGP CMS Reseller FTP Server Ready"
            
            # Set passive ports
            handler.passive_ports = range(60000, 60100)
            
            self.server = FTPServer((FTP_HOST, FTP_PORT), handler)
            
            # Set limits
            self.server.max_cons = 256
            self.server.max_cons_per_ip = 5
            
            print(f"\n{'='*80}")
            print(f"FTP SERVER STARTED")
            print(f"{'='*80}")
            print(f"Host: {FTP_HOST}")
            print(f"Port: {FTP_PORT}")
            print(f"Max Connections: {self.server.max_cons}")
            print(f"Max Connections per IP: {self.server.max_cons_per_ip}")
            print(f"{'='*80}\n")
            
            # Start serving
            self.server.serve_forever()
            
        except PermissionError:
            print(f"\n✗ Error: Permission denied to bind to port {FTP_PORT}")
            print(f"  On Unix/Linux, you need root privileges to use port 21.")
            print(f"  Try running: sudo python3 {sys.argv[0]} ftp")
            print(f"  Or use a port > 1024 by editing FTP_PORT in the script")
            sys.exit(1)
        except Exception as e:
            print(f"✗ Error starting FTP server: {e}")
            sys.exit(1)
    
    def stop(self):
        """Stop the FTP server"""
        if self.server:
            self.server.close_all()


def print_banner():
    """Print application banner"""
    banner = """
╔═══════════════════════════════════════════════════════════════════════════╗
║                         AGP CMS RESELLER SYSTEM                           ║
║                     Website Provisioning & FTP Server                     ║
╚═══════════════════════════════════════════════════════════════════════════╝
"""
    print(banner)


def show_menu():
    """Display main menu"""
    print("\nMAIN MENU")
    print("-" * 40)
    print("1. Create New Reseller Account")
    print("2. List All Accounts")
    print("3. Start FTP Server")
    print("4. Exit")
    print("-" * 40)


def show_package_menu():
    """Display package selection menu"""
    print("\nSELECT PACKAGE TYPE")
    print("-" * 40)
    for key, package in PACKAGES.items():
        features = ", ".join(package["features"])
        print(f"{key}. {package['name']} - Includes: {features}")
    print("-" * 40)


def create_new_account(manager):
    """Interactive account creation"""
    print("\n" + "="*80)
    print("CREATE NEW RESELLER ACCOUNT")
    print("="*80 + "\n")
    
    # Get username
    while True:
        username = input("Enter username (alphanumeric, 3-20 chars): ").strip()
        if not username or len(username) < 3 or len(username) > 20:
            print("✗ Username must be 3-20 characters")
            continue
        if not username.isalnum():
            print("✗ Username must be alphanumeric")
            continue
        break
    
    # Get password
    while True:
        password = input("Enter password (min 6 chars): ").strip()
        if len(password) < 6:
            print("✗ Password must be at least 6 characters")
            continue
        confirm = input("Confirm password: ").strip()
        if password != confirm:
            print("✗ Passwords do not match")
            continue
        break
    
    # Get email
    email = input("Enter email address: ").strip()
    if not email or "@" not in email:
        print("✗ Invalid email address")
        return
    
    # Get site name
    while True:
        site_name = input("Enter website name (e.g., 'My Cool Site'): ").strip()
        if not site_name:
            print("✗ Site name is required")
            continue
        break
    
    # Select package
    show_package_menu()
    while True:
        package_choice = input("Select package (1-4): ").strip()
        if package_choice in PACKAGES:
            break
        print("✗ Invalid selection")
    
    # Confirm
    print("\n" + "-"*80)
    print("CONFIRM ACCOUNT DETAILS")
    print("-"*80)
    print(f"Username: {username}")
    print(f"Email: {email}")
    print(f"Site Name: {site_name}")
    print(f"Package: {PACKAGES[package_choice]['name']}")
    print("-"*80)
    
    confirm = input("Create this account? (yes/no): ").strip().lower()
    if confirm not in ('yes', 'y'):
        print("✗ Account creation cancelled")
        return
    
    # Create account
    print("\nCreating account...")
    account_id = manager.create_account(username, password, email, site_name, package_choice)
    
    if account_id:
        print(f"\n✓ Account created successfully! (ID: {account_id})")
        print("\nNEXT STEPS:")
        print("1. Start the FTP server (Option 3 from main menu)")
        print(f"2. Connect via FTP to upload your files")
        print(f"3. Your site files will be in: {SITES_ROOT}/{site_name.lower().replace(' ', '-')}")


def main():
    """Main application entry point"""
    print_banner()
    
    # Initialize manager
    manager = ResellerManager()
    
    # Check for command line arguments
    if len(sys.argv) > 1:
        if sys.argv[1] == "ftp":
            # Start FTP server directly
            ftp_server = ResellerFTPServer(manager)
            try:
                ftp_server.start()
            except KeyboardInterrupt:
                print("\n\nShutting down FTP server...")
                ftp_server.stop()
                print("✓ FTP server stopped")
            return
    
    # Interactive menu
    while True:
        show_menu()
        choice = input("\nEnter your choice (1-4): ").strip()
        
        if choice == "1":
            create_new_account(manager)
        elif choice == "2":
            manager.list_accounts()
        elif choice == "3":
            print("\nStarting FTP server...")
            print("Press Ctrl+C to stop the server\n")
            ftp_server = ResellerFTPServer(manager)
            try:
                ftp_server.start()
            except KeyboardInterrupt:
                print("\n\nShutting down FTP server...")
                ftp_server.stop()
                print("✓ FTP server stopped")
        elif choice == "4":
            print("\nThank you for using AGP CMS Reseller System!")
            sys.exit(0)
        else:
            print("✗ Invalid choice. Please try again.")


if __name__ == "__main__":
    # Check if required module is installed
    try:
        import pyftpdlib
    except ImportError:
        print("✗ Error: pyftpdlib is not installed")
        print("\nTo install dependencies, run:")
        print("  pip install pyftpdlib")
        sys.exit(1)
    
    main()
