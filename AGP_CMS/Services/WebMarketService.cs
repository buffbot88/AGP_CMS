using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AGP_CMS.Services
{
    public class WebMarketService
    {
        private readonly string _connectionString;
        private readonly string _sitesRootPath;
        private readonly string _webRootPath;

        public WebMarketService(IConfiguration configuration)
        {
            _connectionString = configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
            _sitesRootPath = configuration["WebMarket:SitesRootPath"] ?? "reseller_sites";
            _webRootPath = configuration["WebMarket:WebRootPath"] ?? "wwwroot/user_websites";
            
            // Ensure directories exist
            Directory.CreateDirectory(_sitesRootPath);
            Directory.CreateDirectory(_webRootPath);
        }

        public void InitializeWebMarketTables()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS WebMarketPackages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Description TEXT,
                    Price REAL NOT NULL,
                    Features TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS WebMarketAccounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    Username TEXT UNIQUE NOT NULL,
                    Email TEXT NOT NULL,
                    SiteName TEXT NOT NULL,
                    SiteSlug TEXT UNIQUE NOT NULL,
                    PackageId INTEGER NOT NULL,
                    SitePath TEXT NOT NULL,
                    WebPath TEXT NOT NULL,
                    FtpEnabled INTEGER NOT NULL DEFAULT 1,
                    FtpPassword TEXT NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'active',
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id),
                    FOREIGN KEY (PackageId) REFERENCES WebMarketPackages(Id)
                );

                CREATE TABLE IF NOT EXISTS WebMarketFeatures (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AccountId INTEGER NOT NULL,
                    FeatureName TEXT NOT NULL,
                    Enabled INTEGER NOT NULL DEFAULT 1,
                    FOREIGN KEY (AccountId) REFERENCES WebMarketAccounts(Id)
                );

                -- Insert default packages if not exist
                INSERT OR IGNORE INTO WebMarketPackages (Name, Description, Price, Features, CreatedAt, UpdatedAt)
                VALUES 
                    ('Forum', 'Discussion forums for your community', 9.99, 'forum', datetime('now'), datetime('now')),
                    ('Blog', 'Blogging platform for content creators', 14.99, 'blog', datetime('now'), datetime('now')),
                    ('Website', 'Static website hosting with FTP access', 19.99, 'website', datetime('now'), datetime('now')),
                    ('Full Suite', 'Complete platform with all features', 39.99, 'forum,blog,website,downloads', datetime('now'), datetime('now'));
            ";

            command.ExecuteNonQuery();
        }

        public List<WebMarketPackage> GetPackages()
        {
            var packages = new List<WebMarketPackage>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Description, Price, Features, IsActive
                FROM WebMarketPackages
                WHERE IsActive = 1
                ORDER BY Price ASC
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                packages.Add(new WebMarketPackage
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Price = reader.GetDouble(3),
                    Features = reader.GetString(4).Split(',', StringSplitOptions.RemoveEmptyEntries),
                    IsActive = reader.GetInt32(5) == 1
                });
            }

            return packages;
        }

        public WebMarketPackage? GetPackageById(int packageId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Description, Price, Features, IsActive
                FROM WebMarketPackages
                WHERE Id = @packageId
            ";
            command.Parameters.AddWithValue("@packageId", packageId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new WebMarketPackage
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Price = reader.GetDouble(3),
                    Features = reader.GetString(4).Split(',', StringSplitOptions.RemoveEmptyEntries),
                    IsActive = reader.GetInt32(5) == 1
                };
            }

            return null;
        }

        public (bool success, string message, int? accountId) CreateAccount(int userId, string username, string email, string siteName, int packageId, string ftpPassword)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Get package info
            var package = GetPackageById(packageId);
            if (package == null)
            {
                return (false, "Invalid package selected", null);
            }

            // Create site slug
            var siteSlug = GenerateSlug(siteName);

            // Check if slug already exists
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM WebMarketAccounts WHERE SiteSlug = @slug";
            checkCommand.Parameters.AddWithValue("@slug", siteSlug);
            var count = Convert.ToInt32(checkCommand.ExecuteScalar());
            if (count > 0)
            {
                return (false, "A site with this name already exists", null);
            }

            // Create paths
            var sitePath = Path.Combine(_sitesRootPath, siteSlug);
            var webPath = Path.Combine(_webRootPath, siteSlug);

            if (Directory.Exists(sitePath) || Directory.Exists(webPath))
            {
                return (false, "Site directory already exists", null);
            }

            // Create directory structure
            try
            {
                Directory.CreateDirectory(sitePath);
                Directory.CreateDirectory(Path.Combine(sitePath, "wwwroot"));
                Directory.CreateDirectory(Path.Combine(sitePath, "data"));
                Directory.CreateDirectory(Path.Combine(sitePath, "uploads"));
                Directory.CreateDirectory(Path.Combine(sitePath, "logs"));

                // Create web path as symlink or copy
                Directory.CreateDirectory(webPath);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to create directories: {ex.Message}", null);
            }

            // Hash FTP password
            var hashedFtpPassword = HashPassword(ftpPassword);

            // Insert account
            var now = DateTime.UtcNow.ToString("o");
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO WebMarketAccounts 
                (UserId, Username, Email, SiteName, SiteSlug, PackageId, SitePath, WebPath, FtpPassword, CreatedAt, UpdatedAt)
                VALUES (@userId, @username, @email, @siteName, @siteSlug, @packageId, @sitePath, @webPath, @ftpPassword, @now, @now)
            ";
            insertCommand.Parameters.AddWithValue("@userId", userId);
            insertCommand.Parameters.AddWithValue("@username", username);
            insertCommand.Parameters.AddWithValue("@email", email);
            insertCommand.Parameters.AddWithValue("@siteName", siteName);
            insertCommand.Parameters.AddWithValue("@siteSlug", siteSlug);
            insertCommand.Parameters.AddWithValue("@packageId", packageId);
            insertCommand.Parameters.AddWithValue("@sitePath", sitePath);
            insertCommand.Parameters.AddWithValue("@webPath", webPath);
            insertCommand.Parameters.AddWithValue("@ftpPassword", hashedFtpPassword);
            insertCommand.Parameters.AddWithValue("@now", now);

            insertCommand.ExecuteNonQuery();

            // Get the last inserted ID
            var selectIdCommand = connection.CreateCommand();
            selectIdCommand.CommandText = "SELECT last_insert_rowid()";
            var accountId = Convert.ToInt32(selectIdCommand.ExecuteScalar());

            // Add features
            foreach (var feature in package.Features)
            {
                var featureCommand = connection.CreateCommand();
                featureCommand.CommandText = @"
                    INSERT INTO WebMarketFeatures (AccountId, FeatureName, Enabled)
                    VALUES (@accountId, @feature, 1)
                ";
                featureCommand.Parameters.AddWithValue("@accountId", accountId);
                featureCommand.Parameters.AddWithValue("@feature", feature);
                featureCommand.ExecuteNonQuery();
            }

            // Create default files
            CreateDefaultFiles(sitePath, webPath, siteName, package.Features);

            return (true, "Account created successfully", accountId);
        }

        public WebMarketAccount? GetAccountByUserId(int userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, UserId, Username, Email, SiteName, SiteSlug, PackageId, 
                       SitePath, WebPath, FtpEnabled, Status, CreatedAt, UpdatedAt
                FROM WebMarketAccounts
                WHERE UserId = @userId
            ";
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new WebMarketAccount
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Username = reader.GetString(2),
                    Email = reader.GetString(3),
                    SiteName = reader.GetString(4),
                    SiteSlug = reader.GetString(5),
                    PackageId = reader.GetInt32(6),
                    SitePath = reader.GetString(7),
                    WebPath = reader.GetString(8),
                    FtpEnabled = reader.GetInt32(9) == 1,
                    Status = reader.GetString(10),
                    CreatedAt = DateTime.Parse(reader.GetString(11)),
                    UpdatedAt = DateTime.Parse(reader.GetString(12))
                };
            }

            return null;
        }

        public List<WebMarketAccount> GetAllAccounts()
        {
            var accounts = new List<WebMarketAccount>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, UserId, Username, Email, SiteName, SiteSlug, PackageId, 
                       SitePath, WebPath, FtpEnabled, Status, CreatedAt, UpdatedAt
                FROM WebMarketAccounts
                ORDER BY CreatedAt DESC
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                accounts.Add(new WebMarketAccount
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Username = reader.GetString(2),
                    Email = reader.GetString(3),
                    SiteName = reader.GetString(4),
                    SiteSlug = reader.GetString(5),
                    PackageId = reader.GetInt32(6),
                    SitePath = reader.GetString(7),
                    WebPath = reader.GetString(8),
                    FtpEnabled = reader.GetInt32(9) == 1,
                    Status = reader.GetString(10),
                    CreatedAt = DateTime.Parse(reader.GetString(11)),
                    UpdatedAt = DateTime.Parse(reader.GetString(12))
                });
            }

            return accounts;
        }

        private string GenerateSlug(string text)
        {
            var slug = text.ToLowerInvariant();
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
            slug = slug.Trim('-');
            return slug;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private void CreateDefaultFiles(string sitePath, string webPath, string siteName, string[] features)
        {
            // Create index.html in both locations
            var indexHtml = GenerateDefaultIndexHtml(siteName, features);
            File.WriteAllText(Path.Combine(sitePath, "wwwroot", "index.html"), indexHtml);
            File.WriteAllText(Path.Combine(webPath, "index.html"), indexHtml);

            // Create README
            var readme = GenerateReadme(siteName, features);
            File.WriteAllText(Path.Combine(sitePath, "README.md"), readme);
        }

        private string GenerateDefaultIndexHtml(string siteName, string[] features)
        {
            var featuresList = string.Join("\n", features.Select(f => 
                $"            <div class=\"feature\">✓ {GetFeatureDisplayName(f)}</div>"));

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{siteName} - Welcome</title>
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
    <div class=""container"">
        <h1>Welcome to {siteName}!</h1>
        <p>Your site has been successfully provisioned with AGP CMS Web Market.</p>
        
        <div class=""features"">
            <h3>Your Package Includes:</h3>
{featuresList}
        </div>
        
        <p>You can upload your files via FTP to customize this website.</p>
        
        <div class=""footer"">
            <p>Powered by AGP CMS Web Market</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateReadme(string siteName, string[] features)
        {
            return $@"# {siteName}

Welcome to your AGP CMS Web Market site!

## Site Information
- Site Name: {siteName}
- Features: {string.Join(", ", features)}
- Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

## Directory Structure
- `wwwroot/` - Your website files (HTML, CSS, JS, images)
- `data/` - Database and data files
- `uploads/` - User uploaded files
- `logs/` - System logs

## FTP Access
You can access your files via FTP using your username and password.
Your files will be accessible at: http://agpstudios.org/{siteName.ToLowerInvariant().Replace(" ", "-")}

## Getting Started
1. Upload your website files to the `wwwroot` directory via FTP
2. Customize the default index.html file
3. Add your content and media files

For support, please contact your site administrator.
";
        }

        private string GetFeatureDisplayName(string feature)
        {
            return feature switch
            {
                "forum" => "Discussion Forums",
                "blog" => "Blog System",
                "website" => "Website Hosting",
                "downloads" => "File Downloads",
                _ => feature
            };
        }
    }

    public class WebMarketPackage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Price { get; set; }
        public string[] Features { get; set; } = Array.Empty<string>();
        public bool IsActive { get; set; }
    }

    public class WebMarketAccount
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string SiteSlug { get; set; } = string.Empty;
        public int PackageId { get; set; }
        public string SitePath { get; set; } = string.Empty;
        public string WebPath { get; set; } = string.Empty;
        public bool FtpEnabled { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
