# AGP CMS Reseller System - Usage Examples

This document provides practical examples of how to use the AGP CMS Reseller System.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Creating Accounts](#creating-accounts)
3. [Starting FTP Server](#starting-ftp-server)
4. [Connecting via FTP](#connecting-via-ftp)
5. [Managing Sites](#managing-sites)
6. [Common Workflows](#common-workflows)

## Quick Start

### Installation

```bash
# Install dependencies
pip install -r requirements-reseller.txt

# Or install directly
pip install pyftpdlib
```

### Running the System

```bash
# Interactive mode
python3 reseller.py

# Or use quick start
./reseller-quickstart.sh   # Linux/Mac
reseller-quickstart.bat     # Windows
```

## Creating Accounts

### Example 1: Forum Package

Perfect for community discussions:

```python
from reseller import ResellerManager

manager = ResellerManager()

# Create a forum account
account_id = manager.create_account(
    username="gaming_community",
    password="GamingForum2024!",
    email="admin@gamingcommunity.com",
    site_name="Gaming Community Forum",
    package_type="1"  # Forum package
)

print(f"Account ID: {account_id}")
```

**Result:**
- Creates folder: `reseller_sites/gamingcommunityforum/`
- Includes: Discussion forums feature
- Default homepage created
- FTP access enabled

### Example 2: Blog Package

For bloggers and content creators:

```python
account_id = manager.create_account(
    username="tech_writer",
    password="TechBlog2024!",
    email="writer@techblog.com",
    site_name="Tech Insights Blog",
    package_type="2"  # Blog package
)
```

**Result:**
- Creates folder: `reseller_sites/techinsightsblog/`
- Includes: Blog system
- Ready for content publishing
- FTP access for uploading media

### Example 3: Website Package

For static websites:

```python
account_id = manager.create_account(
    username="small_business",
    password="Business2024!",
    email="info@smallbiz.com",
    site_name="Small Business Solutions",
    package_type="3"  # Website package
)
```

**Result:**
- Creates folder: `reseller_sites/smallbusinesssolutions/`
- Includes: Static website hosting
- Default professional homepage
- Upload custom HTML, CSS, JS files

### Example 4: Full Suite Package

Complete platform with all features:

```python
account_id = manager.create_account(
    username="mega_corp",
    password="Enterprise2024!",
    email="admin@megacorp.com",
    site_name="MegaCorp Enterprise Portal",
    package_type="4"  # Full Suite
)
```

**Result:**
- Creates folder: `reseller_sites/megacorpenterpriseportal/`
- Includes: Forums + Blog + Website + Downloads + Learning
- Full-featured platform
- Maximum flexibility

## Starting FTP Server

### Method 1: Interactive Menu

```bash
python3 reseller.py
# Select option 3 from menu
```

### Method 2: Direct Start

```bash
python3 reseller.py ftp
```

### Method 3: With Sudo (Linux/Mac)

```bash
sudo python3 reseller.py ftp
```

**Output:**
```
================================================================================
FTP SERVER STARTED
================================================================================
Host: 0.0.0.0
Port: 21
Max Connections: 256
Max Connections per IP: 5
================================================================================

✓ Added FTP user: gaming_community -> reseller_sites/gamingcommunityforum
✓ Added FTP user: tech_writer -> reseller_sites/techinsightsblog
✓ Added FTP user: small_business -> reseller_sites/smallbusinesssolutions
✓ Added FTP user: mega_corp -> reseller_sites/megacorpenterpriseportal
```

## Connecting via FTP

### Using Command Line FTP (Linux/Mac/Windows)

```bash
ftp localhost
# or
ftp your-server-ip
```

**Example Session:**
```
Connected to localhost.
220 AGP CMS Reseller FTP Server Ready
Name (localhost:user): gaming_community
331 Username ok, send password.
Password: GamingForum2024!
230 Login successful.
Remote system type is UNIX.
Using binary mode to transfer files.

ftp> ls
227 Entering passive mode (127,0,0,1,234,78)
150 File status okay. About to open data connection.
drwxrwxr-x   2 owner    group         4096 Nov 13 23:00 data
drwxrwxr-x   2 owner    group         4096 Nov 13 23:00 logs
drwxrwxr-x   2 owner    group         4096 Nov 13 23:00 uploads
drwxrwxr-x   2 owner    group         4096 Nov 13 23:00 wwwroot
-rw-rw-r--   1 owner    group          702 Nov 13 23:00 README.md
226 Transfer complete.

ftp> cd wwwroot
250 "/wwwroot" is the current directory.

ftp> put myfile.html
local: myfile.html remote: myfile.html
227 Entering passive mode (127,0,0,1,234,79)
150 File status okay. About to open data connection.
226 Transfer complete.
1234 bytes sent in 0.01 secs (120.4 kB/s)

ftp> quit
221 Goodbye.
```

### Using FileZilla (GUI Client)

1. **Open FileZilla**
2. **Enter connection details:**
   - Host: `localhost` or `your-server-ip`
   - Username: `gaming_community`
   - Password: `GamingForum2024!`
   - Port: `21`
3. **Click "Quickconnect"**
4. **Browse and upload files**

### Using WinSCP (Windows)

1. **Open WinSCP**
2. **New Session:**
   - File protocol: `FTP`
   - Host name: `localhost` or `your-server-ip`
   - Port number: `21`
   - User name: `gaming_community`
   - Password: `GamingForum2024!`
3. **Click "Login"**
4. **Drag and drop files**

## Managing Sites

### List All Accounts

```python
from reseller import ResellerManager

manager = ResellerManager()
manager.list_accounts()
```

**Output:**
```
================================================================================
RESELLER ACCOUNTS
================================================================================

ID: 1
Username: gaming_community
Email: admin@gamingcommunity.com
Site Name: Gaming Community Forum
Package: Forum
Status: active
Created: 2025-11-13T23:00:00.000000
--------------------------------------------------------------------------------

ID: 2
Username: tech_writer
Email: writer@techblog.com
Site Name: Tech Insights Blog
Package: Blog
Status: active
Created: 2025-11-13T23:05:00.000000
--------------------------------------------------------------------------------
```

### View Site Files

```bash
# View site structure
ls -la reseller_sites/gamingcommunityforum/

# Output:
# drwxrwxr-x 6 user user 4096 Nov 13 23:00 .
# drwxrwxr-x 3 user user 4096 Nov 13 23:00 ..
# -rw-rw-r-- 1 user user  702 Nov 13 23:00 README.md
# drwxrwxr-x 2 user user 4096 Nov 13 23:00 data
# drwxrwxr-x 2 user user 4096 Nov 13 23:00 logs
# drwxrwxr-x 2 user user 4096 Nov 13 23:00 uploads
# drwxrwxr-x 2 user user 4096 Nov 13 23:00 wwwroot
```

### View Default Homepage

```bash
cat reseller_sites/gamingcommunityforum/wwwroot/index.html
```

## Common Workflows

### Workflow 1: Onboard a New Customer

1. **Create account:**
   ```bash
   python3 reseller.py
   # Select option 1 (Create New Reseller Account)
   # Enter customer details
   ```

2. **Start FTP server:**
   ```bash
   python3 reseller.py ftp
   ```

3. **Send credentials to customer:**
   ```
   FTP Host: ftp.yourserver.com
   Username: customer_username
   Password: customer_password
   Port: 21
   ```

4. **Customer uploads their files to `wwwroot/`**

### Workflow 2: Bulk Account Creation

```python
from reseller import ResellerManager

manager = ResellerManager()

# Batch create accounts from a list
customers = [
    {"username": "client1", "password": "pass1", "email": "client1@example.com", 
     "site_name": "Client One Site", "package_type": "4"},
    {"username": "client2", "password": "pass2", "email": "client2@example.com", 
     "site_name": "Client Two Site", "package_type": "2"},
    {"username": "client3", "password": "pass3", "email": "client3@example.com", 
     "site_name": "Client Three Site", "package_type": "3"},
]

for customer in customers:
    account_id = manager.create_account(**customer)
    if account_id:
        print(f"✓ Created account for {customer['username']}")
    else:
        print(f"✗ Failed to create account for {customer['username']}")
```

### Workflow 3: Running as a Service

**Linux (systemd):**

```bash
# Create service file
sudo nano /etc/systemd/system/agp-reseller-ftp.service

# Add content:
[Unit]
Description=AGP CMS Reseller FTP Server
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/path/to/AGP_CMS
ExecStart=/usr/bin/python3 /path/to/AGP_CMS/reseller.py ftp
Restart=always

[Install]
WantedBy=multi-user.target

# Enable and start
sudo systemctl enable agp-reseller-ftp
sudo systemctl start agp-reseller-ftp
```

**Windows (NSSM):**

```cmd
# Install NSSM service manager
nssm install AGPResellerFTP "C:\Python39\python.exe" "C:\AGP_CMS\reseller.py ftp"
nssm start AGPResellerFTP
```

### Workflow 4: Customer File Upload Example

Customer uploads their website:

```bash
# Customer creates local files
mkdir my-website
cd my-website
echo "<h1>Welcome to My Site</h1>" > index.html
echo "body { font-family: Arial; }" > style.css

# Upload via FTP
ftp localhost
# Login with credentials
cd wwwroot
put index.html
put style.css
quit
```

### Workflow 5: Backup Customer Sites

```bash
# Backup all sites
tar -czf reseller_sites_backup_$(date +%Y%m%d).tar.gz reseller_sites/

# Backup database
cp reseller_accounts.db reseller_accounts_backup_$(date +%Y%m%d).db

# Or backup specific site
tar -czf client1_backup.tar.gz reseller_sites/clientonesite/
```

### Workflow 6: Migrate Customer to Larger Package

```python
# Note: Currently requires manual steps
# 1. Update database
# 2. Add new features to site_features table
# 3. Notify customer of new features

import sqlite3

conn = sqlite3.connect("reseller_accounts.db")
cursor = conn.cursor()

# Update package type
cursor.execute("""
    UPDATE reseller_accounts 
    SET package_type = '4', updated_at = datetime('now')
    WHERE username = 'customer_username'
""")

# Add new features
account_id = cursor.execute(
    "SELECT id FROM reseller_accounts WHERE username = 'customer_username'"
).fetchone()[0]

new_features = ['downloads', 'learning']
for feature in new_features:
    cursor.execute("""
        INSERT INTO site_features (account_id, feature_name, enabled)
        VALUES (?, ?, 1)
    """, (account_id, feature))

conn.commit()
conn.close()

print("✓ Customer upgraded to Full Suite package")
```

## Demo Script

For a complete demonstration, use the included demo script:

```bash
# Run complete demo
python3 reseller_demo.py auto

# Or interactive demo
python3 reseller_demo.py
```

This creates 4 sample accounts (one for each package type) and shows all features.

## Troubleshooting

### Port 21 Permission Denied

```bash
# Solution 1: Run with sudo
sudo python3 reseller.py ftp

# Solution 2: Change port (edit reseller.py)
FTP_PORT = 2121  # Use port > 1024
```

### Account Already Exists

```bash
# Check existing accounts
python3 reseller.py
# Select option 2 (List All Accounts)

# Or programmatically
python3 -c "from reseller import ResellerManager; ResellerManager().list_accounts()"
```

### FTP Connection Timeout

1. Check firewall allows port 21
2. Check passive ports (60000-60100) are open
3. Verify FTP server is running: `netstat -an | grep :21`

## Additional Resources

- [RESELLER_README.md](RESELLER_README.md) - Full documentation
- [LegendaryCMS/README.md](LegendaryCMS/README.md) - Main CMS docs
- [reseller_demo.py](reseller_demo.py) - Demo script

---

**AGP CMS Reseller System** - Complete Website Provisioning Solution
