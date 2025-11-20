using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// Blog Management - Manage blog posts, categories, and moderators
    /// </summary>
    public class BlogsModel : PageModel
    {
        private readonly DatabaseService _db;

        public List<BlogPostInfo> Posts { get; set; } = new();
        public List<BlogCategoryInfo> Categories { get; set; } = new();
        public List<ContentCreatorInfo> ContentCreators { get; set; } = new();

        [BindProperty]
        public string? CategoryName { get; set; }

        [BindProperty]
        public string? CategoryDescription { get; set; }

        [BindProperty]
        public bool IsPrivate { get; set; }

        public BlogsModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/blogs" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to manage blogs.";
                return RedirectToPage("/Index");
            }

            LoadPosts();
            LoadCategories();
            LoadContentCreators();
            return Page();
        }

        public IActionResult OnPostCreateCategory()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/blogs" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create blog categories.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                TempData["Error"] = "Category name is required";
                return RedirectToPage();
            }

            try
            {
                _db.CreateBlogCategory(CategoryName, CategoryDescription ?? string.Empty);
                TempData["Success"] = $"Blog category '{CategoryName}' created successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to create category: {ex.Message}";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostPublishPost(int postId)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/blogs" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to publish blog posts.";
                return RedirectToPage("/Index");
            }

            if (_db.UpdateBlogPostStatus(postId, true))
            {
                TempData["Success"] = "Post published successfully";
            }
            else
            {
                TempData["Error"] = "Failed to publish post";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUnpublishPost(int postId)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/blogs" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to unpublish blog posts.";
                return RedirectToPage("/Index");
            }

            if (_db.UpdateBlogPostStatus(postId, false))
            {
                TempData["Success"] = "Post unpublished successfully";
            }
            else
            {
                TempData["Error"] = "Failed to unpublish post";
            }

            return RedirectToPage();
        }

        private void LoadPosts()
        {
            var dbPosts = _db.GetAllBlogPosts();
            Posts = dbPosts.Select(p => new BlogPostInfo
            {
                PostId = p.Id,
                Title = p.Title,
                Author = p.AuthorName,
                Status = p.Published ? "Published" : "Draft",
                CreatedAt = p.CreatedAt,
                PublishedAt = p.Published ? p.UpdatedAt : null,
                ViewCount = p.ViewCount,
                CommentCount = 0, // Can be enhanced later
                CategoryName = p.CategoryName
            }).ToList();
        }

        private void LoadCategories()
        {
            var dbCategories = _db.GetBlogCategories();
            Categories = dbCategories.Select(c => new BlogCategoryInfo
            {
                CategoryId = c.Id,
                Name = c.Name,
                Description = c.Description,
                PostCount = c.PostCount,
                IsPrivate = false, // Can be enhanced later
                Moderators = new List<string>() // Can be enhanced later
            }).ToList();
        }

        private void LoadContentCreators()
        {
            var dbUsers = _db.GetAllUsers();
            ContentCreators = dbUsers.Select(u => new ContentCreatorInfo
            {
                UserId = u.Id.ToString(),
                Username = u.Username,
                PostCount = 0, // Can be enhanced later
                PublishedCount = 0, // Can be enhanced later
                DraftCount = 0 // Can be enhanced later
            }).ToList();
        }
    }

    public class BlogPostInfo
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft"; // Draft, Published
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class BlogCategoryInfo
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PostCount { get; set; }
        public bool IsPrivate { get; set; }
        public List<string> Moderators { get; set; } = new();
    }

    public class ContentCreatorInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int PostCount { get; set; }
        public int PublishedCount { get; set; }
        public int DraftCount { get; set; }
    }
}
