using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace AGP_CMS.Services
{
    public partial class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
        }

        public SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public int? GetAuthenticatedUserId(HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.TryGetValue("AGPCMS_SESSION", out var sessionId))
                return null;

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT UserId 
            FROM Sessions 
            WHERE Id = @sessionId AND datetime(ExpiresAt) > datetime('now')
        ";
            command.Parameters.AddWithValue("@sessionId", sessionId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return reader.GetInt32(0);
            }

            return null;
        }

        public UserInfo? GetUserById(int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Id, Username, Email, Role, Title, Bio, AvatarUrl, CreatedAt
            FROM Users 
            WHERE Id = @userId AND IsActive = 1
        ";
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new UserInfo
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3),
                    Title = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Bio = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AvatarUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7))
                };
            }

            return null;
        }

        public UserInfo? GetUserForAdmin(int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Id, Username, Email, Role, Title, Bio, AvatarUrl, CreatedAt, IsActive
            FROM Users 
            WHERE Id = @userId
        ";
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new UserInfo
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3),
                    Title = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Bio = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AvatarUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    IsActive = reader.GetInt32(8) == 1
                };
            }

            return null;
        }

        public bool IsUserAdmin(int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Role FROM Users WHERE Id = @userId";
            command.Parameters.AddWithValue("@userId", userId);

            var role = command.ExecuteScalar() as string;
            return role == "Admin" || role == "SuperAdmin";
        }

        public bool HasRole(int userId, string role)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Role FROM Users WHERE Id = @userId";
            command.Parameters.AddWithValue("@userId", userId);

            var userRole = command.ExecuteScalar() as string;
            return userRole == role;
        }

        public bool HasAnyRole(int userId, params string[] roles)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Role FROM Users WHERE Id = @userId";
            command.Parameters.AddWithValue("@userId", userId);

            var userRole = command.ExecuteScalar() as string;
            return roles.Contains(userRole);
        }

        // Forum-related methods
        public void InitializeForumTables()
        {
            using var connection = GetConnection();

            // Create ForumCategories table
            var createCategoriesTable = connection.CreateCommand();
            createCategoriesTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS ForumCategories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                Icon TEXT DEFAULT '💬',
                DisplayOrder INTEGER DEFAULT 0,
                IsPrivate INTEGER DEFAULT 0,
                CreatedAt TEXT DEFAULT (datetime('now')),
                UpdatedAt TEXT DEFAULT (datetime('now'))
            )";
            createCategoriesTable.ExecuteNonQuery();

            // Create Forums table
            var createForumsTable = connection.CreateCommand();
            createForumsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS Forums (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT,
                Icon TEXT DEFAULT '💬',
                DisplayOrder INTEGER DEFAULT 0,
                CreatedAt TEXT DEFAULT (datetime('now')),
                UpdatedAt TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (CategoryId) REFERENCES ForumCategories(Id) ON DELETE CASCADE
            )";
            createForumsTable.ExecuteNonQuery();

            // Create ForumThreads table
            var createThreadsTable = connection.CreateCommand();
            createThreadsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS ForumThreads (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ForumId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                UserId INTEGER NOT NULL,
                ViewCount INTEGER DEFAULT 0,
                IsLocked INTEGER DEFAULT 0,
                IsSticky INTEGER DEFAULT 0,
                CreatedAt TEXT DEFAULT (datetime('now')),
                UpdatedAt TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (ForumId) REFERENCES Forums(Id) ON DELETE CASCADE,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )";
            createThreadsTable.ExecuteNonQuery();

            // Create ForumPosts table
            var createPostsTable = connection.CreateCommand();
            createPostsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS ForumPosts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ThreadId INTEGER NOT NULL,
                UserId INTEGER NOT NULL,
                Content TEXT NOT NULL,
                IsFirstPost INTEGER DEFAULT 0,
                CreatedAt TEXT DEFAULT (datetime('now')),
                UpdatedAt TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (ThreadId) REFERENCES ForumThreads(Id) ON DELETE CASCADE,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )";
            createPostsTable.ExecuteNonQuery();

            // Create indexes
            var createIndexes = connection.CreateCommand();
            createIndexes.CommandText = @"
            CREATE INDEX IF NOT EXISTS idx_forums_category ON Forums(CategoryId);
            CREATE INDEX IF NOT EXISTS idx_threads_forum ON ForumThreads(ForumId);
            CREATE INDEX IF NOT EXISTS idx_posts_thread ON ForumPosts(ThreadId);
        ";
            createIndexes.ExecuteNonQuery();

            // Create ForumModerators table
            var createModeratorsTable = connection.CreateCommand();
            createModeratorsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS ForumModerators (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                AssignedAt TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (CategoryId) REFERENCES ForumCategories(Id) ON DELETE CASCADE,
                UNIQUE(UserId, CategoryId)
            )";
            createModeratorsTable.ExecuteNonQuery();

            // Create FlaggedPosts table
            var createFlaggedPostsTable = connection.CreateCommand();
            createFlaggedPostsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS FlaggedPosts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PostId INTEGER NOT NULL,
                ThreadId INTEGER NOT NULL,
                FlaggedByUserId INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                Status TEXT DEFAULT 'pending',
                CreatedAt TEXT DEFAULT (datetime('now')),
                ResolvedAt TEXT,
                ResolvedByUserId INTEGER,
                FOREIGN KEY (PostId) REFERENCES ForumPosts(Id) ON DELETE CASCADE,
                FOREIGN KEY (ThreadId) REFERENCES ForumThreads(Id) ON DELETE CASCADE,
                FOREIGN KEY (FlaggedByUserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (ResolvedByUserId) REFERENCES Users(Id) ON DELETE SET NULL
            )";
            createFlaggedPostsTable.ExecuteNonQuery();

            // Create DeletedPosts table
            var createDeletedPostsTable = connection.CreateCommand();
            createDeletedPostsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS DeletedPosts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PostId INTEGER NOT NULL,
                ThreadId INTEGER NOT NULL,
                Content TEXT NOT NULL,
                AuthorUserId INTEGER NOT NULL,
                DeletedByUserId INTEGER NOT NULL,
                DeleteReason TEXT,
                DeletedAt TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (ThreadId) REFERENCES ForumThreads(Id) ON DELETE CASCADE,
                FOREIGN KEY (AuthorUserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (DeletedByUserId) REFERENCES Users(Id) ON DELETE CASCADE
            )";
            createDeletedPostsTable.ExecuteNonQuery();

            // Create BannedUsers table
            var createBannedUsersTable = connection.CreateCommand();
            createBannedUsersTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS BannedUsers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                BannedByUserId INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                BannedAt TEXT DEFAULT (datetime('now')),
                ExpiresAt TEXT,
                IsActive INTEGER DEFAULT 1,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (BannedByUserId) REFERENCES Users(Id) ON DELETE CASCADE
            )";
            createBannedUsersTable.ExecuteNonQuery();
        }

        public int CreateForumCategory(string name, string description, string icon, bool isPrivate)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO ForumCategories (Name, Description, Icon, IsPrivate)
            VALUES (@name, @description, @icon, @isPrivate);
            SELECT last_insert_rowid();
        ";
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@description", description ?? string.Empty);
            command.Parameters.AddWithValue("@icon", icon ?? "💬");
            command.Parameters.AddWithValue("@isPrivate", isPrivate ? 1 : 0);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public int CreateForum(int categoryId, string name, string description, string icon)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Forums (CategoryId, Name, Description, Icon)
            VALUES (@categoryId, @name, @description, @icon);
            SELECT last_insert_rowid();
        ";
            command.Parameters.AddWithValue("@categoryId", categoryId);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@description", description ?? string.Empty);
            command.Parameters.AddWithValue("@icon", icon ?? "💬");

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public int CreateThread(int forumId, int userId, string title, string content)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Create thread
                var threadCommand = connection.CreateCommand();
                threadCommand.CommandText = @"
                INSERT INTO ForumThreads (ForumId, UserId, Title)
                VALUES (@forumId, @userId, @title);
                SELECT last_insert_rowid();
            ";
                threadCommand.Parameters.AddWithValue("@forumId", forumId);
                threadCommand.Parameters.AddWithValue("@userId", userId);
                threadCommand.Parameters.AddWithValue("@title", title);

                var threadId = Convert.ToInt32(threadCommand.ExecuteScalar());

                // Create first post
                var postCommand = connection.CreateCommand();
                postCommand.CommandText = @"
                INSERT INTO ForumPosts (ThreadId, UserId, Content, IsFirstPost)
                VALUES (@threadId, @userId, @content, 1);
            ";
                postCommand.Parameters.AddWithValue("@threadId", threadId);
                postCommand.Parameters.AddWithValue("@userId", userId);
                postCommand.Parameters.AddWithValue("@content", content);
                postCommand.ExecuteNonQuery();

                transaction.Commit();
                return threadId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public int CreatePost(int threadId, int userId, string content)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO ForumPosts (ThreadId, UserId, Content)
            VALUES (@threadId, @userId, @content);
            SELECT last_insert_rowid();
        ";
            command.Parameters.AddWithValue("@threadId", threadId);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@content", content);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public List<ForumCategoryWithStats> GetForumCategories()
        {
            var categories = new List<ForumCategoryWithStats>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                fc.Id, fc.Name, fc.Description, fc.Icon, fc.IsPrivate,
                COUNT(DISTINCT f.Id) as ForumCount,
                COUNT(DISTINCT ft.Id) as ThreadCount,
                COUNT(DISTINCT fp.Id) as PostCount
            FROM ForumCategories fc
            LEFT JOIN Forums f ON fc.Id = f.CategoryId
            LEFT JOIN ForumThreads ft ON f.Id = ft.ForumId
            LEFT JOIN ForumPosts fp ON ft.Id = fp.ThreadId
            GROUP BY fc.Id
            ORDER BY fc.DisplayOrder, fc.Id
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new ForumCategoryWithStats
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Icon = reader.IsDBNull(3) ? "💬" : reader.GetString(3),
                    IsPrivate = reader.GetInt32(4) == 1,
                    ForumCount = reader.GetInt32(5),
                    ThreadCount = reader.GetInt32(6),
                    PostCount = reader.GetInt32(7)
                });
            }

            return categories;
        }

        public List<ForumInfo> GetForumsByCategory(int categoryId)
        {
            var forums = new List<ForumInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                f.Id, f.Name, f.Description, f.Icon,
                COUNT(DISTINCT ft.Id) as ThreadCount,
                COUNT(DISTINCT fp.Id) as PostCount
            FROM Forums f
            LEFT JOIN ForumThreads ft ON f.Id = ft.ForumId
            LEFT JOIN ForumPosts fp ON ft.Id = fp.ThreadId
            WHERE f.CategoryId = @categoryId
            GROUP BY f.Id
            ORDER BY f.DisplayOrder, f.Id
        ";
            command.Parameters.AddWithValue("@categoryId", categoryId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                forums.Add(new ForumInfo
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Icon = reader.IsDBNull(3) ? "💬" : reader.GetString(3),
                    ThreadCount = reader.GetInt32(4),
                    PostCount = reader.GetInt32(5)
                });
            }

            return forums;
        }

        public ForumCategoryWithStats? GetForumCategoryById(int categoryId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                fc.Id, fc.Name, fc.Description, fc.Icon, fc.IsPrivate,
                COUNT(DISTINCT f.Id) as ForumCount,
                COUNT(DISTINCT ft.Id) as ThreadCount,
                COUNT(DISTINCT fp.Id) as PostCount
            FROM ForumCategories fc
            LEFT JOIN Forums f ON fc.Id = f.CategoryId
            LEFT JOIN ForumThreads ft ON f.Id = ft.ForumId
            LEFT JOIN ForumPosts fp ON ft.Id = fp.ThreadId
            WHERE fc.Id = @categoryId
            GROUP BY fc.Id
        ";
            command.Parameters.AddWithValue("@categoryId", categoryId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ForumCategoryWithStats
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Icon = reader.IsDBNull(3) ? "💬" : reader.GetString(3),
                    IsPrivate = reader.GetInt32(4) == 1,
                    ForumCount = reader.GetInt32(5),
                    ThreadCount = reader.GetInt32(6),
                    PostCount = reader.GetInt32(7)
                };
            }

            return null;
        }

        public bool UpdateForumCategory(int categoryId, string name, string description, string icon, bool isPrivate)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE ForumCategories 
            SET Name = @name, Description = @description, Icon = @icon, IsPrivate = @isPrivate, UpdatedAt = datetime('now')
            WHERE Id = @categoryId
        ";
            command.Parameters.AddWithValue("@categoryId", categoryId);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@description", description ?? string.Empty);
            command.Parameters.AddWithValue("@icon", icon ?? "💬");
            command.Parameters.AddWithValue("@isPrivate", isPrivate ? 1 : 0);

            return command.ExecuteNonQuery() > 0;
        }

        public bool UpdateForum(int forumId, string name, string description, string icon)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Forums 
            SET Name = @name, Description = @description, Icon = @icon, UpdatedAt = datetime('now')
            WHERE Id = @forumId
        ";
            command.Parameters.AddWithValue("@forumId", forumId);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@description", description ?? string.Empty);
            command.Parameters.AddWithValue("@icon", icon ?? "💬");

            return command.ExecuteNonQuery() > 0;
        }

        public bool DeleteForumCategory(int categoryId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ForumCategories WHERE Id = @categoryId";
            command.Parameters.AddWithValue("@categoryId", categoryId);

            return command.ExecuteNonQuery() > 0;
        }

        public bool DeleteForum(int forumId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Forums WHERE Id = @forumId";
            command.Parameters.AddWithValue("@forumId", forumId);

            return command.ExecuteNonQuery() > 0;
        }

        // Download Category Management
        public void InitializeDownloadTables()
        {
            using var connection = GetConnection();

            var createCategoriesTable = connection.CreateCommand();
            createCategoriesTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS DownloadCategories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                Icon TEXT DEFAULT '📁',
                IsPrivate INTEGER DEFAULT 0,
                CreatedAt TEXT DEFAULT (datetime('now')),
                UpdatedAt TEXT DEFAULT (datetime('now'))
            )";
            createCategoriesTable.ExecuteNonQuery();

            var updateDownloadsTable = connection.CreateCommand();
            updateDownloadsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS DownloadsNew (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                FilePath TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                FileType TEXT,
                CategoryId INTEGER,
                UploadedBy INTEGER NOT NULL,
                UploadedAt TEXT DEFAULT (datetime('now')),
                DownloadCount INTEGER NOT NULL DEFAULT 0,
                IsPublic INTEGER DEFAULT 1,
                FOREIGN KEY (UploadedBy) REFERENCES Users(Id),
                FOREIGN KEY (CategoryId) REFERENCES DownloadCategories(Id)
            );
            
            INSERT OR IGNORE INTO DownloadsNew (Id, Name, Description, FilePath, FileSize, CategoryId, UploadedBy, UploadedAt, DownloadCount)
            SELECT Id, Name, Description, FilePath, FileSize, CategoryId, UploadedBy, UploadedAt, DownloadCount FROM Downloads;
            
            DROP TABLE IF EXISTS Downloads;
            
            ALTER TABLE DownloadsNew RENAME TO Downloads;
        ";
            updateDownloadsTable.ExecuteNonQuery();
        }

        public List<DownloadCategoryInfo> GetDownloadCategories()
        {
            var categories = new List<DownloadCategoryInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                dc.Id, dc.Name, dc.Description, dc.Icon, dc.IsPrivate,
                COUNT(d.Id) as FileCount
            FROM DownloadCategories dc
            LEFT JOIN Downloads d ON dc.Id = d.CategoryId
            GROUP BY dc.Id
            ORDER BY dc.Name
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new DownloadCategoryInfo
                {
                    CategoryId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Icon = reader.IsDBNull(3) ? "📁" : reader.GetString(3),
                    IsPrivate = reader.GetInt32(4) == 1,
                    FileCount = reader.GetInt32(5)
                });
            }

            return categories;
        }

        public List<DownloadFileInfo> GetRecentDownloads(int limit = 10)
        {
            var files = new List<DownloadFileInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                d.Id, d.Name, d.Description, d.FilePath, d.FileSize, d.FileType,
                d.CategoryId, dc.Name as CategoryName, d.UploadedBy, u.Username,
                d.UploadedAt, d.DownloadCount, d.IsPublic
            FROM Downloads d
            LEFT JOIN DownloadCategories dc ON d.CategoryId = dc.Id
            LEFT JOIN Users u ON d.UploadedBy = u.Id
            ORDER BY d.UploadedAt DESC
            LIMIT @limit
        ";
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                files.Add(new DownloadFileInfo
                {
                    FileId = reader.GetInt32(0),
                    FileName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    FilePath = reader.GetString(3),
                    FileSize = reader.GetInt64(4),
                    FileType = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    CategoryId = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    CategoryName = reader.IsDBNull(7) ? "Uncategorized" : reader.GetString(7),
                    UploadedBy = reader.GetString(9),
                    UploadedAt = DateTime.Parse(reader.GetString(10)),
                    DownloadCount = reader.GetInt32(11),
                    IsPublic = reader.GetInt32(12) == 1
                });
            }

            return files;
        }

        public List<DownloadFileInfo> GetPopularDownloads(int limit = 10)
        {
            var files = new List<DownloadFileInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                d.Id, d.Name, d.Description, d.FilePath, d.FileSize, d.FileType,
                d.CategoryId, dc.Name as CategoryName, d.UploadedBy, u.Username,
                d.UploadedAt, d.DownloadCount, d.IsPublic
            FROM Downloads d
            LEFT JOIN DownloadCategories dc ON d.CategoryId = dc.Id
            LEFT JOIN Users u ON d.UploadedBy = u.Id
            WHERE d.DownloadCount > 0
            ORDER BY d.DownloadCount DESC
            LIMIT @limit
        ";
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                files.Add(new DownloadFileInfo
                {
                    FileId = reader.GetInt32(0),
                    FileName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    FilePath = reader.GetString(3),
                    FileSize = reader.GetInt64(4),
                    FileType = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    CategoryId = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    CategoryName = reader.IsDBNull(7) ? "Uncategorized" : reader.GetString(7),
                    UploadedBy = reader.GetString(9),
                    UploadedAt = DateTime.Parse(reader.GetString(10)),
                    DownloadCount = reader.GetInt32(11),
                    IsPublic = reader.GetInt32(12) == 1
                });
            }

            return files;
        }

        // Admin Dashboard Stats
        public DashboardStats GetDashboardStats()
        {
            using var connection = GetConnection();

            var stats = new DashboardStats();

            // Get total users
            var userCountCmd = connection.CreateCommand();
            userCountCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE IsActive = 1";
            stats.TotalUsers = Convert.ToInt32(userCountCmd.ExecuteScalar() ?? 0);

            // Get total forum posts
            var forumPostCountCmd = connection.CreateCommand();
            forumPostCountCmd.CommandText = "SELECT COUNT(*) FROM ForumPosts";
            stats.TotalForumPosts = Convert.ToInt32(forumPostCountCmd.ExecuteScalar() ?? 0);

            // Get total blog posts
            var blogPostCountCmd = connection.CreateCommand();
            blogPostCountCmd.CommandText = "SELECT COUNT(*) FROM BlogPosts WHERE Published = 1";
            stats.TotalBlogPosts = Convert.ToInt32(blogPostCountCmd.ExecuteScalar() ?? 0);

            // Get active sessions
            var sessionCountCmd = connection.CreateCommand();
            sessionCountCmd.CommandText = "SELECT COUNT(*) FROM Sessions WHERE datetime(ExpiresAt) > datetime('now')";
            stats.ActiveSessions = Convert.ToInt32(sessionCountCmd.ExecuteScalar() ?? 0);

            // Get total downloads
            var downloadCountCmd = connection.CreateCommand();
            downloadCountCmd.CommandText = "SELECT COUNT(*) FROM Downloads";
            stats.TotalDownloads = Convert.ToInt32(downloadCountCmd.ExecuteScalar() ?? 0);

            return stats;
        }

        public List<RecentActivityInfo> GetRecentActivity(int limit = 20)
        {
            var activities = new List<RecentActivityInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                al.Id, al.UserId, u.Username, al.ActivityType, al.Description, al.CreatedAt
            FROM ActivityLog al
            LEFT JOIN Users u ON al.UserId = u.Id
            ORDER BY al.CreatedAt DESC
            LIMIT @limit
        ";
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                activities.Add(new RecentActivityInfo
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Username = reader.IsDBNull(2) ? "Unknown" : reader.GetString(2),
                    ActivityType = reader.GetString(3),
                    Description = reader.GetString(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5))
                });
            }

            return activities;
        }

        // User Management
        public List<UserInfo> GetAllUsers()
        {
            var users = new List<UserInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Id, Username, Email, Role, Title, Bio, AvatarUrl, CreatedAt
            FROM Users
            WHERE IsActive = 1
            ORDER BY CreatedAt DESC
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new UserInfo
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3),
                    Title = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Bio = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AvatarUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7))
                });
            }

            return users;
        }

        public bool UpdateUserRole(int userId, string role)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Users 
            SET Role = @role, UpdatedAt = datetime('now')
            WHERE Id = @userId
        ";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@role", role);

            return command.ExecuteNonQuery() > 0;
        }

        public bool UpdateUser(int userId, string username, string email, string role, bool isActive)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Users 
            SET Username = @username, 
                Email = @email, 
                Role = @role, 
                IsActive = @isActive,
                UpdatedAt = datetime('now')
            WHERE Id = @userId
        ";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@role", role);
            command.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);

            return command.ExecuteNonQuery() > 0;
        }

        // Blog Category Management
        public void InitializeBlogCategoryTables()
        {
            using var connection = GetConnection();

            var createTable = connection.CreateCommand();
            createTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS BlogCategories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Description TEXT,
                Slug TEXT NOT NULL UNIQUE,
                CreatedAt TEXT DEFAULT (datetime('now')),
                UpdatedAt TEXT DEFAULT (datetime('now'))
            )";
            createTable.ExecuteNonQuery();

            // Create FlaggedBlogComments table for blog moderation
            var createFlaggedBlogCommentsTable = connection.CreateCommand();
            createFlaggedBlogCommentsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS FlaggedBlogComments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CommentId INTEGER NOT NULL,
                PostId INTEGER NOT NULL,
                FlaggedByUserId INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                Status TEXT DEFAULT 'pending',
                CreatedAt TEXT DEFAULT (datetime('now')),
                ResolvedAt TEXT,
                ResolvedByUserId INTEGER,
                FOREIGN KEY (CommentId) REFERENCES BlogComments(Id) ON DELETE CASCADE,
                FOREIGN KEY (PostId) REFERENCES BlogPosts(Id) ON DELETE CASCADE,
                FOREIGN KEY (FlaggedByUserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (ResolvedByUserId) REFERENCES Users(Id) ON DELETE SET NULL
            )";
            createFlaggedBlogCommentsTable.ExecuteNonQuery();

            // Create DeletedBlogComments table
            var createDeletedBlogCommentsTable = connection.CreateCommand();
            createDeletedBlogCommentsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS DeletedBlogComments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CommentId INTEGER NOT NULL,
                PostId INTEGER NOT NULL,
                Content TEXT NOT NULL,
                AuthorUserId INTEGER NOT NULL,
                DeletedByUserId INTEGER NOT NULL,
                DeleteReason TEXT,
                DeletedAt TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (PostId) REFERENCES BlogPosts(Id) ON DELETE CASCADE,
                FOREIGN KEY (AuthorUserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (DeletedByUserId) REFERENCES Users(Id) ON DELETE CASCADE
            )";
            createDeletedBlogCommentsTable.ExecuteNonQuery();

            // Create FlaggedBlogPosts table
            var createFlaggedBlogPostsTable = connection.CreateCommand();
            createFlaggedBlogPostsTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS FlaggedBlogPosts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PostId INTEGER NOT NULL,
                FlaggedByUserId INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                Status TEXT DEFAULT 'pending',
                CreatedAt TEXT DEFAULT (datetime('now')),
                ResolvedAt TEXT,
                ResolvedByUserId INTEGER,
                FOREIGN KEY (PostId) REFERENCES BlogPosts(Id) ON DELETE CASCADE,
                FOREIGN KEY (FlaggedByUserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (ResolvedByUserId) REFERENCES Users(Id) ON DELETE SET NULL
            )";
            createFlaggedBlogPostsTable.ExecuteNonQuery();
        }

        public int CreateBlogCategory(string name, string description)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();

            // Generate slug from name
            var slug = name.ToLower().Replace(" ", "-").Replace("'", "");

            command.CommandText = @"
            INSERT INTO BlogCategories (Name, Description, Slug)
            VALUES (@name, @description, @slug);
            SELECT last_insert_rowid();
        ";
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@description", description ?? string.Empty);
            command.Parameters.AddWithValue("@slug", slug);

            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        public List<BlogCategoryInfo> GetBlogCategories()
        {
            var categories = new List<BlogCategoryInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                bc.Id, bc.Name, bc.Description, bc.Slug,
                COUNT(bp.Id) as PostCount
            FROM BlogCategories bc
            LEFT JOIN BlogPosts bp ON bc.Id = bp.CategoryId AND bp.Published = 1
            GROUP BY bc.Id
            ORDER BY bc.Name
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new BlogCategoryInfo
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Slug = reader.GetString(3),
                    PostCount = reader.GetInt32(4)
                });
            }

            return categories;
        }

        public List<BlogPostInfo> GetAllBlogPosts()
        {
            var posts = new List<BlogPostInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                bp.Id, bp.Title, bp.Slug, bp.AuthorId, u.Username,
                bp.CategoryId, bc.Name as CategoryName, bp.Published,
                bp.ViewCount, bp.CreatedAt, bp.UpdatedAt
            FROM BlogPosts bp
            LEFT JOIN Users u ON bp.AuthorId = u.Id
            LEFT JOIN BlogCategories bc ON bp.CategoryId = bc.Id
            ORDER BY bp.CreatedAt DESC
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                posts.Add(new BlogPostInfo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Slug = reader.GetString(2),
                    AuthorId = reader.GetInt32(3),
                    AuthorName = reader.IsDBNull(4) ? "Unknown" : reader.GetString(4),
                    CategoryId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    CategoryName = reader.IsDBNull(6) ? "Uncategorized" : reader.GetString(6),
                    Published = reader.GetInt32(7) == 1,
                    ViewCount = reader.GetInt32(8),
                    CreatedAt = DateTime.Parse(reader.GetString(9)),
                    UpdatedAt = DateTime.Parse(reader.GetString(10))
                });
            }

            return posts;
        }

        public bool UpdateBlogPostStatus(int postId, bool published)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE BlogPosts 
            SET Published = @published, UpdatedAt = datetime('now')
            WHERE Id = @postId
        ";
            command.Parameters.AddWithValue("@postId", postId);
            command.Parameters.AddWithValue("@published", published ? 1 : 0);

            return command.ExecuteNonQuery() > 0;
        }

        // Settings Management
        public void InitializeSettingsTables()
        {
            using var connection = GetConnection();

            // Settings table already exists from Program.cs, just ensure it's there
            var createTable = connection.CreateCommand();
            createTable.CommandText = @"
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )";
            createTable.ExecuteNonQuery();
        }

        public string? GetSetting(string key)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE Key = @key";
            command.Parameters.AddWithValue("@key", key);

            return command.ExecuteScalar() as string;
        }

        public void SetSetting(string key, string value)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT OR REPLACE INTO Settings (Key, Value, UpdatedAt)
            VALUES (@key, @value, datetime('now'))
        ";
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);
            command.ExecuteNonQuery();
        }

        public Dictionary<string, string> GetAllSettings()
        {
            var settings = new Dictionary<string, string>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Key, Value FROM Settings";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                settings[reader.GetString(0)] = reader.GetString(1);
            }

            return settings;
        }

        // Forum Thread and Post Details
        public ForumThreadInfo? GetForumThreadById(int threadId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                ft.Id, ft.Title, ft.ForumId, f.Name as ForumName,
                ft.UserId, u.Username, ft.ViewCount, ft.IsLocked, ft.IsSticky,
                ft.CreatedAt, ft.UpdatedAt
            FROM ForumThreads ft
            LEFT JOIN Forums f ON ft.ForumId = f.Id
            LEFT JOIN Users u ON ft.UserId = u.Id
            WHERE ft.Id = @threadId
        ";
            command.Parameters.AddWithValue("@threadId", threadId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ForumThreadInfo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    ForumId = reader.GetInt32(2),
                    ForumName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    UserId = reader.GetInt32(4),
                    Username = reader.IsDBNull(5) ? "Unknown" : reader.GetString(5),
                    ViewCount = reader.GetInt32(6),
                    IsLocked = reader.GetInt32(7) == 1,
                    IsSticky = reader.GetInt32(8) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(9)),
                    UpdatedAt = DateTime.Parse(reader.GetString(10))
                };
            }

            return null;
        }

        public List<ForumPostInfo> GetThreadPosts(int threadId)
        {
            var posts = new List<ForumPostInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                fp.Id, fp.ThreadId, fp.UserId, u.Username, u.Title as UserTitle,
                fp.Content, fp.IsFirstPost, fp.CreatedAt, fp.UpdatedAt
            FROM ForumPosts fp
            LEFT JOIN Users u ON fp.UserId = u.Id
            WHERE fp.ThreadId = @threadId
            ORDER BY fp.CreatedAt ASC
        ";
            command.Parameters.AddWithValue("@threadId", threadId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                posts.Add(new ForumPostInfo
                {
                    Id = reader.GetInt32(0),
                    ThreadId = reader.GetInt32(1),
                    UserId = reader.GetInt32(2),
                    Username = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3),
                    UserTitle = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Content = reader.GetString(5),
                    IsFirstPost = reader.GetInt32(6) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    UpdatedAt = DateTime.Parse(reader.GetString(8))
                });
            }

            return posts;
        }

        public List<ForumThreadInfo> GetForumThreads(int forumId)
        {
            var threads = new List<ForumThreadInfo>();

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                ft.Id, ft.Title, ft.ForumId, f.Name as ForumName,
                ft.UserId, u.Username, ft.ViewCount, ft.IsLocked, ft.IsSticky,
                ft.CreatedAt, ft.UpdatedAt,
                COUNT(fp.Id) as PostCount
            FROM ForumThreads ft
            LEFT JOIN Forums f ON ft.ForumId = f.Id
            LEFT JOIN Users u ON ft.UserId = u.Id
            LEFT JOIN ForumPosts fp ON ft.Id = fp.ThreadId
            WHERE ft.ForumId = @forumId
            GROUP BY ft.Id
            ORDER BY ft.IsSticky DESC, ft.UpdatedAt DESC
        ";
            command.Parameters.AddWithValue("@forumId", forumId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                threads.Add(new ForumThreadInfo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    ForumId = reader.GetInt32(2),
                    ForumName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    UserId = reader.GetInt32(4),
                    Username = reader.IsDBNull(5) ? "Unknown" : reader.GetString(5),
                    ViewCount = reader.GetInt32(6),
                    IsLocked = reader.GetInt32(7) == 1,
                    IsSticky = reader.GetInt32(8) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(9)),
                    UpdatedAt = DateTime.Parse(reader.GetString(10)),
                    PostCount = reader.GetInt32(11)
                });
            }

            return threads;
        }

        public ForumInfo? GetForumById(int forumId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                f.Id, f.Name, f.Description, f.Icon,
                COUNT(DISTINCT ft.Id) as ThreadCount,
                COUNT(DISTINCT fp.Id) as PostCount
            FROM Forums f
            LEFT JOIN ForumThreads ft ON f.Id = ft.ForumId
            LEFT JOIN ForumPosts fp ON ft.Id = fp.ThreadId
            WHERE f.Id = @forumId
            GROUP BY f.Id
        ";
            command.Parameters.AddWithValue("@forumId", forumId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Services.ForumInfo
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Icon = reader.IsDBNull(3) ? "💬" : reader.GetString(3),
                    ThreadCount = reader.GetInt32(4),
                    PostCount = reader.GetInt32(5)
                };
            }

            return null;
        }

        // Moderator Management Methods
        public bool AssignModerator(int userId, int categoryId)
        {
            try
            {
                using var connection = GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                INSERT OR IGNORE INTO ForumModerators (UserId, CategoryId)
                VALUES (@userId, @categoryId)
            ";
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@categoryId", categoryId);
                return command.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveModerator(int userId, int categoryId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM ForumModerators
            WHERE UserId = @userId AND CategoryId = @categoryId
        ";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@categoryId", categoryId);
            return command.ExecuteNonQuery() > 0;
        }

        public bool IsUserModerator(int userId, int categoryId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT COUNT(*) FROM ForumModerators
            WHERE UserId = @userId AND CategoryId = @categoryId
        ";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@categoryId", categoryId);
            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        public List<ModeratorWithDetails> GetModeratorsForCategory(int categoryId)
        {
            var moderators = new List<ModeratorWithDetails>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT u.Id, u.Username, u.Email, fm.AssignedAt
            FROM ForumModerators fm
            JOIN Users u ON fm.UserId = u.Id
            WHERE fm.CategoryId = @categoryId
            ORDER BY u.Username
        ";
            command.Parameters.AddWithValue("@categoryId", categoryId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                moderators.Add(new ModeratorWithDetails
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    AssignedAt = DateTime.Parse(reader.GetString(3))
                });
            }

            return moderators;
        }

        public List<ModeratorAssignment> GetAllModerators()
        {
            var moderators = new List<ModeratorAssignment>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT u.Id, u.Username, fc.Name as CategoryName
            FROM ForumModerators fm
            JOIN Users u ON fm.UserId = u.Id
            JOIN ForumCategories fc ON fm.CategoryId = fc.Id
            ORDER BY u.Username, fc.Name
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                moderators.Add(new ModeratorAssignment
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    CategoryName = reader.GetString(2)
                });
            }

            return moderators;
        }

        // Thread Management Methods
        public bool LockThread(int threadId, int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE ForumThreads
            SET IsLocked = 1, UpdatedAt = datetime('now')
            WHERE Id = @threadId
        ";
            command.Parameters.AddWithValue("@threadId", threadId);
            return command.ExecuteNonQuery() > 0;
        }

        public bool UnlockThread(int threadId, int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE ForumThreads
            SET IsLocked = 0, UpdatedAt = datetime('now')
            WHERE Id = @threadId
        ";
            command.Parameters.AddWithValue("@threadId", threadId);
            return command.ExecuteNonQuery() > 0;
        }

        public bool PinThread(int threadId, int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE ForumThreads
            SET IsSticky = 1, UpdatedAt = datetime('now')
            WHERE Id = @threadId
        ";
            command.Parameters.AddWithValue("@threadId", threadId);
            return command.ExecuteNonQuery() > 0;
        }

        public bool UnpinThread(int threadId, int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE ForumThreads
            SET IsSticky = 0, UpdatedAt = datetime('now')
            WHERE Id = @threadId
        ";
            command.Parameters.AddWithValue("@threadId", threadId);
            return command.ExecuteNonQuery() > 0;
        }

        public bool DeleteThread(int threadId, int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ForumThreads WHERE Id = @threadId";
            command.Parameters.AddWithValue("@threadId", threadId);
            return command.ExecuteNonQuery() > 0;
        }

        // Post Management Methods
        public bool DeletePost(int postId, int deletedByUserId, string reason)
        {
            using var connection = GetConnection();

            // First, get post details for the deleted posts log
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
            SELECT ThreadId, UserId, Content
            FROM ForumPosts
            WHERE Id = @postId
        ";
            selectCmd.Parameters.AddWithValue("@postId", postId);

            using var reader = selectCmd.ExecuteReader();
            if (reader.Read())
            {
                var threadId = reader.GetInt32(0);
                var authorUserId = reader.GetInt32(1);
                var content = reader.GetString(2);
                reader.Close();

                // Log the deletion
                var logCmd = connection.CreateCommand();
                logCmd.CommandText = @"
                INSERT INTO DeletedPosts (PostId, ThreadId, Content, AuthorUserId, DeletedByUserId, DeleteReason)
                VALUES (@postId, @threadId, @content, @authorUserId, @deletedByUserId, @reason)
            ";
                logCmd.Parameters.AddWithValue("@postId", postId);
                logCmd.Parameters.AddWithValue("@threadId", threadId);
                logCmd.Parameters.AddWithValue("@content", content);
                logCmd.Parameters.AddWithValue("@authorUserId", authorUserId);
                logCmd.Parameters.AddWithValue("@deletedByUserId", deletedByUserId);
                logCmd.Parameters.AddWithValue("@reason", reason ?? string.Empty);
                logCmd.ExecuteNonQuery();

                // Delete the post
                var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM ForumPosts WHERE Id = @postId";
                deleteCmd.Parameters.AddWithValue("@postId", postId);
                return deleteCmd.ExecuteNonQuery() > 0;
            }

            return false;
        }

        public bool FlagPost(int postId, int threadId, int flaggedByUserId, string reason)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO FlaggedPosts (PostId, ThreadId, FlaggedByUserId, Reason)
            VALUES (@postId, @threadId, @flaggedByUserId, @reason)
        ";
            command.Parameters.AddWithValue("@postId", postId);
            command.Parameters.AddWithValue("@threadId", threadId);
            command.Parameters.AddWithValue("@flaggedByUserId", flaggedByUserId);
            command.Parameters.AddWithValue("@reason", reason);
            return command.ExecuteNonQuery() > 0;
        }

        public bool ResolveFlaggedPost(int flagId, int resolvedByUserId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE FlaggedPosts
            SET Status = 'resolved', ResolvedAt = datetime('now'), ResolvedByUserId = @resolvedByUserId
            WHERE Id = @flagId
        ";
            command.Parameters.AddWithValue("@flagId", flagId);
            command.Parameters.AddWithValue("@resolvedByUserId", resolvedByUserId);
            return command.ExecuteNonQuery() > 0;
        }

        public ForumPostInfo? GetPostById(int postId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                fp.Id, fp.ThreadId, fp.UserId, u.Username, u.Title as UserTitle,
                fp.Content, fp.IsFirstPost, fp.CreatedAt, fp.UpdatedAt
            FROM ForumPosts fp
            LEFT JOIN Users u ON fp.UserId = u.Id
            WHERE fp.Id = @postId
        ";
            command.Parameters.AddWithValue("@postId", postId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ForumPostInfo
                {
                    Id = reader.GetInt32(0),
                    ThreadId = reader.GetInt32(1),
                    UserId = reader.GetInt32(2),
                    Username = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3),
                    UserTitle = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Content = reader.GetString(5),
                    IsFirstPost = reader.GetInt32(6) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    UpdatedAt = DateTime.Parse(reader.GetString(8))
                };
            }

            return null;
        }

        // Moderation Statistics and Lists
        public List<FlaggedPostInfo> GetFlaggedPosts()
        {
            var flaggedPosts = new List<FlaggedPostInfo>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                fp.Id, fp.PostId, fp.ThreadId, ft.Title as ThreadTitle,
                u1.Username as Author, u2.Username as FlaggedBy,
                fp.Reason, fp.CreatedAt, fp.Status
            FROM FlaggedPosts fp
            JOIN ForumPosts fpost ON fp.PostId = fpost.Id
            JOIN ForumThreads ft ON fp.ThreadId = ft.Id
            JOIN Users u1 ON fpost.UserId = u1.Id
            JOIN Users u2 ON fp.FlaggedByUserId = u2.Id
            WHERE fp.Status = 'pending'
            ORDER BY fp.CreatedAt DESC
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                flaggedPosts.Add(new FlaggedPostInfo
                {
                    Id = reader.GetInt32(0),
                    PostId = reader.GetInt32(1),
                    ThreadId = reader.GetInt32(2),
                    ThreadTitle = reader.GetString(3),
                    Author = reader.GetString(4),
                    FlaggedBy = reader.GetString(5),
                    Reason = reader.GetString(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    Status = reader.GetString(8)
                });
            }

            return flaggedPosts;
        }

        public List<LockedThreadInfo> GetLockedThreads()
        {
            var lockedThreads = new List<LockedThreadInfo>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                ft.Id, ft.Title, u.Username as Author,
                ft.CreatedAt, ft.UpdatedAt
            FROM ForumThreads ft
            JOIN Users u ON ft.UserId = u.Id
            WHERE ft.IsLocked = 1
            ORDER BY ft.UpdatedAt DESC
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lockedThreads.Add(new LockedThreadInfo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Author = reader.GetString(2),
                    LockedAt = DateTime.Parse(reader.GetString(4))
                });
            }

            return lockedThreads;
        }

        public List<BannedUserInfo> GetBannedUsers()
        {
            var bannedUsers = new List<BannedUserInfo>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                bu.Id, u1.Id as UserId, u1.Username,
                u2.Username as BannedBy, bu.Reason,
                bu.BannedAt, bu.ExpiresAt, bu.IsActive
            FROM BannedUsers bu
            JOIN Users u1 ON bu.UserId = u1.Id
            JOIN Users u2 ON bu.BannedByUserId = u2.Id
            WHERE bu.IsActive = 1
            ORDER BY bu.BannedAt DESC
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                bannedUsers.Add(new BannedUserInfo
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Username = reader.GetString(2),
                    BannedBy = reader.GetString(3),
                    Reason = reader.GetString(4),
                    BannedAt = DateTime.Parse(reader.GetString(5)),
                    ExpiresAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
                    IsActive = reader.GetInt32(7) == 1
                });
            }

            return bannedUsers;
        }

        public List<DeletedPostInfo> GetDeletedPosts(int limit = 50)
        {
            var deletedPosts = new List<DeletedPostInfo>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                dp.Id, dp.PostId, dp.ThreadId, ft.Title as ThreadTitle,
                u1.Username as Author, u2.Username as DeletedBy,
                dp.DeleteReason, dp.DeletedAt
            FROM DeletedPosts dp
            JOIN ForumThreads ft ON dp.ThreadId = ft.Id
            JOIN Users u1 ON dp.AuthorUserId = u1.Id
            JOIN Users u2 ON dp.DeletedByUserId = u2.Id
            ORDER BY dp.DeletedAt DESC
            LIMIT @limit
        ";
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                deletedPosts.Add(new DeletedPostInfo
                {
                    Id = reader.GetInt32(0),
                    PostId = reader.GetInt32(1),
                    ThreadId = reader.GetInt32(2),
                    ThreadTitle = reader.GetString(3),
                    Author = reader.GetString(4),
                    DeletedBy = reader.GetString(5),
                    DeleteReason = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    DeletedAt = DateTime.Parse(reader.GetString(7))
                });
            }

            return deletedPosts;
        }

        public bool BanUser(int userId, int bannedByUserId, string reason, DateTime? expiresAt = null)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO BannedUsers (UserId, BannedByUserId, Reason, ExpiresAt)
            VALUES (@userId, @bannedByUserId, @reason, @expiresAt)
        ";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@bannedByUserId", bannedByUserId);
            command.Parameters.AddWithValue("@reason", reason);
            command.Parameters.AddWithValue("@expiresAt", expiresAt.HasValue ? expiresAt.Value.ToString("o") : (object)DBNull.Value);
            return command.ExecuteNonQuery() > 0;
        }

        public bool UnbanUser(int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE BannedUsers
            SET IsActive = 0
            WHERE UserId = @userId AND IsActive = 1
        ";
            command.Parameters.AddWithValue("@userId", userId);
            return command.ExecuteNonQuery() > 0;
        }

        public bool IsUserBanned(int userId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT COUNT(*) FROM BannedUsers
            WHERE UserId = @userId 
            AND IsActive = 1
            AND (ExpiresAt IS NULL OR datetime(ExpiresAt) > datetime('now'))
        ";
            command.Parameters.AddWithValue("@userId", userId);
            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        // Moderation Statistics
        public ModerationStats GetModerationStats()
        {
            using var connection = GetConnection();

            var stats = new ModerationStats();

            // Total threads
            var threadsCmd = connection.CreateCommand();
            threadsCmd.CommandText = "SELECT COUNT(*) FROM ForumThreads";
            stats.TotalThreads = Convert.ToInt32(threadsCmd.ExecuteScalar());

            // Total posts
            var postsCmd = connection.CreateCommand();
            postsCmd.CommandText = "SELECT COUNT(*) FROM ForumPosts";
            stats.TotalPosts = Convert.ToInt32(postsCmd.ExecuteScalar());

            // Deleted posts count
            var deletedCmd = connection.CreateCommand();
            deletedCmd.CommandText = "SELECT COUNT(*) FROM DeletedPosts";
            stats.DeletedPostsCount = Convert.ToInt32(deletedCmd.ExecuteScalar());

            // Locked threads count
            var lockedCmd = connection.CreateCommand();
            lockedCmd.CommandText = "SELECT COUNT(*) FROM ForumThreads WHERE IsLocked = 1";
            stats.LockedThreadsCount = Convert.ToInt32(lockedCmd.ExecuteScalar());

            // Banned users count
            var bannedCmd = connection.CreateCommand();
            bannedCmd.CommandText = "SELECT COUNT(*) FROM BannedUsers WHERE IsActive = 1";
            stats.BannedUsers = Convert.ToInt32(bannedCmd.ExecuteScalar());

            // Active warnings/flagged posts
            var flaggedCmd = connection.CreateCommand();
            flaggedCmd.CommandText = "SELECT COUNT(*) FROM FlaggedPosts WHERE Status = 'pending'";
            stats.ActiveWarnings = Convert.ToInt32(flaggedCmd.ExecuteScalar());

            // Blog moderation stats
            var flaggedBlogCommentsCmd = connection.CreateCommand();
            flaggedBlogCommentsCmd.CommandText = "SELECT COUNT(*) FROM FlaggedBlogComments WHERE Status = 'pending'";
            stats.FlaggedBlogComments = Convert.ToInt32(flaggedBlogCommentsCmd.ExecuteScalar() ?? 0);

            var deletedBlogCommentsCmd = connection.CreateCommand();
            deletedBlogCommentsCmd.CommandText = "SELECT COUNT(*) FROM DeletedBlogComments";
            stats.DeletedBlogComments = Convert.ToInt32(deletedBlogCommentsCmd.ExecuteScalar() ?? 0);

            var flaggedBlogPostsCmd = connection.CreateCommand();
            flaggedBlogPostsCmd.CommandText = "SELECT COUNT(*) FROM FlaggedBlogPosts WHERE Status = 'pending'";
            stats.FlaggedBlogPosts = Convert.ToInt32(flaggedBlogPostsCmd.ExecuteScalar() ?? 0);

            return stats;
        }

        // Blog Moderation Methods
        public List<FlaggedBlogCommentInfo> GetFlaggedBlogComments()
        {
            var flaggedComments = new List<FlaggedBlogCommentInfo>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                fbc.Id, fbc.CommentId, fbc.PostId, bp.Title as PostTitle,
                u1.Username as Author, u2.Username as FlaggedBy,
                fbc.Reason, fbc.CreatedAt, fbc.Status
            FROM FlaggedBlogComments fbc
            JOIN BlogComments bc ON fbc.CommentId = bc.Id
            JOIN BlogPosts bp ON fbc.PostId = bp.Id
            JOIN Users u1 ON bc.AuthorId = u1.Id
            JOIN Users u2 ON fbc.FlaggedByUserId = u2.Id
            WHERE fbc.Status = 'pending'
            ORDER BY fbc.CreatedAt DESC
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                flaggedComments.Add(new FlaggedBlogCommentInfo
                {
                    Id = reader.GetInt32(0),
                    CommentId = reader.GetInt32(1),
                    PostId = reader.GetInt32(2),
                    PostTitle = reader.GetString(3),
                    Author = reader.GetString(4),
                    FlaggedBy = reader.GetString(5),
                    Reason = reader.GetString(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    Status = reader.GetString(8)
                });
            }

            return flaggedComments;
        }

        public List<FlaggedBlogPostInfo> GetFlaggedBlogPosts()
        {
            var flaggedPosts = new List<FlaggedBlogPostInfo>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                fbp.Id, fbp.PostId, bp.Title, bp.Slug,
                u1.Username as Author, u2.Username as FlaggedBy,
                fbp.Reason, fbp.CreatedAt, fbp.Status
            FROM FlaggedBlogPosts fbp
            JOIN BlogPosts bp ON fbp.PostId = bp.Id
            JOIN Users u1 ON bp.AuthorId = u1.Id
            JOIN Users u2 ON fbp.FlaggedByUserId = u2.Id
            WHERE fbp.Status = 'pending'
            ORDER BY fbp.CreatedAt DESC
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                flaggedPosts.Add(new FlaggedBlogPostInfo
                {
                    Id = reader.GetInt32(0),
                    PostId = reader.GetInt32(1),
                    PostTitle = reader.GetString(2),
                    PostSlug = reader.GetString(3),
                    Author = reader.GetString(4),
                    FlaggedBy = reader.GetString(5),
                    Reason = reader.GetString(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    Status = reader.GetString(8)
                });
            }

            return flaggedPosts;
        }

        public List<DeletedBlogCommentInfo> GetDeletedBlogComments(int limit = 50)
        {
            var deletedComments = new List<DeletedBlogCommentInfo>();
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT 
                dbc.Id, dbc.CommentId, dbc.PostId, bp.Title as PostTitle,
                u1.Username as Author, u2.Username as DeletedBy,
                dbc.DeleteReason, dbc.DeletedAt
            FROM DeletedBlogComments dbc
            JOIN BlogPosts bp ON dbc.PostId = bp.Id
            JOIN Users u1 ON dbc.AuthorUserId = u1.Id
            JOIN Users u2 ON dbc.DeletedByUserId = u2.Id
            ORDER BY dbc.DeletedAt DESC
            LIMIT @limit
        ";
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                deletedComments.Add(new DeletedBlogCommentInfo
                {
                    Id = reader.GetInt32(0),
                    CommentId = reader.GetInt32(1),
                    PostId = reader.GetInt32(2),
                    PostTitle = reader.GetString(3),
                    Author = reader.GetString(4),
                    DeletedBy = reader.GetString(5),
                    DeleteReason = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    DeletedAt = DateTime.Parse(reader.GetString(7))
                });
            }

            return deletedComments;
        }

        public bool ResolveFlaggedBlogComment(int flagId, int resolvedByUserId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE FlaggedBlogComments
            SET Status = 'resolved', ResolvedAt = datetime('now'), ResolvedByUserId = @resolvedByUserId
            WHERE Id = @flagId
        ";
            command.Parameters.AddWithValue("@flagId", flagId);
            command.Parameters.AddWithValue("@resolvedByUserId", resolvedByUserId);
            return command.ExecuteNonQuery() > 0;
        }

        public bool ResolveFlaggedBlogPost(int flagId, int resolvedByUserId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE FlaggedBlogPosts
            SET Status = 'resolved', ResolvedAt = datetime('now'), ResolvedByUserId = @resolvedByUserId
            WHERE Id = @flagId
        ";
            command.Parameters.AddWithValue("@flagId", flagId);
            command.Parameters.AddWithValue("@resolvedByUserId", resolvedByUserId);
            return command.ExecuteNonQuery() > 0;
        }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ForumCategoryWithStats
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "💬";
        public bool IsPrivate { get; set; }
        public int ForumCount { get; set; }
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }
    }

    public class ForumInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "💬";
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }
    }

    public class ForumThreadInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ForumId { get; set; }
        public string ForumName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public bool IsLocked { get; set; }
        public bool IsSticky { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int PostCount { get; set; }
    }

    public class ForumPostInfo
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserTitle { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsFirstPost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ModeratorWithDetails
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class ModeratorAssignment
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }

    public class FlaggedPostInfo
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int ThreadId { get; set; }
        public string ThreadTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string FlaggedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class LockedThreadInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime LockedAt { get; set; }
    }

    public class BannedUserInfo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string BannedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime BannedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class DeletedPostInfo
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int ThreadId { get; set; }
        public string ThreadTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string DeletedBy { get; set; } = string.Empty;
        public string DeleteReason { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
    }

    public class ModerationStats
    {
        public int TotalThreads { get; set; }
        public int TotalPosts { get; set; }
        public int DeletedPostsCount { get; set; }
        public int LockedThreadsCount { get; set; }
        public int BannedUsers { get; set; }
        public int ActiveWarnings { get; set; }
        public int FlaggedBlogComments { get; set; }
        public int DeletedBlogComments { get; set; }
        public int FlaggedBlogPosts { get; set; }
    }

    public class DownloadCategoryInfo
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "📁";
        public int FileCount { get; set; }
        public bool IsPrivate { get; set; }
    }

    public class DownloadFileInfo
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public int DownloadCount { get; set; }
        public bool IsPublic { get; set; }
    }

    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalForumPosts { get; set; }
        public int TotalBlogPosts { get; set; }
        public int ActiveSessions { get; set; }
        public int TotalDownloads { get; set; }
    }

    public class RecentActivityInfo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class BlogCategoryInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int PostCount { get; set; }
    }

    public class BlogPostInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool Published { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class FlaggedBlogCommentInfo
    {
        public int Id { get; set; }
        public int CommentId { get; set; }
        public int PostId { get; set; }
        public string PostTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string FlaggedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class FlaggedBlogPostInfo
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string PostTitle { get; set; } = string.Empty;
        public string PostSlug { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string FlaggedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DeletedBlogCommentInfo
    {
        public int Id { get; set; }
        public int CommentId { get; set; }
        public int PostId { get; set; }
        public string PostTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string DeletedBy { get; set; } = string.Empty;
        public string DeleteReason { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
    }

}

// Authentication helper extension methods for DatabaseService
namespace AGP_CMS.Services
{
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Data.Sqlite;

    public partial class DatabaseService
    {
        public UserInfo? AuthenticateUser(string usernameOrEmail, string password)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Username, Email, PasswordHash, Role 
                FROM Users 
                WHERE (Username = @username OR Email = @username) AND IsActive = 1
            ";
            command.Parameters.AddWithValue("@username", usernameOrEmail);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var storedHash = reader.GetString(3);
                var passwordHash = HashPassword(password);

                if (storedHash == passwordHash)
                {
                    return new UserInfo
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Email = reader.GetString(2),
                        Role = reader.GetString(4)
                    };
                }
            }

            return null;
        }

        public bool UserExists(string username, string email)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username OR Email = @email";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        public int CreateUser(string username, string email, string password)
        {
            using var connection = GetConnection();
            
            // Check if this is the first user
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Role = 'Admin'";
            var adminCount = Convert.ToInt32(checkCmd.ExecuteScalar());
            var role = adminCount == 0 ? "Admin" : "User";

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (Username, Email, PasswordHash, Role, CreatedAt, UpdatedAt, IsActive)
                VALUES (@username, @email, @passwordHash, @role, @createdAt, @updatedAt, 1);
                SELECT last_insert_rowid();
            ";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@passwordHash", HashPassword(password));
            command.Parameters.AddWithValue("@role", role);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));

            var userId = Convert.ToInt32(command.ExecuteScalar());

            // Create user profile
            var profileCmd = connection.CreateCommand();
            profileCmd.CommandText = @"
                INSERT INTO UserProfiles (UserId, PostCount, LikesReceived)
                VALUES (@userId, 0, 0)
            ";
            profileCmd.Parameters.AddWithValue("@userId", userId);
            profileCmd.ExecuteNonQuery();

            return userId;
        }

        public string CreateSession(int userId, string username, HttpContext httpContext)
        {
            var sessionId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Sessions (Id, UserId, CreatedAt, ExpiresAt, IpAddress, UserAgent)
                VALUES (@id, @userId, @createdAt, @expiresAt, @ip, @userAgent)
            ";
            command.Parameters.AddWithValue("@id", sessionId);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@expiresAt", expiresAt.ToString("o"));
            command.Parameters.AddWithValue("@ip", httpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            command.Parameters.AddWithValue("@userAgent", httpContext.Request.Headers["User-Agent"].ToString());
            command.ExecuteNonQuery();

            // Update last login
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE Users SET LastLoginAt = @lastLogin WHERE Id = @userId";
            updateCommand.Parameters.AddWithValue("@lastLogin", DateTime.UtcNow.ToString("o"));
            updateCommand.Parameters.AddWithValue("@userId", userId);
            updateCommand.ExecuteNonQuery();

            // Set cookies
            httpContext.Response.Cookies.Append("AGPCMS_SESSION", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            });

            httpContext.Response.Cookies.Append("AGPCMS_USER", username, new CookieOptions
            {
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            });

            return sessionId;
        }

        public int? GetUserIdBySession(string sessionId)
        {
            using var connection = GetConnection();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT UserId 
                FROM Sessions 
                WHERE Id = @sessionId AND datetime(ExpiresAt) > datetime('now')
            ";
            command.Parameters.AddWithValue("@sessionId", sessionId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return reader.GetInt32(0);
            }

            return null;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "AGP_CMS_SALT"));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
