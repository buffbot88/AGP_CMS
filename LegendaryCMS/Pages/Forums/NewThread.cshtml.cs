using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Forums
{
    /// <summary>
    /// Alternate route for creating forum threads (newthread alias for createthread)
    /// </summary>
    public class NewThreadModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string PostContent { get; set; } = string.Empty;

        [BindProperty]
        public int ForumId { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public List<Services.ForumInfo> AvailableForums { get; set; } = new();

        public NewThreadModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet(int? forumId)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/forums/newthread" });
            }

            if (forumId.HasValue)
            {
                ForumId = forumId.Value;
            }

            LoadForums();
            return Page();
        }

        public IActionResult OnPost()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/forums/newthread" });
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(PostContent) || ForumId <= 0)
            {
                ErrorMessage = "Please fill in all required fields and select a forum.";
                LoadForums();
                return Page();
            }

            // Sanitize content to prevent XSS attacks
            var sanitizedContent = SanitizeHtml(PostContent);

            try
            {
                // Create new thread in database
                var threadId = _db.CreateThread(ForumId, userId.Value, Title, sanitizedContent);

                if (threadId > 0)
                {
                    SuccessMessage = "Thread created successfully!";
                    return RedirectToPage("./Thread", new { id = threadId });
                }
                else
                {
                    ErrorMessage = "Failed to create thread. Please try again.";
                    LoadForums();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating thread: {ex.Message}";
                LoadForums();
                return Page();
            }
        }

        private void LoadForums()
        {
            var categories = _db.GetForumCategories();
            foreach (var category in categories)
            {
                var forums = _db.GetForumsByCategory(category.Id);
                AvailableForums.AddRange(forums);
            }
        }

        private string SanitizeHtml(string html)
        {
            // Basic sanitization - in production, use HtmlSanitizer NuGet package
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var dangerous = new[] { "<script", "<iframe", "javascript:", "onerror=", "onload=" };
            var result = html;

            foreach (var danger in dangerous)
            {
                result = result.Replace(danger, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }
    }
}
