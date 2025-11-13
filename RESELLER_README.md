# AGP CMS Reseller System

A complete reseller solution for AGP CMS that allows you to provision and sell forum, blog, website, or full suite packages to customers with built-in FTP access.

## Features

- 🏪 **Multiple Package Types**
  - Forum Package - Discussion forums
  - Blog Package - Blogging platform
  - Website Package - Static website hosting
  - Full Suite - Complete CMS with all features

- 📁 **Automatic Provisioning**
  - Creates isolated site directories for each customer
  - Generates default website files
  - Sets up proper folder structure

- 🔐 **User Management**
  - Secure account creation with hashed passwords
  - Email and username validation
  - Account status tracking

- 📤 **FTP Server**
  - Built-in FTP server on port 21
  - Per-user home directory isolation
  - Full file management capabilities
  - Upload, download, delete, and modify files

- 🗄️ **Database Management**
  - SQLite database for account tracking
  - Stores user credentials, site information, and features
  - Easy to backup and migrate

## Installation

### Prerequisites

- Python 3.7 or higher
- pip (Python package manager)

### Install Dependencies

```bash
pip install -r requirements-reseller.txt
```

Or install manually:

```bash
pip install pyftpdlib
```

## Usage

### Interactive Mode

Run the reseller script in interactive mode:

```bash
python3 reseller.py
```

This will display a menu with the following options:

1. **Create New Reseller Account** - Set up a new customer account
2. **List All Accounts** - View all existing reseller accounts
3. **Start FTP Server** - Launch the FTP server for file access
4. **Exit** - Close the application

### Direct FTP Server Mode

Start only the FTP server (useful for running as a service):

```bash
python3 reseller.py ftp
```

**Note on Port 21:** On Unix/Linux systems, port 21 requires root privileges. You can either:
- Run with sudo: `sudo python3 reseller.py ftp`
- Or edit `FTP_PORT` in the script to use a port > 1024 (e.g., 2121)

## Creating a Reseller Account

Follow the interactive prompts:

1. **Username**: 3-20 alphanumeric characters
2. **Password**: Minimum 6 characters (will be hashed)
3. **Email**: Valid email address
4. **Website Name**: Display name for the site (e.g., "My Cool Website")
5. **Package Type**: Choose from 4 package options

### Package Types

| Package | Includes | Use Case |
|---------|----------|----------|
| Forum | Discussion forums | Community sites |
| Blog | Blog system | Personal or corporate blogs |
| Website | Static website hosting | Portfolio, business sites |
| Full Suite | Forums + Blog + Website + Downloads + Learning | Complete platform |

## Directory Structure

Each customer site is created with the following structure:

```
reseller_sites/
└── customer-site-name/
    ├── wwwroot/          # Web files (HTML, CSS, JS, images)
    │   └── index.html    # Default homepage
    ├── data/             # Database and data files
    ├── uploads/          # User uploaded files
    ├── logs/             # System logs
    └── README.md         # Site documentation
```

## FTP Access

### For Resellers

Start the FTP server:

```bash
python3 reseller.py ftp
```

The server will run on:
- **Host**: 0.0.0.0 (all interfaces)
- **Port**: 21 (configurable in script)

### For Customers

Customers can connect via any FTP client using:

```
Host: your-server-ip
Port: 21
Username: their-username
Password: their-password
```

Example with command line FTP:

```bash
ftp your-server-ip
# Enter username and password when prompted
```

Example with FileZilla:
1. Host: `ftp://your-server-ip`
2. Username: `customer-username`
3. Password: `customer-password`
4. Port: 21

## Database

The reseller system uses SQLite for data storage:

- **File**: `reseller_accounts.db`
- **Location**: Same directory as reseller.py

### Tables

#### reseller_accounts
- Stores customer account information
- Includes username, hashed password, email, site details
- Tracks creation date and status

#### site_features
- Maps features to accounts
- Allows enabling/disabling specific features per account

## Configuration

Edit these variables at the top of `reseller.py`:

```python
RESELLER_DB = "reseller_accounts.db"     # Database file
CMS_DB = "LegendaryCMS/agp_cms.db"       # Main CMS database
SITES_ROOT = "reseller_sites"             # Root directory for sites
FTP_PORT = 21                             # FTP server port
FTP_HOST = "0.0.0.0"                      # FTP bind address
```

## Security Considerations

1. **Passwords**: All passwords are hashed using SHA256 before storage
2. **FTP Security**: 
   - FTP transmits in plaintext - consider using FTPS or SFTP in production
   - Each user is isolated to their home directory
   - Passive ports: 60000-60100
3. **File Permissions**: Users have full access to their directory only
4. **Database**: Keep `reseller_accounts.db` secure with proper file permissions

## Running as a Service

### Linux (systemd)

Create `/etc/systemd/system/agp-reseller-ftp.service`:

```ini
[Unit]
Description=AGP CMS Reseller FTP Server
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/path/to/AGP_CMS
ExecStart=/usr/bin/python3 /path/to/AGP_CMS/reseller.py ftp
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl enable agp-reseller-ftp
sudo systemctl start agp-reseller-ftp
sudo systemctl status agp-reseller-ftp
```

### Windows (NSSM)

1. Download NSSM (Non-Sucking Service Manager)
2. Install the service:

```cmd
nssm install AGPResellerFTP "C:\Python39\python.exe" "C:\path\to\reseller.py ftp"
nssm set AGPResellerFTP AppDirectory "C:\path\to\AGP_CMS"
nssm start AGPResellerFTP
```

## Troubleshooting

### Port 21 Permission Denied

**Error**: `Permission denied to bind to port 21`

**Solution**: Run with sudo or change FTP_PORT to a value > 1024

```bash
sudo python3 reseller.py ftp
# OR
# Edit reseller.py and change: FTP_PORT = 2121
```

### FTP Connection Timeout

**Problem**: FTP client can't connect

**Solutions**:
1. Check firewall rules allow port 21
2. Check passive ports (60000-60100) are open
3. Verify FTP server is running: `netstat -an | grep :21`

### Site Folder Already Exists

**Error**: Site folder already exists when creating account

**Solution**: Choose a different website name or manually remove the old folder

### Database Locked

**Error**: Database is locked

**Solution**: Close any other processes accessing the database

## Backup and Maintenance

### Backup Customer Sites

```bash
# Backup all sites
tar -czf reseller_sites_backup.tar.gz reseller_sites/

# Backup database
cp reseller_accounts.db reseller_accounts.db.backup
```

### View Database Contents

```bash
sqlite3 reseller_accounts.db
> SELECT * FROM reseller_accounts;
> .quit
```

## Integration with AGP CMS

The reseller system is designed to work alongside AGP CMS:

1. **Separate Database**: Uses its own `reseller_accounts.db`
2. **Isolated Sites**: Each customer gets their own directory
3. **No Conflicts**: Does not interfere with main CMS operation

You can run both the main AGP CMS and the reseller FTP server simultaneously:

```bash
# Terminal 1: Run AGP CMS
cd LegendaryCMS
dotnet run

# Terminal 2: Run Reseller FTP Server
python3 reseller.py ftp
```

## Examples

### Example 1: Create a Blog Package

```bash
$ python3 reseller.py
# Select option 1 (Create New Reseller Account)
Username: johnblogger
Password: secure123
Email: john@example.com
Website Name: John's Tech Blog
Package: 2 (Blog)
```

Result: Creates `reseller_sites/johntechblog/` with blog features

### Example 2: Create Full Suite

```bash
$ python3 reseller.py
# Select option 1
Username: megacorp
Password: corporate2024
Email: admin@megacorp.com
Website Name: MegaCorp Solutions
Package: 4 (Full Suite)
```

Result: Creates full-featured site with all modules

## API Reference

The reseller script provides a `ResellerManager` class that can be imported:

```python
from reseller import ResellerManager

manager = ResellerManager()

# Create account programmatically
account_id = manager.create_account(
    username="testuser",
    password="testpass",
    email="test@example.com",
    site_name="Test Site",
    package_type="4"
)

# List accounts
manager.list_accounts()

# Get credentials for FTP
creds = manager.get_account_credentials("testuser")
```

## Support

For issues or questions:
- Check this README
- Review the AGP CMS main README
- Open an issue on GitHub

## License

See the LICENSE file in the main repository.

---

**AGP CMS Reseller System** - Website Provisioning & FTP Server
