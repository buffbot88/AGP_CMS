using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Admin
{
    /// <summary>
    /// Admin page for creating and editing blog posts
    /// </summary>
    public class CreateBlogModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string PostContent { get; set; } = string.Empty;

        [BindProperty]
        public string? Category { get; set; }

        public bool IsEdit { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public List<BlogCategoryInfo> Categories { get; set; } = new();

        public CreateBlogModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet(int? id)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/blogs/create" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create blog posts.";
                return RedirectToPage("/Index");
            }

            LoadCategories();

            if (id.HasValue)
            {
                IsEdit = true;
                LoadPost(id.Value);
            }

            return Page();
        }

        public IActionResult OnPost(int? id, string? action)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/blogs/create" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create blog posts.";
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(PostContent))
            {
                ErrorMessage = "Please fill in all required fields.";
                IsEdit = id.HasValue;
                LoadCategories();
                return Page();
            }

            // Sanitize content to prevent XSS attacks
            var sanitizedContent = SanitizeHtml(PostContent);

            if (id.HasValue)
            {
                // Update existing post
                var success = UpdatePost(id.Value, Title, sanitizedContent, Category ?? string.Empty);

                if (success)
                {
                    TempData["Success"] = "Blog post updated successfully!";
                    return RedirectToPage("./Blogs");
                }
                else
                {
                    ErrorMessage = "Failed to update blog post. Please try again.";
                    IsEdit = true;
                    LoadCategories();
                    return Page();
                }
            }
            else
            {
                // Create new post
                var isDraft = action == "draft";
                var postId = CreatePost(Title, sanitizedContent, Category ?? string.Empty, isDraft, userId.Value);

                if (postId > 0)
                {
                    TempData["Success"] = isDraft ? "Blog post saved as draft!" : "Blog post published successfully!";
                    return RedirectToPage("./Blogs");
                }
                else
                {
                    ErrorMessage = "Failed to create blog post. Please try again.";
                    LoadCategories();
                    return Page();
                }
            }
        }

        private void LoadCategories()
        {
            var dbCategories = _db.GetBlogCategories();
            Categories = dbCategories.Select(c => new BlogCategoryInfo
            {
                CategoryId = c.Id,
                Name = c.Name,
                Description = c.Description,
                PostCount = c.PostCount
            }).ToList();
        }

        private void LoadPost(int id)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Title, Content, CategoryId
                    FROM BlogPosts
                    WHERE Id = @id
                ";
                command.Parameters.AddWithValue("@id", id);

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

        private bool UpdatePost(int id, string title, string content, string category)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                
                var categoryId = string.IsNullOrEmpty(category) ? (int?)null : int.Parse(category);
                
                command.CommandText = @"
                    UPDATE BlogPosts
                    SET Title = @title, Content = @content, UpdatedAt = @updatedAt,
                        Excerpt = @excerpt, CategoryId = @categoryId
                    WHERE Id = @id
                ";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@content", content);
                command.Parameters.AddWithValue("@excerpt", content.Length > 200 ? content.Substring(0, 200) + "..." : content);
                command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@categoryId", categoryId.HasValue ? (object)categoryId.Value : DBNull.Value);

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
                
                var categoryId = string.IsNullOrEmpty(category) ? (int?)null : int.Parse(category);
                
                command.CommandText = @"
                    INSERT INTO BlogPosts (Title, Slug, Content, Excerpt, AuthorId, CategoryId, CreatedAt, UpdatedAt, Published)
                    VALUES (@title, @slug, @content, @excerpt, @authorId, @categoryId, @createdAt, @updatedAt, @published);
                    SELECT last_insert_rowid();
                ";

                var slug = title.ToLower().Replace(" ", "-").Replace("'", "");
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@slug", slug);
                command.Parameters.AddWithValue("@content", content);
                command.Parameters.AddWithValue("@excerpt", content.Length > 200 ? content.Substring(0, 200) + "..." : content);
                command.Parameters.AddWithValue("@authorId", userId);
                command.Parameters.AddWithValue("@categoryId", categoryId.HasValue ? (object)categoryId.Value : DBNull.Value);
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
