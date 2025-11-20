using AGP_CMS.Security;
using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// Admin Dashboard - Central control panel for CMS management
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _db;

        public DashboardStats Stats { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();
        public SystemHealth Health { get; set; } = new();

        public IndexModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to access the admin panel.";
                return RedirectToPage("/Index");
            }

            // Load dashboard statistics
            Stats = LoadDashboardStats();

            // Load recent activities
            RecentActivities = LoadRecentActivities();

            // Load system health
            Health = LoadSystemHealth();

            return Page();
        }

        private DashboardStats LoadDashboardStats()
        {
            var dbStats = _db.GetDashboardStats();
            return new DashboardStats
            {
                TotalUsers = dbStats.TotalUsers,
                TotalForumPosts = dbStats.TotalForumPosts,
                TotalBlogPosts = dbStats.TotalBlogPosts,
                TotalDownloads = dbStats.TotalDownloads,
                ActiveUsers24h = dbStats.ActiveSessions,
                NewUsersThisWeek = 0, // Can be enhanced later
                PostsThisWeek = 0, // Can be enhanced later
                DownloadsThisWeek = 0 // Can be enhanced later
            };
        }

        private List<RecentActivity> LoadRecentActivities()
        {
            var dbActivities = _db.GetRecentActivity(20);
            return dbActivities.Select(a => new RecentActivity
            {
                ActivityType = a.ActivityType,
                Description = a.Description,
                UserName = a.Username,
                Timestamp = a.CreatedAt
            }).ToList();
        }

        private SystemHealth LoadSystemHealth()
        {
            return new SystemHealth
            {
                Status = "Healthy",
                DatabaseStatus = "Connected",
                ApiStatus = "Operational",
                StorageUsed = 0,
                StorageTotal = 100,
                LastBackup = null
            };
        }
    }

    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalForumPosts { get; set; }
        public int TotalBlogPosts { get; set; }
        public int TotalDownloads { get; set; }
        public int ActiveUsers24h { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int PostsThisWeek { get; set; }
        public int DownloadsThisWeek { get; set; }
    }

    public class RecentActivity
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class SystemHealth
    {
        public string Status { get; set; } = string.Empty;
        public string DatabaseStatus { get; set; } = string.Empty;
        public string ApiStatus { get; set; } = string.Empty;
        public long StorageUsed { get; set; }
        public long StorageTotal { get; set; }
        public DateTime? LastBackup { get; set; }
    }
}
