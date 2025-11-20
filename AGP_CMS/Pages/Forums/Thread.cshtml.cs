using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Forums
{
    /// <summary>
    /// Razor Page Model for Thread View with Posts and Quick Reply
    /// </summary>
    public class ThreadModel : PageModel
    {
        private readonly DatabaseService _db;

        public int ThreadId { get; set; }
        public int ForumId { get; set; }
        public string ForumName { get; set; } = string.Empty;
        public string ThreadTitle { get; set; } = string.Empty;
        public string ThreadAuthor { get; set; } = string.Empty;
        public DateTime ThreadCreatedDate { get; set; }
        public int ViewCount { get; set; }
        public int ReplyCount { get; set; }
        public List<PostInfo> Posts { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        [BindProperty]
        public string ReplyContent { get; set; } = string.Empty;

        public ThreadModel(DatabaseService db)
        {
            _db = db;
        }

        public void OnGet(int id, int page = 1)
        {
            ThreadId = id;
            CurrentPage = page;

            // Load thread details and posts
            LoadThreadDetails(id);
            LoadPosts(id, page);
        }

        public IActionResult OnPost(int id, int page = 1)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = $"/cms/forums/thread?id={id}" });
            }

            // Validate content
            if (string.IsNullOrWhiteSpace(ReplyContent))
            {
                // Reload page with error
                ThreadId = id;
                CurrentPage = page;
                LoadThreadDetails(id);
                LoadPosts(id, page);
                return Page();
            }

            // Sanitize content to prevent XSS attacks
            var sanitizedContent = SanitizeHtml(ReplyContent);

            try
            {
                // Create reply in database
                var postId = _db.CreatePost(id, userId.Value, sanitizedContent);

                if (postId > 0)
                {
                    // Redirect back to thread to show the new post
                    return RedirectToPage("/Forums/Thread", new { id = id, page = page });
                }
                else
                {
                    // Reload page with error
                    ThreadId = id;
                    CurrentPage = page;
                    LoadThreadDetails(id);
                    LoadPosts(id, page);
                    return Page();
                }
            }
            catch
            {
                // Reload page with error
                ThreadId = id;
                CurrentPage = page;
                LoadThreadDetails(id);
                LoadPosts(id, page);
                return Page();
            }
        }

        private void LoadThreadDetails(int threadId)
        {
            var thread = _db.GetForumThreadById(threadId);
            if (thread != null)
            {
                ThreadTitle = thread.Title;
                ThreadAuthor = thread.Username;
                ThreadCreatedDate = thread.CreatedAt;
                ViewCount = thread.ViewCount;
                ReplyCount = thread.PostCount - 1; // Subtract first post
                ForumId = thread.ForumId;
                ForumName = thread.ForumName;
            }
            else
            {
                ThreadTitle = "Thread Not Found";
                ThreadAuthor = "Unknown";
                ThreadCreatedDate = DateTime.MinValue;
                ViewCount = 0;
                ReplyCount = 0;
                ForumId = 0;
                ForumName = "Unknown";
            }
        }

        private void LoadPosts(int threadId, int page)
        {
            var dbPosts = _db.GetThreadPosts(threadId);
            Posts = dbPosts.Select(p => new PostInfo
            {
                Id = p.Id,
                Author = p.Username,
                PostDate = p.CreatedAt,
                Content = p.Content,
                UserTitle = p.UserTitle,
                PostCount = 0, // Can be enhanced later
                JoinDate = DateTime.MinValue, // Can be enhanced later
                Location = string.Empty // Can be enhanced later
            }).ToList();

            // Simple pagination (can be enhanced later)
            TotalPages = (int)Math.Ceiling(Posts.Count / 20.0);
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

    public class PostInfo
    {
        public int Id { get; set; }
        public string Author { get; set; } = string.Empty;
        public DateTime PostDate { get; set; }
        public string Content { get; set; } = string.Empty;
        public string UserTitle { get; set; } = string.Empty;
        public int PostCount { get; set; }
        public DateTime JoinDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public bool IsEditable { get; set; }
    }
}
