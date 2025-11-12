using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Forums
{
    /// <summary>
    /// Page model for replying to forum threads with WYSIWYG editor
    /// </summary>
    public class ReplyModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public string PostContent { get; set; } = string.Empty;

        [BindProperty]
        public int ThreadId { get; set; }

        [BindProperty]
        public int? QuotePostId { get; set; }

        public string ThreadTitle { get; set; } = string.Empty;
        public string ForumName { get; set; } = string.Empty;
        public string QuoteAuthor { get; set; } = string.Empty;
        public string QuoteContent { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public ReplyModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet(int threadId, int? quotePostId)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = $"/cms/forums/reply?threadId={threadId}" });
            }

            ThreadId = threadId;
            QuotePostId = quotePostId;

            // Load thread information
            LoadThreadInfo(threadId);

            // If quoting a post, load the quoted content
            if (quotePostId.HasValue)
            {
                LoadQuotedPost(quotePostId.Value);
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = $"/cms/forums/reply?threadId={ThreadId}" });
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(PostContent) || ThreadId <= 0)
            {
                ErrorMessage = "Please enter your reply content.";
                LoadThreadInfo(ThreadId);
                if (QuotePostId.HasValue)
                {
                    LoadQuotedPost(QuotePostId.Value);
                }
                return Page();
            }

            // Sanitize content to prevent XSS attacks
            var sanitizedContent = SanitizeHtml(PostContent);

            // If this is a quote, prepend the quoted content
            if (QuotePostId.HasValue && !string.IsNullOrEmpty(QuoteAuthor))
            {
                sanitizedContent = $"<blockquote><strong>{QuoteAuthor} wrote:</strong><br/>{QuoteContent}</blockquote><br/>{sanitizedContent}";
            }

            try
            {
                // Create reply in database
                var postId = _db.CreatePost(ThreadId, userId.Value, sanitizedContent);

                if (postId > 0)
                {
                    SuccessMessage = "Reply posted successfully!";
                    return RedirectToPage("./Thread", new { id = ThreadId });
                }
                else
                {
                    ErrorMessage = "Failed to post reply. Please try again.";
                    LoadThreadInfo(ThreadId);
                    if (QuotePostId.HasValue)
                    {
                        LoadQuotedPost(QuotePostId.Value);
                    }
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error posting reply: {ex.Message}";
                LoadThreadInfo(ThreadId);
                if (QuotePostId.HasValue)
                {
                    LoadQuotedPost(QuotePostId.Value);
                }
                return Page();
            }
        }

        private void LoadThreadInfo(int threadId)
        {
            var thread = _db.GetForumThreadById(threadId);
            if (thread != null)
            {
                ThreadTitle = thread.Title;
                ForumName = thread.ForumName;
            }
            else
            {
                ThreadTitle = "Unknown Thread";
                ForumName = "Unknown Forum";
            }
        }

        private void LoadQuotedPost(int postId)
        {
            var post = _db.GetPostById(postId);
            if (post != null)
            {
                QuoteAuthor = post.Username;
                QuoteContent = post.Content;
            }
            else
            {
                QuoteAuthor = "Unknown User";
                QuoteContent = "Post not found";
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
