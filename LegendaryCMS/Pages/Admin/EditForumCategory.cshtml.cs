using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Admin
{
    /// <summary>
    /// Edit Forum Category - Manage category details and forums
    /// </summary>
    public class EditForumCategoryModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public int CategoryId { get; set; }

        [BindProperty]
        public string CategoryName { get; set; } = string.Empty;

        [BindProperty]
        public string CategoryDescription { get; set; } = string.Empty;

        [BindProperty]
        public string CategoryIcon { get; set; } = "💬";

        [BindProperty]
        public bool IsPrivate { get; set; }

        public List<ForumInfo> Forums { get; set; } = new();

        public EditForumCategoryModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet(int id)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = $"/cms/admin/forums/edit?id={id}" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to edit forum categories.";
                return RedirectToPage("/Index");
            }

            CategoryId = id;
            LoadCategory();
            LoadForums();

            return Page();
        }

        public IActionResult OnPostUpdateCategory()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to edit forum categories.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                TempData["Error"] = "Category name is required";
                LoadForums();
                return Page();
            }

            try
            {
                var success = _db.UpdateForumCategory(
                    CategoryId,
                    CategoryName,
                    CategoryDescription,
                    CategoryIcon,
                    IsPrivate
                );

                if (success)
                {
                    TempData["Success"] = "Category updated successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to update category";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating category: {ex.Message}";
            }

            LoadForums();
            return Page();
        }

        public IActionResult OnPostCreateForum(string forumName, string forumDescription, string forumIcon)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create forums.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrWhiteSpace(forumName))
            {
                TempData["Error"] = "Forum name is required";
                LoadCategory();
                LoadForums();
                return Page();
            }

            try
            {
                var forumId = _db.CreateForum(
                    CategoryId,
                    forumName,
                    forumDescription ?? string.Empty,
                    forumIcon ?? "💬"
                );

                if (forumId > 0)
                {
                    TempData["Success"] = $"Forum '{forumName}' created successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to create forum";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating forum: {ex.Message}";
            }

            LoadCategory();
            LoadForums();
            return Page();
        }

        public IActionResult OnPostDeleteForum(int forumId)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to delete forums.";
                return RedirectToPage("/Index");
            }

            try
            {
                var success = _db.DeleteForum(forumId);

                if (success)
                {
                    TempData["Success"] = "Forum deleted successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to delete forum";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting forum: {ex.Message}";
            }

            LoadCategory();
            LoadForums();
            return Page();
        }

        public IActionResult OnPostDeleteCategory()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to delete forum categories.";
                return RedirectToPage("/Index");
            }

            try
            {
                var success = _db.DeleteForumCategory(CategoryId);

                if (success)
                {
                    TempData["Success"] = "Category deleted successfully";
                    return RedirectToPage("/Admin/Forums");
                }
                else
                {
                    TempData["Error"] = "Failed to delete category";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting category: {ex.Message}";
            }

            LoadCategory();
            LoadForums();
            return Page();
        }

        private void LoadCategory()
        {
            var category = _db.GetForumCategoryById(CategoryId);
            if (category != null)
            {
                CategoryName = category.Name;
                CategoryDescription = category.Description;
                CategoryIcon = category.Icon;
                IsPrivate = category.IsPrivate;
            }
        }

        private void LoadForums()
        {
            Forums = _db.GetForumsByCategory(CategoryId);
        }
    }
}
