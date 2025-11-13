#!/usr/bin/env python3
"""
Demo script for AGP CMS Reseller System
Shows how to use the reseller system programmatically
"""

import sys
from reseller import ResellerManager, PACKAGES

def demo_account_creation():
    """Demonstrate creating accounts programmatically"""
    print("=" * 80)
    print("DEMO: Creating Reseller Accounts Programmatically")
    print("=" * 80)
    print()
    
    manager = ResellerManager()
    
    # Example accounts for different use cases
    demo_accounts = [
        {
            "username": "communityforum",
            "password": "forum2024!",
            "email": "admin@communityforum.com",
            "site_name": "Community Discussion Forum",
            "package_type": "1",  # Forum only
            "description": "A community forum for discussions"
        },
        {
            "username": "techblogger",
            "password": "blog2024!",
            "email": "writer@techblog.com",
            "site_name": "Tech Blog Platform",
            "package_type": "2",  # Blog only
            "description": "A technical blog platform"
        },
        {
            "username": "smallbiz",
            "password": "biz2024!",
            "email": "info@smallbiz.com",
            "site_name": "Small Business Website",
            "package_type": "3",  # Website only
            "description": "A small business website"
        },
        {
            "username": "enterprise",
            "password": "corp2024!",
            "email": "admin@enterprise.com",
            "site_name": "Enterprise Portal",
            "package_type": "4",  # Full suite
            "description": "A complete enterprise portal"
        }
    ]
    
    created_accounts = []
    
    for account in demo_accounts:
        print(f"\n📦 Creating: {account['description']}")
        print(f"   Username: {account['username']}")
        print(f"   Package: {PACKAGES[account['package_type']]['name']}")
        
        account_id = manager.create_account(
            username=account['username'],
            password=account['password'],
            email=account['email'],
            site_name=account['site_name'],
            package_type=account['package_type']
        )
        
        if account_id:
            created_accounts.append({
                'username': account['username'],
                'password': account['password'],
                'email': account['email'],
                'site_name': account['site_name']
            })
            print(f"   ✓ Account created successfully!")
        else:
            print(f"   ✗ Account already exists or error occurred")
        
        print("-" * 80)
    
    return created_accounts


def demo_ftp_credentials(accounts):
    """Show FTP connection information"""
    print("\n" + "=" * 80)
    print("DEMO: FTP Connection Information")
    print("=" * 80)
    print()
    
    print("Your customers can connect to their sites using these credentials:\n")
    
    for i, account in enumerate(accounts, 1):
        print(f"{i}. {account['site_name']}")
        print(f"   FTP Host: ftp://localhost:21")
        print(f"   Username: {account['username']}")
        print(f"   Password: {account['password']}")
        print(f"   Email: {account['email']}")
        print()
    
    print("=" * 80)
    print("To start the FTP server, run:")
    print("  python3 reseller.py ftp")
    print("  (or with sudo if using port 21)")
    print("=" * 80)


def demo_site_structure():
    """Show the site directory structure"""
    import os
    from pathlib import Path
    
    print("\n" + "=" * 80)
    print("DEMO: Site Directory Structure")
    print("=" * 80)
    print()
    
    sites_root = Path("reseller_sites")
    
    if not sites_root.exists() or not any(sites_root.iterdir()):
        print("No sites created yet. Run demo_account_creation() first.")
        return
    
    print("Each customer site has the following structure:\n")
    
    for site_dir in sorted(sites_root.iterdir()):
        if site_dir.is_dir():
            print(f"📁 {site_dir.name}/")
            
            for root, dirs, files in os.walk(site_dir):
                level = root.replace(str(site_dir), '').count(os.sep)
                indent = '  ' * (level + 1)
                subdir = os.path.basename(root)
                
                if level > 0:
                    print(f"{indent}📁 {subdir}/")
                
                file_indent = '  ' * (level + 2)
                for file in sorted(files):
                    if file.endswith('.html'):
                        print(f"{file_indent}📄 {file} (Website)")
                    elif file.endswith('.md'):
                        print(f"{file_indent}📝 {file} (Documentation)")
                    else:
                        print(f"{file_indent}📄 {file}")
            
            print()


def demo_list_all():
    """List all accounts in the system"""
    print("\n" + "=" * 80)
    print("DEMO: Listing All Reseller Accounts")
    print("=" * 80)
    print()
    
    manager = ResellerManager()
    manager.list_accounts()


def cleanup_demo_accounts():
    """Clean up demo accounts (use with caution!)"""
    import sqlite3
    import shutil
    from pathlib import Path
    
    print("\n" + "=" * 80)
    print("CLEANUP: Removing Demo Accounts")
    print("=" * 80)
    print()
    
    response = input("⚠️  This will delete ALL demo accounts and their files. Continue? (yes/no): ")
    
    if response.lower() != 'yes':
        print("Cleanup cancelled.")
        return
    
    try:
        # Remove database
        db_path = "reseller_accounts.db"
        if Path(db_path).exists():
            Path(db_path).unlink()
            print(f"✓ Removed {db_path}")
        
        # Remove sites directory
        sites_path = Path("reseller_sites")
        if sites_path.exists():
            shutil.rmtree(sites_path)
            print(f"✓ Removed {sites_path}/")
        
        print("\n✓ Cleanup complete!")
        
    except Exception as e:
        print(f"\n✗ Error during cleanup: {e}")


def print_menu():
    """Print demo menu"""
    print("\n" + "=" * 80)
    print("AGP CMS RESELLER SYSTEM - DEMO")
    print("=" * 80)
    print()
    print("1. Create Demo Accounts (4 different package types)")
    print("2. Show FTP Connection Information")
    print("3. Show Site Directory Structure")
    print("4. List All Accounts")
    print("5. Cleanup Demo Accounts (Delete All)")
    print("6. Run Complete Demo (Options 1-4)")
    print("7. Exit")
    print()
    print("=" * 80)


def run_complete_demo():
    """Run a complete demonstration"""
    print("\n" + "=" * 80)
    print("RUNNING COMPLETE DEMO")
    print("=" * 80)
    print()
    print("This will demonstrate all features of the reseller system.\n")
    
    # Step 1: Create accounts
    print("\n🔷 STEP 1: Creating Demo Accounts")
    accounts = demo_account_creation()
    
    if not accounts:
        print("\n⚠️  No new accounts were created. They may already exist.")
        print("   Run option 5 to cleanup and try again.")
        return
    
    # Step 2: Show FTP info
    print("\n🔷 STEP 2: FTP Connection Information")
    demo_ftp_credentials(accounts)
    
    # Step 3: Show structure
    print("\n🔷 STEP 3: Site Directory Structure")
    demo_site_structure()
    
    # Step 4: List accounts
    print("\n🔷 STEP 4: All Accounts in Database")
    demo_list_all()
    
    print("\n" + "=" * 80)
    print("DEMO COMPLETE!")
    print("=" * 80)
    print()
    print("Next steps:")
    print("1. Start the FTP server: python3 reseller.py ftp")
    print("2. Connect with an FTP client using the credentials shown above")
    print("3. Upload files to customize each customer's website")
    print("4. Use option 5 from the demo menu to cleanup when done")
    print()


def main():
    """Main demo entry point"""
    print("""
╔═══════════════════════════════════════════════════════════════════════════╗
║                   AGP CMS RESELLER SYSTEM - DEMO                          ║
║                                                                           ║
║  This demo script shows how to use the reseller system programmatically  ║
╚═══════════════════════════════════════════════════════════════════════════╝
""")
    
    created_accounts = []
    
    while True:
        print_menu()
        choice = input("Select an option (1-7): ").strip()
        
        if choice == "1":
            created_accounts = demo_account_creation()
        elif choice == "2":
            if not created_accounts:
                print("\n⚠️  No accounts in memory. Create accounts first (option 1) or they may already exist.")
                demo_ftp_credentials([
                    {'username': 'communityforum', 'password': 'forum2024!', 'email': 'admin@communityforum.com', 'site_name': 'Community Discussion Forum'},
                    {'username': 'techblogger', 'password': 'blog2024!', 'email': 'writer@techblog.com', 'site_name': 'Tech Blog Platform'},
                    {'username': 'smallbiz', 'password': 'biz2024!', 'email': 'info@smallbiz.com', 'site_name': 'Small Business Website'},
                    {'username': 'enterprise', 'password': 'corp2024!', 'email': 'admin@enterprise.com', 'site_name': 'Enterprise Portal'}
                ])
            else:
                demo_ftp_credentials(created_accounts)
        elif choice == "3":
            demo_site_structure()
        elif choice == "4":
            demo_list_all()
        elif choice == "5":
            cleanup_demo_accounts()
            created_accounts = []
        elif choice == "6":
            run_complete_demo()
        elif choice == "7":
            print("\nThank you for trying the AGP CMS Reseller System Demo!")
            print("For production use, run: python3 reseller.py\n")
            sys.exit(0)
        else:
            print("\n✗ Invalid choice. Please select 1-7.")


if __name__ == "__main__":
    # Check if running in non-interactive mode
    if len(sys.argv) > 1:
        if sys.argv[1] == "auto":
            print("Running in automatic mode...\n")
            run_complete_demo()
            sys.exit(0)
    
    # Interactive mode
    main()
