using Abstractions;
using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Profiles
{
    /// <summary>
    /// Razor Page Model for User Profiles - MySpace-style social profiles
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _db;

        public Abstractions.UserProfile? Profile { get; set; }
        public List<Abstractions.Activity> RecentActivity { get; set; } = new();
        public List<SocialPost> Posts { get; set; } = new();
        public List<string> Friends { get; set; } = new();

        public IndexModel(DatabaseService db)
        {
            _db = db;
        }

        public async Task OnGet(string? username)
        {
            // If no username provided, try to get the authenticated user
            if (string.IsNullOrEmpty(username))
            {
                var userId = _db.GetAuthenticatedUserId(HttpContext);
                if (userId != null)
                {
                    var user = _db.GetUserById(userId.Value);
                    if (user != null)
                    {
                        username = user.Username;
                    }
                    else
                    {
                        username = "admin";
                    }
                }
                else
                {
                    username = "admin";
                }
            }

            // Fetch profile from database
            // First try to get user by username
            var dbUser = _db.GetAllUsers().FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (dbUser != null)
            {
                Profile = new Abstractions.UserProfile
                {
                    UserId = dbUser.Username,
                    DisplayName = dbUser.Username,
                    Bio = dbUser.Bio ?? "LegendaryCMS user",
                    CreatedAt = dbUser.CreatedAt,
                    LastActiveAt = DateTime.UtcNow, // Can be enhanced later
                    Role = dbUser.Role
                };
            }
            else
            {
                // User not found, show default profile
                Profile = new Abstractions.UserProfile
                {
                    UserId = username,
                    DisplayName = username,
                    Bio = "User not found",
                    CreatedAt = DateTime.MinValue,
                    LastActiveAt = DateTime.MinValue,
                    Role = "Unknown"
                };
            }

            // Fetch activity, posts, and friends - can be enhanced later with dedicated tables
            RecentActivity = new List<Abstractions.Activity>();
            Posts = new List<SocialPost>();
            Friends = new List<string>();

            await Task.CompletedTask;
        }
    }
}
