using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Blogs
{
    /// <summary>
    /// Page model for creating and editing blog posts with WYSIWYG editor
    /// </summary>
    public class CreateModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string PostContent { get; set; } = string.Empty;

        [BindProperty]
        public string Category { get; set; } = string.Empty;

        public bool IsEdit { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public CreateModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet(int? id)
        {
            // Check authentication
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            if (id.HasValue)
            {
                IsEdit = true;
                LoadPost(id.Value, userId.Value);
            }

            return Page();
        }

        public IActionResult OnPost(int? id, string? action)
        {
            // Check authentication
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(PostContent))
            {
                ErrorMessage = "Please fill in all required fields.";
                IsEdit = id.HasValue;
                return Page();
            }

            // Sanitize content to prevent XSS attacks
            var sanitizedContent = SanitizeHtml(PostContent);

            if (id.HasValue)
            {
                // Update existing post
                var success = UpdatePost(id ?? 0, Title, sanitizedContent, Category, userId.Value);

                if (success)
                {
                    SuccessMessage = "Blog post updated successfully!";
                    return RedirectToPage("./Post", new { id = id ?? 0 });
                }
                else
                {
                    ErrorMessage = "Failed to update blog post. Please try again.";
                    IsEdit = true;
                    return Page();
                }
            }
            else
            {
                // Create new post
                var isDraft = action == "draft";
                var postId = CreatePost(Title, sanitizedContent, Category, isDraft, userId.Value);

                if (postId > 0)
                {
                    SuccessMessage = isDraft ? "Blog post saved as draft!" : "Blog post published successfully!";
                    return RedirectToPage("./Post", new { id = postId });
                }
                else
                {
                    ErrorMessage = "Failed to create blog post. Please try again.";
                    return Page();
                }
            }
        }

        private void LoadPost(int id, int userId)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT Title, Content, CategoryId
                FROM BlogPosts
                WHERE Id = @id AND (AuthorId = @userId OR @userId IN (SELECT Id FROM Users WHERE Role = 'Admin'))
            ";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@userId", userId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    Title = reader.GetString(0);
                    PostContent = reader.GetString(1);
                    Category = reader.IsDBNull(2) ? "" : reader.GetInt32(2).ToString();
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private bool UpdatePost(int id, string title, string content, string category, int userId)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE BlogPosts
                SET Title = @title, Content = @content, UpdatedAt = @updatedAt,
                    Excerpt = @excerpt
                WHERE Id = @id AND (AuthorId = @userId OR @userId IN (SELECT Id FROM Users WHERE Role = 'Admin'))
            ";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@content", content);
                command.Parameters.AddWithValue("@excerpt", content.Length > 200 ? content.Substring(0, 200) + "..." : content);
                command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@userId", userId);

                return command.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        private int CreatePost(string title, string content, string category, bool isDraft, int userId)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                INSERT INTO BlogPosts (Title, Slug, Content, Excerpt, AuthorId, CreatedAt, UpdatedAt, Published)
                VALUES (@title, @slug, @content, @excerpt, @authorId, @createdAt, @updatedAt, @published);
                SELECT last_insert_rowid();
            ";

                var slug = title.ToLower().Replace(" ", "-").Replace("'", "");
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@slug", slug);
                command.Parameters.AddWithValue("@content", content);
                command.Parameters.AddWithValue("@excerpt", content.Length > 200 ? content.Substring(0, 200) + "..." : content);
                command.Parameters.AddWithValue("@authorId", userId);
                command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@published", isDraft ? 0 : 1);

                return Convert.ToInt32(command.ExecuteScalar());
            }
            catch
            {
                return 0;
            }
        }

        private string SanitizeHtml(string html)
        {
            // Basic sanitization - in production, use HtmlSanitizer NuGet package
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Remove potentially dangerous tags and attributes
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
