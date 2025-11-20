using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace AGP_CMS.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseService _db;

        public bool IsAuthenticated { get; set; }
        public string Username { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int TotalPosts { get; set; }
        public int TotalBlogs { get; set; }
        public int OnlineUsers { get; set; }
        public string HomePageTitle { get; set; } = "AGP Studios, INC - CMS";
        public string HomePageTagline { get; set; } = "A Complete Content Management System";
        public string HomePageWelcomeMessage { get; set; } = "Forums Hosting 5$ * CMS with Blogs and Forums 20$";
        public string CurrentTheme { get; set; } = "classic";

        public IndexModel(IConfiguration configuration, DatabaseService db)
        {
            _configuration = configuration;
            _db = db;
        }

        public void OnGet()
        {
            // Load statistics from database
            LoadStatistics();

            // Check authentication status
            CheckAuthentication();

            // Load homepage settings
            LoadHomePageSettings();
        }

        private void LoadHomePageSettings()
        {
            var settings = _db.GetAllSettings();
            HomePageTitle = settings.GetValueOrDefault("HomePageTitle", "ASHAT OS CMS");
            HomePageTagline = settings.GetValueOrDefault("HomePageTagline", "Your Complete Content Management System");
            HomePageWelcomeMessage = settings.GetValueOrDefault("HomePageWelcomeMessage", "Forums Hosting 5$ * CMS with Blogs and Forums 20$");
            CurrentTheme = settings.GetValueOrDefault("DefaultTheme", "classic");
        }

        private void LoadStatistics()
        {
            var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";

            try
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // Get total users
                var usersCmd = connection.CreateCommand();
                usersCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE IsActive = 1";
                TotalUsers = Convert.ToInt32(usersCmd.ExecuteScalar());

                // Get total forum posts
                var postsCmd = connection.CreateCommand();
                postsCmd.CommandText = "SELECT COUNT(*) FROM ForumPosts WHERE IsDeleted = 0";
                TotalPosts = Convert.ToInt32(postsCmd.ExecuteScalar());

                // Get total blog posts
                var blogsCmd = connection.CreateCommand();
                blogsCmd.CommandText = "SELECT COUNT(*) FROM BlogPosts WHERE Published = 1";
                TotalBlogs = Convert.ToInt32(blogsCmd.ExecuteScalar());

                // Get online users (sessions active in last 15 minutes)
                var onlineCmd = connection.CreateCommand();
                onlineCmd.CommandText = "SELECT COUNT(DISTINCT UserId) FROM Sessions WHERE datetime(ExpiresAt) > datetime('now')";
                OnlineUsers = Convert.ToInt32(onlineCmd.ExecuteScalar());
            }
            catch
            {
                // Fallback values if database query fails
                TotalUsers = 0;
                TotalPosts = 0;
                TotalBlogs = 0;
                OnlineUsers = 0;
            }
        }

        private void CheckAuthentication()
        {
            // Check for session cookie
            if (Request.Cookies.TryGetValue("AGPCMS_SESSION", out var sessionId))
            {
                var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";

                try
                {
                    using var connection = new SqliteConnection(connectionString);
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                    SELECT u.Username 
                    FROM Sessions s 
                    JOIN Users u ON s.UserId = u.Id 
                    WHERE s.Id = @sessionId AND datetime(s.ExpiresAt) > datetime('now')
                ";
                    command.Parameters.AddWithValue("@sessionId", sessionId);

                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        IsAuthenticated = true;
                        Username = reader.GetString(0);
                    }
                }
                catch
                {
                    IsAuthenticated = false;
                    Username = string.Empty;
                }
            }
        }
    }
}
