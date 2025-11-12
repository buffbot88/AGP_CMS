using Abstractions;
using LegendaryChat;
using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Forums
{
    /// <summary>
    /// Razor Page Model for Moderation Panel (Forums, Blogs, Chat)
    /// </summary>
    public class ModerateModel : PageModel
    {
        private readonly DatabaseService _db;
        private readonly IChatModule? _chatModule;

        // Forum stats
        public int TotalThreads { get; set; }
        public int TotalPosts { get; set; }
        public int DeletedPostsCount { get; set; }
        public int LockedThreadsCount { get; set; }
        public int BannedUsers { get; set; }
        public int ActiveWarnings { get; set; }

        // Blog stats
        public int FlaggedBlogCommentsCount { get; set; }
        public int DeletedBlogCommentsCount { get; set; }
        public int FlaggedBlogPostsCount { get; set; }

        // Chat stats
        public int TotalChatRooms { get; set; }
        public int TotalChatMessages { get; set; }
        public int MutedChatUsers { get; set; }
        public int ChatRoomBans { get; set; }
        public int FlaggedChatMessages { get; set; }

        // Forum moderation lists
        public List<FlaggedPostInfo> FlaggedPosts { get; set; } = new();
        public List<LockedThreadInfo> LockedThreadsList { get; set; } = new();
        public List<BannedUserInfo> BannedUsersList { get; set; } = new();
        public List<DeletedPostInfo> DeletedPostsList { get; set; } = new();

        // Blog moderation lists
        public List<FlaggedBlogCommentInfo> FlaggedBlogComments { get; set; } = new();
        public List<FlaggedBlogPostInfo> FlaggedBlogPosts { get; set; } = new();
        public List<DeletedBlogCommentInfo> DeletedBlogComments { get; set; } = new();

        // Chat moderation lists
        public List<MutedUser> MutedUsers { get; set; } = new();
        public List<RoomBan> RoomBans { get; set; } = new();
        public List<FlaggedMessage> FlaggedMessages { get; set; } = new();

        public ModerateModel(DatabaseService db, IChatModule? chatModule = null)
        {
            _db = db;
            _chatModule = chatModule;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/forums/moderate" });
            }

            // Check if user has admin or moderator permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to access the moderation panel.";
                return RedirectToPage("/Index");
            }

            // Load moderation statistics
            LoadStatistics();

            // Load forum moderation queues
            LoadFlaggedPosts();
            LoadLockedThreads();
            LoadBannedUsers();
            LoadDeletedPosts();

            // Load blog moderation queues
            LoadFlaggedBlogComments();
            LoadFlaggedBlogPosts();
            LoadDeletedBlogComments();

            // Load chat moderation queues
            LoadChatModeration();

            return Page();
        }

        // Forum moderation handlers
        public IActionResult OnPostResolveFlag(int flagId)
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null || !_db.IsUserAdmin(userId.Value))
            {
                return RedirectToPage("/Login");
            }

            if (_db.ResolveFlaggedPost(flagId, userId.Value))
            {
                TempData["Success"] = "Flagged post resolved successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to resolve flagged post.";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUnlockThread(int threadId)
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null || !_db.IsUserAdmin(userId.Value))
            {
                return RedirectToPage("/Login");
            }

            if (_db.UnlockThread(threadId, userId.Value))
            {
                TempData["Success"] = "Thread unlocked successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to unlock thread.";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUnbanUser(int userId)
        {
            var moderatorId = _db.GetAuthenticatedUserId(HttpContext);
            if (moderatorId == null || !_db.IsUserAdmin(moderatorId.Value))
            {
                return RedirectToPage("/Login");
            }

            if (_db.UnbanUser(userId))
            {
                TempData["Success"] = "User unbanned successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to unban user.";
            }

            return RedirectToPage();
        }

        // Blog moderation handlers
        public IActionResult OnPostResolveBlogCommentFlag(int flagId)
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null || !_db.IsUserAdmin(userId.Value))
            {
                return RedirectToPage("/Login");
            }

            if (_db.ResolveFlaggedBlogComment(flagId, userId.Value))
            {
                TempData["Success"] = "Flagged blog comment resolved successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to resolve flagged blog comment.";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostResolveBlogPostFlag(int flagId)
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null || !_db.IsUserAdmin(userId.Value))
            {
                return RedirectToPage("/Login");
            }

            if (_db.ResolveFlaggedBlogPost(flagId, userId.Value))
            {
                TempData["Success"] = "Flagged blog post resolved successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to resolve flagged blog post.";
            }

            return RedirectToPage();
        }

        private void LoadStatistics()
        {
            var stats = _db.GetModerationStats();
            TotalThreads = stats.TotalThreads;
            TotalPosts = stats.TotalPosts;
            DeletedPostsCount = stats.DeletedPostsCount;
            LockedThreadsCount = stats.LockedThreadsCount;
            BannedUsers = stats.BannedUsers;
            ActiveWarnings = stats.ActiveWarnings;
            FlaggedBlogCommentsCount = stats.FlaggedBlogComments;
            DeletedBlogCommentsCount = stats.DeletedBlogComments;
            FlaggedBlogPostsCount = stats.FlaggedBlogPosts;
        }

        private void LoadFlaggedPosts()
        {
            FlaggedPosts = _db.GetFlaggedPosts();
        }

        private void LoadLockedThreads()
        {
            LockedThreadsList = _db.GetLockedThreads();
        }

        private void LoadBannedUsers()
        {
            BannedUsersList = _db.GetBannedUsers();
        }

        private void LoadDeletedPosts()
        {
            DeletedPostsList = _db.GetDeletedPosts(50);
        }

        private void LoadFlaggedBlogComments()
        {
            FlaggedBlogComments = _db.GetFlaggedBlogComments();
        }

        private void LoadFlaggedBlogPosts()
        {
            FlaggedBlogPosts = _db.GetFlaggedBlogPosts();
        }

        private void LoadDeletedBlogComments()
        {
            DeletedBlogComments = _db.GetDeletedBlogComments(50);
        }

        private void LoadChatModeration()
        {
            if (_chatModule is ChatModule chatModule)
            {
                var stats = chatModule.GetModerationStats();
                TotalChatRooms = stats.TotalRooms;
                TotalChatMessages = stats.TotalMessages;
                MutedChatUsers = stats.MutedUsers;
                ChatRoomBans = stats.RoomBans;
                FlaggedChatMessages = stats.FlaggedMessages;

                MutedUsers = chatModule.GetMutedUsers();
                RoomBans = chatModule.GetRoomBans();
                FlaggedMessages = chatModule.GetFlaggedMessages();
            }
        }

        // Chat moderation handlers
        public async Task<IActionResult> OnPostUnmuteUserAsync(string userId, string roomId)
        {
            var moderatorId = _db.GetAuthenticatedUserId(HttpContext);
            if (moderatorId == null || !_db.IsUserAdmin(moderatorId.Value))
            {
                return RedirectToPage("/Login");
            }

            if (_chatModule is ChatModule chatModule)
            {
                if (await chatModule.UnmuteUserAsync(userId, roomId))
                {
                    TempData["Success"] = "User unmuted successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to unmute user.";
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnbanFromRoomAsync(string userId, string roomId)
        {
            var moderatorId = _db.GetAuthenticatedUserId(HttpContext);
            if (moderatorId == null || !_db.IsUserAdmin(moderatorId.Value))
            {
                return RedirectToPage("/Login");
            }

            if (_chatModule is ChatModule chatModule)
            {
                if (await chatModule.UnbanUserFromRoomAsync(userId, roomId))
                {
                    TempData["Success"] = "User unbanned from room successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to unban user from room.";
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResolveFlaggedMessageAsync(string flagId)
        {
            var moderatorId = _db.GetAuthenticatedUserId(HttpContext);
            if (moderatorId == null || !_db.IsUserAdmin(moderatorId.Value))
            {
                return RedirectToPage("/Login");
            }

            if (_chatModule is ChatModule chatModule)
            {
                if (await chatModule.ResolveFlaggedMessageAsync(flagId, moderatorId.Value.ToString()))
                {
                    TempData["Success"] = "Flagged message resolved successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to resolve flagged message.";
                }
            }

            return RedirectToPage();
        }
    }
}
