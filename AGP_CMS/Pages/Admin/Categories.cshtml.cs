using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// Category Management - Manage categories across all content types
    /// </summary>
    public class CategoriesModel : PageModel
    {
        private readonly DatabaseService _db;

        public List<CategoryInfo> ForumCategories { get; set; } = new();
        public List<CategoryInfo> BlogCategories { get; set; } = new();
        public List<CategoryInfo> DownloadCategories { get; set; } = new();

        [BindProperty]
        public string? CategoryType { get; set; }

        [BindProperty]
        public string? Name { get; set; }

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public bool IsPrivate { get; set; }

        [BindProperty]
        public string? AllowedRoles { get; set; }

        public CategoriesModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/categories" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to manage categories.";
                return RedirectToPage("/Index");
            }

            LoadCategories();
            return Page();
        }

        public IActionResult OnPostCreate()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/categories" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create categories.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(CategoryType))
            {
                TempData["Error"] = "Name and Type are required";
                return RedirectToPage();
            }

            try
            {
                switch (CategoryType?.ToLower())
                {
                    case "forum":
                        _db.CreateForumCategory(Name, Description ?? string.Empty, "💬", IsPrivate);
                        break;
                    case "blog":
                        _db.CreateBlogCategory(Name, Description ?? string.Empty);
                        break;
                    case "download":
                        // We don't have a method for this yet, but we'll create one
                        TempData["Error"] = "Download categories not yet implemented";
                        return RedirectToPage();
                    default:
                        TempData["Error"] = "Invalid category type";
                        return RedirectToPage();
                }

                TempData["Success"] = $"{CategoryType} category '{Name}' created successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to create category: {ex.Message}";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostTogglePrivacy(int categoryId, string type)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/categories" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to modify categories.";
                return RedirectToPage("/Index");
            }

            try
            {
                // For now, only forum categories support privacy toggle
                if (type?.ToLower() == "forum")
                {
                    var category = _db.GetForumCategoryById(categoryId);
                    if (category != null)
                    {
                        _db.UpdateForumCategory(categoryId, category.Name, category.Description,
                            category.Icon, !category.IsPrivate);
                        TempData["Success"] = "Category privacy updated";
                    }
                    else
                    {
                        TempData["Error"] = "Category not found";
                    }
                }
                else
                {
                    TempData["Error"] = "Privacy toggle not supported for this category type";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to update category: {ex.Message}";
            }

            return RedirectToPage();
        }

        private void LoadCategories()
        {
            // Load Forum Categories
            var forumCats = _db.GetForumCategories();
            ForumCategories = forumCats.Select(c => new CategoryInfo
            {
                CategoryId = c.Id,
                Name = c.Name,
                Description = c.Description,
                Type = "Forum",
                IsPrivate = c.IsPrivate,
                ItemCount = c.ForumCount,
                AllowedRoles = new List<string>(),
                CreatedAt = DateTime.Now // Can be enhanced later
            }).ToList();

            // Load Blog Categories
            var blogCats = _db.GetBlogCategories();
            BlogCategories = blogCats.Select(c => new CategoryInfo
            {
                CategoryId = c.Id,
                Name = c.Name,
                Description = c.Description,
                Type = "Blog",
                IsPrivate = false,
                ItemCount = c.PostCount,
                AllowedRoles = new List<string>(),
                CreatedAt = DateTime.Now // Can be enhanced later
            }).ToList();

            // Load Download Categories
            var downloadCats = _db.GetDownloadCategories();
            DownloadCategories = downloadCats.Select(c => new CategoryInfo
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                Type = "Download",
                IsPrivate = c.IsPrivate,
                ItemCount = c.FileCount,
                AllowedRoles = new List<string>(),
                CreatedAt = DateTime.Now // Can be enhanced later
            }).ToList();
        }
    }

    public class CategoryInfo
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Forum, Blog, Download
        public bool IsPrivate { get; set; }
        public int ItemCount { get; set; }
        public List<string> AllowedRoles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
