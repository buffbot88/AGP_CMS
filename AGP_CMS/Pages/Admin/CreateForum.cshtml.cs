using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// Admin page for creating forum categories
    /// </summary>
    public class CreateForumModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public string CategoryName { get; set; } = string.Empty;

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        public bool IsPrivate { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public CreateForumModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/forums/create" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create forum categories.";
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/forums/create" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create forum categories.";
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(CategoryName))
            {
                ErrorMessage = "Please provide a category name.";
                return Page();
            }

            // Create the forum category
            if (CreateCategory(CategoryName, Description, IsPrivate))
            {
                TempData["Success"] = $"Forum category '{CategoryName}' created successfully!";
                return RedirectToPage("/cms/admin/forums");
            }
            else
            {
                ErrorMessage = "Failed to create forum category. Please try again.";
                return Page();
            }
        }

        private bool CreateCategory(string name, string description, bool isPrivate)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO ForumCategories (Name, Description, IsPrivate, CreatedAt)
                    VALUES (@name, @description, @isPrivate, @createdAt)
                ";
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@description", description ?? string.Empty);
                command.Parameters.AddWithValue("@isPrivate", isPrivate ? 1 : 0);
                command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));

                return command.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
