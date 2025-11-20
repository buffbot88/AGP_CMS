# FTP Server Configuration for AGP CMS Web Market

This guide helps you set up an FTP server to work with the AGP CMS Web Market.

## Quick Start Options

The Web Market provides FTP credentials and manages user accounts, but you need to configure an actual FTP server separately. Choose one of these options:

### Option 1: External FTP Server (Recommended for Production)

Use a professional FTP server configured to authenticate against the Web Market database.

#### For Linux: ProFTPD with SQLite

1. Install ProFTPD:
```bash
sudo apt-get update
sudo apt-get install proftpd proftpd-mod-sqlite
```

2. Create ProFTPD SQL configuration `/etc/proftpd/sql.conf`:
```apache
<IfModule mod_sql.c>
  # SQLite configuration
  SQLBackend sqlite3
  
  # Path to your database
  SQLConnectInfo /path/to/agp_cms.db
  
  # SHA256 password hashing
  SQLAuthTypes SHA256
  
  # User authentication
  SQLUserInfo WebMarketAccounts Username FtpPassword UserId -1 SitePath /bin/bash
  
  # Only allow active users with FTP enabled
  SQLUserWhereClause "Status='active' AND FtpEnabled=1"
  
  # Log SQL queries (for debugging)
  SQLLogFile /var/log/proftpd/sql.log
</IfModule>
```

3. Update main ProFTPD config `/etc/proftpd/proftpd.conf`:
```apache
# Include SQL configuration
Include /etc/proftpd/sql.conf

# Basic settings
ServerName "AGP CMS Web Market FTP"
ServerType standalone
DefaultServer on
Port 21

# Use the correct user/group
User proftpd
Group nogroup

# Umask
Umask 022

# Limit login attempts
MaxLoginAttempts 3

# Passive mode ports
PassivePorts 60000 60100

# Logging
TransferLog /var/log/proftpd/xferlog
SystemLog /var/log/proftpd/proftpd.log

# Security
<IfModule mod_tls.c>
  TLSEngine off
</IfModule>
```

4. Set permissions:
```bash
sudo chown -R proftpd:nogroup /path/to/reseller_sites
sudo chmod -R 755 /path/to/reseller_sites
```

5. Restart ProFTPD:
```bash
sudo systemctl restart proftpd
sudo systemctl enable proftpd
```

6. Test FTP connection:
```bash
ftp localhost
# Enter username and FTP password from Web Market
```

#### For Windows: FileZilla Server

1. Download [FileZilla Server](https://filezilla-project.org/download.php?type=server)

2. Install and open FileZilla Server

3. **Manual User Management** (for now):
   - For each Web Market customer, manually add user in FileZilla
   - Username: From `WebMarketAccounts.Username`
   - Password: Customer's FTP password (they know it, you have the hash)
   - Home directory: From `WebMarketAccounts.SitePath`

4. **OR Create PowerShell Script** to sync users:

```powershell
# sync-ftp-users.ps1
$dbPath = "C:\path\to\agp_cms.db"
$ftpConfigPath = "C:\FileZilla Server\FileZilla Server.xml"

# Query database
Add-Type -Path "System.Data.SQLite.dll"
$connection = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$dbPath")
$connection.Open()

$command = $connection.CreateCommand()
$command.CommandText = "SELECT Username, SitePath FROM WebMarketAccounts WHERE Status='active' AND FtpEnabled=1"
$reader = $command.ExecuteReader()

while ($reader.Read()) {
    $username = $reader["Username"]
    $homedir = $reader["SitePath"]
    
    # Add user to FileZilla (requires FileZilla Server API or XML manipulation)
    Write-Host "User: $username - Home: $homedir"
}

$connection.Close()
```

### Option 2: Simple Development FTP Server

For development/testing only, you can use a simple Python FTP server:

1. Create `ftp_server.py`:
```python
#!/usr/bin/env python3
import sqlite3
import hashlib
from pathlib import Path
from pyftpdlib.authorizers import DummyAuthorizer
from pyftpdlib.handlers import FTPHandler
from pyftpdlib.servers import FTPServer

DB_PATH = "AGP_CMS/agp_cms.db"
FTP_PORT = 21
FTP_HOST = "0.0.0.0"

def get_active_accounts():
    """Get all active FTP accounts from database"""
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    cursor.execute("""
        SELECT Username, FtpPassword, SitePath
        FROM WebMarketAccounts
        WHERE Status = 'active' AND FtpEnabled = 1
    """)
    
    accounts = cursor.fetchall()
    conn.close()
    return accounts

def main():
    authorizer = DummyAuthorizer()
    
    # Add all active accounts
    accounts = get_active_accounts()
    for username, password_hash, site_path in accounts:
        # Note: DummyAuthorizer expects plaintext password
        # For production, use a custom authorizer that checks hashes
        temp_password = password_hash[:16]  # Temporary workaround
        
        try:
            authorizer.add_user(
                username=username,
                password=temp_password,
                homedir=site_path,
                perm="elradfmwMT"
            )
            print(f"✓ Added FTP user: {username} -> {site_path}")
        except Exception as e:
            print(f"✗ Error adding user {username}: {e}")
    
    # Configure handler
    handler = FTPHandler
    handler.authorizer = authorizer
    handler.banner = "AGP CMS Web Market FTP Server"
    handler.passive_ports = range(60000, 60100)
    
    # Start server
    server = FTPServer((FTP_HOST, FTP_PORT), handler)
    server.max_cons = 256
    server.max_cons_per_ip = 5
    
    print(f"\n{'='*80}")
    print(f"FTP SERVER STARTED")
    print(f"{'='*80}")
    print(f"Host: {FTP_HOST}")
    print(f"Port: {FTP_PORT}")
    print(f"Active Users: {len(accounts)}")
    print(f"{'='*80}\n")
    
    server.serve_forever()

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\nShutting down FTP server...")
```

2. Install dependencies:
```bash
pip install pyftpdlib
```

3. Run the server:
```bash
# On Linux, requires sudo for port 21
sudo python3 ftp_server.py

# Or use a different port (edit FTP_PORT in script)
python3 ftp_server.py
```

### Option 3: Use Existing FTP Infrastructure

If you already have an FTP server:

1. Query the Web Market database to get active users:
```sql
SELECT Username, SitePath, Email
FROM WebMarketAccounts
WHERE Status = 'active' AND FtpEnabled = 1;
```

2. Add these users to your existing FTP server manually or via script

3. Set their home directory to the `SitePath` value

## Testing FTP Access

### Using Command Line FTP

```bash
ftp localhost
# Enter username and password
ls
cd wwwroot
put myfile.html
quit
```

### Using FileZilla Client

1. Open FileZilla Client
2. Host: localhost (or your server IP)
3. Username: Your Web Market username
4. Password: Your FTP password from registration
5. Port: 21
6. Click "Quickconnect"

### Testing with curl

```bash
curl -u username:password ftp://localhost/
curl -u username:password -T file.html ftp://localhost/wwwroot/
```

## Security Best Practices

### 1. Use FTPS (FTP over SSL/TLS)

For ProFTPD, enable TLS:

```apache
<IfModule mod_tls.c>
  TLSEngine on
  TLSLog /var/log/proftpd/tls.log
  TLSProtocol TLSv1.2 TLSv1.3
  
  # Path to certificate
  TLSRSACertificateFile /etc/ssl/certs/proftpd.crt
  TLSRSACertificateKeyFile /etc/ssl/private/proftpd.key
  
  # Require encryption
  TLSRequired on
</IfModule>
```

### 2. Limit Login Attempts

```apache
MaxLoginAttempts 3
```

### 3. Use Firewall Rules

```bash
# Allow FTP
sudo ufw allow 21/tcp
sudo ufw allow 60000:60100/tcp  # Passive ports
sudo ufw enable
```

### 4. Monitor FTP Logs

```bash
tail -f /var/log/proftpd/proftpd.log
tail -f /var/log/proftpd/xferlog
```

### 5. Regular Security Updates

```bash
sudo apt-get update
sudo apt-get upgrade proftpd
```

## Troubleshooting

### Issue: Cannot connect to FTP server

1. Check if FTP server is running:
```bash
sudo systemctl status proftpd
```

2. Check if port 21 is listening:
```bash
sudo netstat -tlnp | grep :21
```

3. Check firewall:
```bash
sudo ufw status
```

### Issue: Authentication fails

1. Verify user exists in database:
```sql
SELECT * FROM WebMarketAccounts WHERE Username = 'username';
```

2. Check password hash format (should be SHA256)

3. Verify Status = 'active' and FtpEnabled = 1

4. Check SQL logs:
```bash
tail -f /var/log/proftpd/sql.log
```

### Issue: Cannot access files after login

1. Verify SitePath in database matches actual directory
2. Check directory permissions:
```bash
ls -la /path/to/reseller_sites/
```

3. Ensure FTP user has read/write permissions

### Issue: Passive mode connection fails

1. Check passive port range is open in firewall
2. Verify passive ports configuration in FTP server
3. For cloud servers, ensure passive ports are forwarded

## Performance Tuning

### For High Traffic Sites

1. **Increase connection limits**:
```apache
MaxClients 100
MaxHostsPerIP 5
```

2. **Enable connection pooling** for database:
```apache
SQLConnectInfo /path/to/db timeout=10 pool_size=10
```

3. **Use caching** for authentication queries

4. **Monitor resource usage**:
```bash
htop
iotop
```

## Automation Scripts

### Auto-sync FTP users (Linux cron job)

Create `/usr/local/bin/sync-ftp-users.sh`:

```bash
#!/bin/bash
# Sync Web Market users to ProFTPD
# Run this script periodically via cron

DB_PATH="/path/to/agp_cms.db"

# Get all active users
sqlite3 $DB_PATH "SELECT Username, SitePath FROM WebMarketAccounts WHERE Status='active' AND FtpEnabled=1" | while IFS='|' read username sitepath; do
    echo "User: $username - Path: $sitepath"
    # Add logic to sync with FTP server
done

# Reload FTP server
sudo systemctl reload proftpd
```

Make it executable and add to cron:
```bash
chmod +x /usr/local/bin/sync-ftp-users.sh
crontab -e
# Add: */15 * * * * /usr/local/bin/sync-ftp-users.sh
```

## Support

If you need help:

1. Check FTP server logs
2. Verify database connection
3. Test with a simple FTP client first
4. Review Web Market settings in admin panel

---

**AGP CMS Web Market** - FTP Configuration Guide
