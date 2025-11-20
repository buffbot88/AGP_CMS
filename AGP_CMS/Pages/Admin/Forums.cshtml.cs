using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// Forum Management - Create and manage forum categories, assign moderators
    /// </summary>
    public class ForumsModel : PageModel
    {
        private readonly DatabaseService _db;

        public List<ForumCategoryInfo> Categories { get; set; } = new();
        public List<ModeratorInfo> Moderators { get; set; } = new();

        [BindProperty]
        public string? CategoryName { get; set; }

        [BindProperty]
        public string? CategoryDescription { get; set; }

        [BindProperty]
        public bool IsPrivate { get; set; }

        [BindProperty]
        public string? CategoryIcon { get; set; }

        public ForumsModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/forums" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to manage forums.";
                return RedirectToPage("/Index");
            }

            LoadCategories();
            LoadModerators();
            return Page();
        }

        public IActionResult OnPostCreateCategory()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/forums" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create forum categories.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                TempData["Error"] = "Category name is required";
                return RedirectToPage();
            }

            try
            {
                var categoryId = _db.CreateForumCategory(
                    CategoryName,
                    CategoryDescription ?? string.Empty,
                    CategoryIcon ?? "💬",
                    IsPrivate
                );

                if (categoryId > 0)
                {
                    TempData["Success"] = $"Forum category '{CategoryName}' created successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to create forum category";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating category: {ex.Message}";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostAssignModerator(int categoryId, int userId)
        {
            // Check if user is logged in
            var authenticatedUserId = _db.GetAuthenticatedUserId(HttpContext);
            if (authenticatedUserId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/forums" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(authenticatedUserId.Value))
            {
                TempData["Error"] = "You do not have permission to assign moderators.";
                return RedirectToPage("/Index");
            }

            if (_db.AssignModerator(userId, categoryId))
            {
                TempData["Success"] = "Moderator assigned successfully";
            }
            else
            {
                TempData["Error"] = "Failed to assign moderator";
            }

            return RedirectToPage();
        }

        private void LoadCategories()
        {
            var dbCategories = _db.GetForumCategories();
            Categories = dbCategories.Select(c => new ForumCategoryInfo
            {
                CategoryId = c.Id,
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon,
                ForumCount = c.ForumCount,
                ThreadCount = c.ThreadCount,
                PostCount = c.PostCount,
                IsPrivate = c.IsPrivate,
                Moderators = _db.GetModeratorsForCategory(c.Id).Select(m => m.Username).ToList()
            }).ToList();
        }

        private void LoadModerators()
        {
            var moderatorAssignments = _db.GetAllModerators();

            // Group by user to show all their assignments
            var grouped = moderatorAssignments.GroupBy(m => new { m.UserId, m.Username });

            Moderators = grouped.Select(g => new ModeratorInfo
            {
                UserId = g.Key.UserId.ToString(),
                Username = g.Key.Username,
                AssignedForums = g.Select(m => m.CategoryName).ToList()
            }).ToList();
        }
    }

    public class ForumCategoryInfo
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "💬";
        public int ForumCount { get; set; }
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }
        public bool IsPrivate { get; set; }
        public List<string> AllowedRoles { get; set; } = new();
        public List<string> Moderators { get; set; } = new();
    }

    public class ModeratorInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public List<string> AssignedForums { get; set; } = new();
    }
}
