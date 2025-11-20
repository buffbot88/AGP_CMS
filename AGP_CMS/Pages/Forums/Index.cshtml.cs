using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Forums
{
    /// <summary>
    /// Razor Page Model for Forums Index - Professional vBulletin-style forum interface
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _db;

        public List<ForumCategory> Categories { get; set; } = new();
        public int TotalThreads { get; set; }
        public int TotalPosts { get; set; }
        public int TotalMembers { get; set; }
        public int OnlineUsers { get; set; }

        public IndexModel(DatabaseService db)
        {
            _db = db;
        }

        public void OnGet()
        {
            // Load forum statistics
            LoadStatistics();

            // Load forum categories and forums
            Categories = LoadForumCategories();
        }

        private void LoadStatistics()
        {
            var stats = _db.GetDashboardStats();
            TotalThreads = 0; // Can add method to DatabaseService to get thread count
            TotalPosts = stats.TotalForumPosts;
            TotalMembers = stats.TotalUsers;
            OnlineUsers = stats.ActiveSessions;
        }

        private List<ForumCategory> LoadForumCategories()
        {
            var categories = _db.GetForumCategories();
            return categories.Select(c => new ForumCategory
            {
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon,
                Forums = _db.GetForumsByCategory(c.Id).Select(f => new ForumInfo
                {
                    Id = f.Id,
                    Title = f.Name,
                    Description = f.Description,
                    Icon = f.Icon,
                    TopicCount = f.ThreadCount,
                    PostCount = f.PostCount,
                    LastPostUser = string.Empty, // Can be enhanced later
                    LastPostDate = DateTime.MinValue // Can be enhanced later
                }).ToList()
            }).ToList();
        }
    }

    public class ForumCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "📁";
        public List<ForumInfo> Forums { get; set; } = new();
    }

    public class ForumInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "💬";
        public int TopicCount { get; set; }
        public int PostCount { get; set; }
        public string LastPostUser { get; set; } = string.Empty;
        public DateTime LastPostDate { get; set; }
    }
}
