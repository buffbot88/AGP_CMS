# AGP CMS Web Market

The AGP CMS Web Market is a comprehensive C# web hosting reseller system built with ASP.NET Core Razor Pages. It replaces the Python-based reseller script with a fully integrated, professional web marketplace.

## Features

### Customer-Facing Features
- **Package Selection**: Choose from 4 different hosting packages (Forum, Blog, Website, Full Suite)
- **Self-Service Registration**: Customers can register for hosting with their AGP CMS account
- **Personal Dashboard**: Manage hosting account, view FTP details, and access site information
- **Custom Website URLs**: Each customer gets their own URL at `/user_websites/{site-slug}`
- **FTP Access**: Full FTP credentials provided for file management
- **Automatic Site Provisioning**: Directory structure and default files created automatically

### Admin Features
- **Web Market Management**: View all customer accounts at `/Admin/WebMarket`
- **Package Management**: Four pre-configured packages with different features
- **Account Statistics**: Track total accounts, active accounts, and FTP-enabled accounts
- **Customer Overview**: See all customer sites, packages, and status at a glance

## How It Works

### 1. Customer Registration Flow

1. **Create AGP CMS Account**: Customer creates a regular user account
2. **Browse Packages**: Visit `/WebMarket` to view available hosting packages
3. **Select Package**: Choose from Forum ($9.99), Blog ($14.99), Website ($19.99), or Full Suite ($39.99)
4. **Register for Hosting**: Fill out registration form with:
   - Website name (becomes the site title and URL slug)
   - FTP password (separate from CMS password for security)
5. **Account Created**: System automatically:
   - Creates directory structure in `reseller_sites/{site-slug}/`
   - Creates web-accessible folder in `wwwroot/user_websites/{site-slug}/`
   - Generates default `index.html` with package information
   - Creates `README.md` with site documentation
   - Sets up database records

### 2. Customer Dashboard

After registration, customers can access their dashboard at `/WebMarket/Dashboard` to view:

- **Website URL**: Direct link to their live site
- **Package Details**: Current package and features
- **FTP Connection Info**: Host, port, username for FTP access
- **Directory Structure**: File organization guide
- **Account Information**: Creation date, status, and details

### 3. FTP Access

Customers receive FTP credentials:
- **Host**: localhost (or configured server hostname)
- **Port**: 21 (or configured FTP port)
- **Username**: Their AGP CMS username
- **Password**: The FTP password they set during registration
- **Home Directory**: `reseller_sites/{site-slug}/`

Files uploaded to `wwwroot/` within their FTP directory are automatically accessible at:
```
http://yourdomain.com/user_websites/{site-slug}/
```

## Directory Structure

### Per-Customer Site Structure
```
reseller_sites/{site-slug}/
├── wwwroot/          # Web files (HTML, CSS, JS, images)
│   └── index.html    # Default homepage
├── data/             # Database and data files
├── uploads/          # User uploaded files
├── logs/             # System logs
└── README.md         # Site documentation
```

### Web-Accessible Structure
```
wwwroot/user_websites/{site-slug}/
└── index.html        # Mirrored from reseller_sites
```

## Database Schema

### WebMarketPackages
Stores available hosting packages:
- `Id`: Package identifier
- `Name`: Package name (Forum, Blog, Website, Full Suite)
- `Description`: Package description
- `Price`: Monthly price
- `Features`: Comma-separated feature list
- `IsActive`: Whether package is available for purchase

### WebMarketAccounts
Stores customer hosting accounts:
- `Id`: Account identifier
- `UserId`: Link to AGP CMS user
- `Username`: FTP username (same as CMS username)
- `Email`: Customer email
- `SiteName`: Display name for the site
- `SiteSlug`: URL-safe slug for the site
- `PackageId`: Selected package
- `SitePath`: Path to reseller_sites directory
- `WebPath`: Path to wwwroot/user_websites directory
- `FtpEnabled`: Whether FTP is enabled
- `FtpPassword`: Hashed FTP password
- `Status`: Account status (active/suspended)
- `CreatedAt`: Account creation timestamp
- `UpdatedAt`: Last update timestamp

### WebMarketFeatures
Links features to customer accounts:
- `Id`: Feature identifier
- `AccountId`: Link to WebMarketAccounts
- `FeatureName`: Feature name (forum, blog, website, downloads)
- `Enabled`: Whether feature is active

## Available Packages

### 1. Forum Package - $9.99/month
- Discussion forums
- Perfect for community sites

### 2. Blog Package - $14.99/month
- Blog system
- Ideal for content creators

### 3. Website Package - $19.99/month
- Static website hosting
- Full FTP access
- Great for portfolios and business sites

### 4. Full Suite - $39.99/month (Most Popular)
- Discussion forums
- Blog system
- Website hosting
- File downloads
- Complete platform with all features

## Configuration

### Application Settings

In `appsettings.json`, you can configure:

```json
{
  "WebMarket": {
    "SitesRootPath": "reseller_sites",
    "WebRootPath": "wwwroot/user_websites"
  }
}
```

### FTP Server Setup

The Web Market provides FTP credentials but **does not include a built-in FTP server**. You need to configure an external FTP server.

#### Option 1: Using FileZilla Server (Windows)

1. Download and install [FileZilla Server](https://filezilla-project.org/download.php?type=server)
2. Configure users by querying the `WebMarketAccounts` table
3. Set home directories to match `SitePath` from database
4. Use custom authentication to validate against hashed `FtpPassword`

#### Option 2: Using ProFTPD (Linux)

1. Install ProFTPD:
   ```bash
   sudo apt-get install proftpd
   ```

2. Configure SQL authentication in `/etc/proftpd/sql.conf`:
   ```
   SQLConnectInfo agp_cms.db
   SQLAuthTypes SHA256
   SQLUserInfo WebMarketAccounts Username FtpPassword UserId -1 SitePath /bin/bash
   SQLUserWhereClause "Status='active' AND FtpEnabled=1"
   ```

3. Restart ProFTPD:
   ```bash
   sudo systemctl restart proftpd
   ```

#### Option 3: Using vsftpd (Linux)

1. Install vsftpd:
   ```bash
   sudo apt-get install vsftpd
   ```

2. Configure PAM authentication to check against the database
3. Set up user directories based on `SitePath`

### Getting FTP User List Programmatically

The `FTPServerService` provides a method to get all FTP users:

```csharp
var ftpService = app.Services.GetRequiredService<FTPServerService>();
var ftpUsers = ftpService.GetAllFTPUsers();

foreach (var user in ftpUsers)
{
    Console.WriteLine($"Username: {user.Username}");
    Console.WriteLine($"Home: {user.HomeDirectory}");
    Console.WriteLine($"Enabled: {user.Enabled}");
}
```

## API Integration

### Services

#### WebMarketService
```csharp
// Get all packages
var packages = webMarketService.GetPackages();

// Get specific package
var package = webMarketService.GetPackageById(packageId);

// Create customer account
var result = webMarketService.CreateAccount(
    userId, username, email, siteName, packageId, ftpPassword
);

// Get customer account
var account = webMarketService.GetAccountByUserId(userId);

// Get all accounts (admin)
var accounts = webMarketService.GetAllAccounts();
```

#### FTPServerService
```csharp
// Get FTP connection info for user
var ftpInfo = ftpService.GetFTPConnectionInfo(userId);

// Get all FTP users for server configuration
var ftpUsers = ftpService.GetAllFTPUsers();
```

## Pages

### Customer Pages

- **`/WebMarket`**: Package selection and marketplace home
- **`/WebMarket/Register?packageId={id}`**: Hosting registration form
- **`/WebMarket/Dashboard`**: Customer hosting dashboard

### Admin Pages

- **`/Admin/WebMarket`**: Web Market management and statistics

## Security Considerations

1. **Separate FTP Passwords**: FTP passwords are separate from CMS passwords for enhanced security
2. **Password Hashing**: FTP passwords are hashed using SHA256 before storage
3. **User Isolation**: Each customer has their own isolated directory
4. **Active Account Checks**: Only active accounts with FTP enabled can access FTP
5. **Input Validation**: Site names are sanitized to create safe URL slugs

## Migration from Python Reseller

If you're migrating from the Python reseller (`reseller.py`):

1. **Data Migration**: The database schemas are compatible
2. **Directory Structure**: Same structure is maintained (`reseller_sites/`)
3. **FTP Access**: FTP credentials work the same way
4. **Enhanced Features**: 
   - Web-based interface instead of CLI
   - Integrated with CMS authentication
   - Professional dashboard for customers
   - Admin management interface

## Troubleshooting

### Issue: User website not accessible

**Solution**: Ensure the static file middleware is configured correctly in `Program.cs`:
```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "user_websites")),
    RequestPath = "/user_websites"
});
```

### Issue: FTP connection fails

**Solutions**:
1. Verify FTP server is running
2. Check FTP server is configured to read from `WebMarketAccounts` table
3. Ensure account status is 'active' and `FtpEnabled` is true
4. Verify firewall allows port 21 (or configured FTP port)

### Issue: Directory permissions

**Solution**: Ensure web server has write permissions to:
- `reseller_sites/` directory
- `wwwroot/user_websites/` directory

```bash
chmod -R 755 reseller_sites/
chmod -R 755 wwwroot/user_websites/
```

## Development

### Adding New Packages

1. Insert into database:
```sql
INSERT INTO WebMarketPackages (Name, Description, Price, Features, CreatedAt, UpdatedAt)
VALUES ('Custom Package', 'Description here', 29.99, 'feature1,feature2', datetime('now'), datetime('now'));
```

2. Packages will automatically appear on `/WebMarket` page

### Customizing Default Site Files

Edit the `GenerateDefaultIndexHtml()` and `GenerateReadme()` methods in `WebMarketService.cs` to customize the default files created for new sites.

## Future Enhancements

Potential improvements for future versions:

1. **Built-in FTP Server**: Integrate a C# FTP library like FluentFTP.Server
2. **Billing Integration**: Add payment processing and subscription management
3. **Resource Limits**: Add disk space and bandwidth quotas per package
4. **Site Analytics**: Track visits and usage for each customer site
5. **Template Gallery**: Provide starter templates customers can choose from
6. **File Manager**: Web-based file manager as alternative to FTP
7. **Domain Mapping**: Allow customers to map custom domains
8. **SSL Certificates**: Automatic SSL for customer sites

## Support

For issues or questions:
- Check the Web Market admin panel at `/Admin/WebMarket`
- Review customer account status and FTP settings
- Check logs in `reseller_sites/{site-slug}/logs/`
- Contact system administrator

---

**AGP CMS Web Market** - Professional Web Hosting Reseller Platform
