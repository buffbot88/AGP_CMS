using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Learning
{
    public class AchievementsModel : PageModel
    {
        private readonly DatabaseService _db;

        public UserInfo? CurrentUser { get; set; }
        public bool IsAuthenticated { get; set; }
        public List<Achievement> Achievements { get; set; } = new();

        public AchievementsModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is authenticated
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            IsAuthenticated = userId.HasValue;

            if (userId.HasValue)
            {
                CurrentUser = _db.GetUserById(userId.Value);
                LoadAchievements(userId.Value);
            }

            return Page();
        }

        private void LoadAchievements(int userId)
        {
            // Define some sample achievements
            // In a real implementation, these would be loaded from the database
            Achievements = new List<Achievement>
            {
                new Achievement
                {
                    Id = 1,
                    Name = "First Steps",
                    Description = "Complete your first course",
                    Icon = "🎓",
                    IsUnlocked = false,
                    Progress = 0
                },
                new Achievement
                {
                    Id = 2,
                    Name = "Knowledge Seeker",
                    Description = "Complete 5 courses",
                    Icon = "📚",
                    IsUnlocked = false,
                    Progress = 0
                },
                new Achievement
                {
                    Id = 3,
                    Name = "Master Learner",
                    Description = "Complete 10 courses",
                    Icon = "🏆",
                    IsUnlocked = false,
                    Progress = 0
                },
                new Achievement
                {
                    Id = 4,
                    Name = "Dedicated Student",
                    Description = "Study for 10 hours",
                    Icon = "⏰",
                    IsUnlocked = false,
                    Progress = 0
                },
                new Achievement
                {
                    Id = 5,
                    Name = "Perfect Score",
                    Description = "Get 100% on a course quiz",
                    Icon = "💯",
                    IsUnlocked = false,
                    Progress = 0
                }
            };
        }
    }

    public class Achievement
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; }
        public int Progress { get; set; }
    }
}
