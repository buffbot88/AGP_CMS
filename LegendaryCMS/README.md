# AGP CMS v1.0.0

**Standalone Content Management System with Kestrel and SQLite**

## Overview

AGP CMS (ASHAT OS CMS) is a standalone, self-contained Content Management System built on ASP.NET Core 9.0 with Kestrel web server and SQLite database. It provides a complete CMS solution with blogs, forums, learning modules, user profiles, and downloads - all in a single executable.

## ✨ Features

### 🚀 **Standalone System**
- Self-contained executable application
- Kestrel web server (no IIS or external web server needed)
- Configurable port and URL via JSON configuration
- SQLite database for all data storage
- Zero external dependencies

### 🔐 **Authentication System**
- User registration and login
- Session-based authentication with secure cookies
- Password hashing with SHA256
- First-run admin account setup
- Role-based access control (User, Admin)
- 24-hour session expiry

### 📝 **Blog Management**
- Create, edit, and view blog posts
- Rich text content with XSS protection
- Published/draft status
- View count tracking
- Automatic excerpt generation
- Author attribution
- Authentication required for posting

### 💬 **Forum System**
- Discussion categories and threads
- Thread viewing and posting
- Forum moderation tools
- Admin controls for categories

### 👤 **User Profiles & Control Panel**
- Extensive user profile system
- User control panel at `/control-panel`
- Edit email, title, and bio
- Profile customization
- Social features foundation (friends system)

### 📦 **Complete CMS Functionality**
- **Blogs**: Create and manage blog posts
- **Forums**: Discussion forums with categories and threads
- **Learning**: Course and lesson management
- **Profiles**: User profile system with social features
- **Downloads**: File management and downloads
- **Admin Panel**: Complete administrative control

### ⚙️ **Configuration**
- JSON-based configuration (appsettings.json)
- Configurable web server port
- Site URL configuration
- Database settings
- Security options

### 🔒 **Security Features**
- XSS protection with content sanitization
- Session management with database tracking
- Secure password hashing
- Role-based authorization
- SQL injection prevention (parameterized queries)

### 🗄️ **SQLite Database**
- Automatic database initialization
- Schema auto-creation on first run
- Comprehensive tables:
  - Users with extended profile fields
  - UserProfiles for additional info
  - Sessions for authentication
  - BlogPosts with comments
  - ForumCategories, ForumThreads, ForumPosts
  - Friends for social features
  - ActivityLog for tracking
  - Downloads
  - Settings

## 📋 Quick Start

### Prerequisites
- .NET 9.0 SDK or Runtime

### Running AGP CMS

1. **Build the application:**
   ```bash
   dotnet build
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Access the application:**
   - Homepage: http://localhost:5000/
   - Blogs: http://localhost:5000/cms/blogs
   - Forums: http://localhost:5000/cms/forums
   - Admin Panel: http://localhost:5000/cms/admin
   - Control Panel: http://localhost:5000/control-panel
   - Health Check: http://localhost:5000/api/health

### First Run Setup

1. Navigate to the homepage
2. Click "Register" or visit http://localhost:5000/register
3. Create your admin account (first account is automatically admin)
4. Login and start using the CMS!

## 🎯 Key Routes

### Public Pages
- `/` - Homepage with feature showcase
- `/login` - User login
- `/register` - User registration
- `/cms/blogs` - Blog listing
- `/cms/blogs/post/{id}` - Individual blog post
- `/cms/forums` - Forum index
- `/cms/profiles/{username}` - User profile
- `/cms/learning` - Learning modules
- `/cms/downloads` - Downloads

### Authenticated Pages
- `/control-panel` - User control panel
- `/cms/blogs/create` - Create/edit blog post

### Admin Pages
- `/cms/admin` - Admin dashboard
- `/cms/admin/users` - User management
- `/cms/admin/forums` - Forum management
- `/cms/admin/categories` - Category management
- `/cms/admin/settings` - System settings

### API Endpoints
- `GET /api/health` - Health check
- `POST /api/auth/logout` - Logout
- `GET /api/auth/status` - Authentication status

## ⚙️ Configuration

Edit `appsettings.json` to customize your installation:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  },
  "AGP_CMS": {
    "SiteName": "AGP CMS",
    "SiteUrl": "http://localhost:5000",
    "Database": {
      "Type": "SQLite",
      "ConnectionString": "Data Source=agp_cms.db",
      "AutoMigrate": true
    }
  }
}
```

#### Configuration Options

**Kestrel Settings:**
- `Kestrel:Endpoints:Http:Url` - Web server URL and port (default: http://localhost:5000)

**Site Settings:**
- `AGP_CMS:SiteName` - Your site name
- `AGP_CMS:SiteUrl` - Public URL for your site
- `AGP_CMS:Database:ConnectionString` - SQLite database file path

**Security Settings:**
- `AGP_CMS:Security:SessionTimeout` - Session timeout in seconds (default: 3600)
- `AGP_CMS:Security:EnableCSRF` - Enable CSRF protection (default: true)
- `AGP_CMS:Security:EnableXSSProtection` - Enable XSS protection (default: true)
- `AGP_CMS:Security:MaxLoginAttempts` - Maximum login attempts (default: 5)

**API Settings:**
- `AGP_CMS:API:Enabled` - Enable REST API (default: true)
- `AGP_CMS:API:RateLimit:RequestsPerMinute` - Rate limit per minute (default: 60)
- `AGP_CMS:API:RateLimit:RequestsPerHour` - Rate limit per hour (default: 1000)

**Feature Toggles:**
- `AGP_CMS:Features:Blogs` - Enable/disable blogs (default: true)
- `AGP_CMS:Features:Forums` - Enable/disable forums (default: true)
- `AGP_CMS:Features:Learning` - Enable/disable learning module (default: true)
- `AGP_CMS:Features:Profiles` - Enable/disable profiles (default: true)
- `AGP_CMS:Features:Downloads` - Enable/disable downloads (default: true)
- `AGP_CMS:Features:AdminPanel` - Enable/disable admin panel (default: true)

## Database

AGP CMS uses SQLite for data storage. The database is automatically created on first run with the following schema:

### Tables
- **Users** - User accounts and authentication
- **BlogPosts** - Blog content
- **ForumCategories** - Forum categories
- **ForumThreads** - Forum discussion threads
- **ForumPosts** - Individual forum posts
- **Downloads** - File downloads and metadata
- **Settings** - System configuration

The database file (`agp_cms.db` by default) is created in the application directory.

## API Endpoints

### Health Check
```bash
curl http://localhost:5000/api/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-08T13:53:00Z",
  "system": "AGP_CMS",
  "version": "1.0.0"
}
```

### CMS Pages
- `/` - Redirects to /cms/blogs
- `/cms/blogs` - Blog listing and posts
- `/cms/forums` - Forum categories and discussions
- `/cms/learning` - Learning modules and courses
- `/cms/profiles` - User profiles
- `/cms/downloads` - File downloads

## Deployment

### Production Deployment

1. **Publish the application:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Copy the publish folder to your server**

3. **Update appsettings.json for production:**
   ```json
   {
     "Kestrel": {
       "Endpoints": {
         "Http": {
           "Url": "http://0.0.0.0:8080"
         }
       }
     },
     "AGP_CMS": {
       "SiteUrl": "http://your-domain.com"
     }
   }
   ```

4. **Run as a service:**
   ```bash
   ./AGP_CMS
   ```

### Running as a Windows Service

Use `sc.exe` or NSSM to run AGP CMS as a Windows service.

### Running as a Linux Service (systemd)

Create `/etc/systemd/system/agp-cms.service`:
```ini
[Unit]
Description=AGP CMS
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/agp-cms
ExecStart=/opt/agp-cms/AGP_CMS
Restart=always
RestartSec=10
User=www-data

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable agp-cms
sudo systemctl start agp-cms
```

## Development

### Project Structure
```
AGP_CMS/
├── API/                    # REST API layer
├── Components/             # Razor Components
├── Configuration/          # Configuration management
├── Core/                   # Core module interfaces
├── Pages/                  # Razor Pages
│   ├── Blogs/
│   ├── Forums/
│   ├── Learning/
│   └── Profiles/
├── Plugins/                # Plugin system
├── Security/              # RBAC and security
├── wwwroot/               # Static files
├── Program.cs             # Application entry point
├── appsettings.json       # Configuration
└── LegendaryCMS.csproj    # Project file
```

### Building from Source
```bash
git clone <repository-url>
cd LegendaryCMS
dotnet restore
dotnet build
dotnet run
```

## Troubleshooting

### Port Already in Use
If port 5000 is already in use, change it in `appsettings.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:8080"
      }
    }
  }
}
```

### Database Locked
If you get "database is locked" errors, ensure no other process is accessing the database file.

### Permission Denied
Ensure the application has write permissions to create the database file in its directory.

## License

See the LICENSE file for licensing information.

## Support

For issues and questions:
- Open an issue on GitHub
- Check the API documentation at `/api/health`

---

**AGP CMS v1.0.0** - Standalone CMS with Kestrel and SQLite
