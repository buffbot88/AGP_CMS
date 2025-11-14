# AGP CMS

**Advanced General Purpose Content Management System**

AGP CMS is a comprehensive, standalone Content Management System built on ASP.NET Core 9.0 with SQLite database, featuring blogs, forums, learning modules, and more.

## 🎯 Quick Links

- **[LegendaryCMS Documentation](LegendaryCMS/README.md)** - Main CMS documentation
- **[Reseller System](RESELLER_README.md)** - Website provisioning and reseller features

## ✨ Main Features

### 🚀 Core CMS (LegendaryCMS)
- **Standalone System** - Self-contained with Kestrel web server
- **Blog Management** - Create and manage blog posts
- **Forum System** - Discussion forums with categories
- **Learning Module** - Course and lesson management
- **User Profiles** - Comprehensive user profile system
- **Downloads** - File management and downloads
- **Admin Panel** - Complete administrative control

### 💼 Reseller System (NEW!)
- **Website Provisioning** - Automatically create isolated websites for customers
- **Multiple Packages** - Forum, Blog, Website, or Full Suite options
- **FTP Server** - Built-in FTP server for file access (port 21)
- **User Management** - Secure account creation and authentication
- **Automated Setup** - Creates directories and default files automatically

## 🚀 Getting Started

### Running the Main CMS

```bash
cd LegendaryCMS
dotnet build
dotnet run
```

Access at: http://localhost:5000

### Running the Reseller System

```bash
# Install dependencies
pip install -r requirements-reseller.txt

# Interactive mode
python3 reseller.py

# Or use quick start
./reseller-quickstart.sh   # Linux/Mac
reseller-quickstart.bat     # Windows
```

See [RESELLER_README.md](RESELLER_README.md) for detailed reseller documentation.

## 📦 Repository Structure

```
AGP_CMS/
├── LegendaryCMS/          # Main CMS application (ASP.NET Core)
│   ├── API/               # REST API layer
│   ├── Pages/             # Razor Pages
│   ├── Services/          # Business logic
│   └── README.md          # CMS documentation
├── LegendaryChat/         # Chat module
├── LegendaryLearning/     # Learning module
├── Abstractions/          # Shared abstractions
├── reseller.py            # Reseller system script
├── RESELLER_README.md     # Reseller documentation
└── requirements-reseller.txt  # Python dependencies
```

## 💡 Use Cases

### For Website Owners
Use LegendaryCMS to run your own blog, forum, or learning platform with complete control.

### For Resellers
Use the Reseller System to:
- Sell website hosting packages to customers
- Automatically provision isolated sites
- Provide FTP access for file management
- Manage multiple customer accounts
- Offer tiered packages (Forum, Blog, Website, Full Suite)

## 🔧 Requirements

### LegendaryCMS
- .NET 9.0 SDK or Runtime
- SQLite (included)

### Reseller System
- Python 3.7+
- pip
- pyftpdlib

## 📚 Documentation

- **[LegendaryCMS README](LegendaryCMS/README.md)** - Comprehensive CMS documentation
- **[Reseller README](RESELLER_README.md)** - Reseller system guide
- **[Testing Guide](LegendaryCMS/TESTING.md)** - Testing documentation

## 🎯 Quick Examples

### Create a Reseller Account

```python
from reseller import ResellerManager

manager = ResellerManager()
account_id = manager.create_account(
    username="customer1",
    password="secure123",
    email="customer@example.com",
    site_name="Customer Website",
    package_type="4"  # Full Suite
)
```

### Start FTP Server

```bash
python3 reseller.py ftp
```

### Connect via FTP

```bash
ftp your-server-ip
# Username: customer1
# Password: secure123
```

## 🔐 Security

- **Password Hashing** - SHA256 for all stored passwords
- **Session Management** - Secure cookie-based sessions
- **XSS Protection** - Content sanitization
- **SQL Injection Prevention** - Parameterized queries
- **FTP Isolation** - Users restricted to their home directories

## 📄 License

See the [LICENSE](LICENSE) file for details.

## 🤝 Support

For issues and questions:
- Check the documentation
- Open an issue on GitHub

---

**AGP CMS** - Complete Content Management & Reseller Solution
