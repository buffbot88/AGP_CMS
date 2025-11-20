using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Forums
{
    /// <summary>
    /// Razor Page Model for Forum Thread List View
    /// </summary>
    public class ViewModel : PageModel
    {
        private readonly DatabaseService _db;

        public int ForumId { get; set; }
        public string ForumTitle { get; set; } = string.Empty;
        public string ForumDescription { get; set; } = string.Empty;
        public List<ThreadInfo> Threads { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public ViewModel(DatabaseService db)
        {
            _db = db;
        }

        public void OnGet(int id, int page = 1)
        {
            ForumId = id;
            CurrentPage = page;

            // Load forum details and threads
            LoadForumDetails(id);
            LoadThreads(id, page);
        }

        private void LoadForumDetails(int forumId)
        {
            var forum = _db.GetForumById(forumId);
            if (forum != null)
            {
                ForumTitle = forum.Name;
                ForumDescription = forum.Description;
            }
            else
            {
                ForumTitle = "Unknown Forum";
                ForumDescription = "Forum not found";
            }
        }

        private void LoadThreads(int forumId, int page)
        {
            var dbThreads = _db.GetForumThreads(forumId);
            Threads = dbThreads.Select(t => new ThreadInfo
            {
                Id = t.Id,
                Title = t.Title,
                Author = t.Username,
                CreatedDate = t.CreatedAt,
                ReplyCount = t.PostCount - 1, // Subtract first post
                ViewCount = t.ViewCount,
                LastPostAuthor = t.Username, // Can be enhanced to get actual last post author
                LastPostDate = t.UpdatedAt,
                IsSticky = t.IsSticky,
                IsLocked = t.IsLocked,
                Prefix = string.Empty // Can be enhanced later
            }).ToList();

            // Simple pagination (can be enhanced later)
            TotalPages = (int)Math.Ceiling(Threads.Count / 20.0);
        }
    }

    public class ThreadInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int ReplyCount { get; set; }
        public int ViewCount { get; set; }
        public string LastPostAuthor { get; set; } = string.Empty;
        public DateTime LastPostDate { get; set; }
        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }
        public string Prefix { get; set; } = string.Empty;
    }
}
