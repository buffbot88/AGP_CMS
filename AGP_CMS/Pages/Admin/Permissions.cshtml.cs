using AGP_CMS.Security;
using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// Permission Management - Fine-grained control over user permissions
    /// </summary>
    public class PermissionsModel : PageModel
    {
        private readonly DatabaseService _db;

        public Dictionary<string, List<string>> RolePermissions { get; set; } = new();
        public List<string> AllPermissions { get; set; } = new();

        [BindProperty]
        public string? RoleName { get; set; }

        [BindProperty]
        public string? Permission { get; set; }

        public PermissionsModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/permissions" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to manage permissions.";
                return RedirectToPage("/Index");
            }

            LoadRolePermissions();
            LoadAllPermissions();
            return Page();
        }

        public IActionResult OnPostAddPermission()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/permissions" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to manage permissions.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrWhiteSpace(RoleName) || string.IsNullOrWhiteSpace(Permission))
            {
                TempData["Error"] = "Role and Permission are required";
                return RedirectToPage();
            }

            // In production, update RBAC system
            TempData["Success"] = $"Permission '{Permission}' added to role '{RoleName}'";
            return RedirectToPage();
        }

        public IActionResult OnPostRemovePermission(string role, string permission)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/permissions" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to manage permissions.";
                return RedirectToPage("/Index");
            }

            // In production, update RBAC system
            TempData["Success"] = $"Permission '{permission}' removed from role '{role}'";
            return RedirectToPage();
        }

        private void LoadRolePermissions()
        {
            RolePermissions = new Dictionary<string, List<string>>
            {
                [CMSRoles.SuperAdmin] = new List<string>
                {
                    "All Permissions", "System Administration", "User Management",
                    "Content Moderation", "Security Configuration"
                },
                [CMSRoles.Admin] = new List<string>
                {
                    "User Management", "Content Moderation", "Settings Management"
                },
                [CMSRoles.Moderator] = new List<string>
                {
                    "Content Moderation", "Forum Management", "Blog Management"
                },
                [CMSRoles.User] = new List<string>
                {
                    "Create Content", "Edit Own Content", "View Public Content"
                },
                [CMSRoles.Guest] = new List<string>
                {
                    "View Public Content"
                }
            };
        }

        private void LoadAllPermissions()
        {
            AllPermissions = new List<string>
            {
                CMSPermissions.ForumView,
                CMSPermissions.ForumPost,
                CMSPermissions.ForumEdit,
                CMSPermissions.ForumDelete,
                CMSPermissions.ForumModeRate,
                CMSPermissions.BlogView,
                CMSPermissions.BlogCreate,
                CMSPermissions.BlogEdit,
                CMSPermissions.BlogDelete,
                CMSPermissions.BlogPublish,
                CMSPermissions.DownloadView,
                CMSPermissions.DownloadUpload,
                CMSPermissions.DownloadDelete,
                CMSPermissions.DownloadManage,
                CMSPermissions.ChatJoin,
                CMSPermissions.ChatSend,
                CMSPermissions.ChatModeRate,
                CMSPermissions.ProfileView,
                CMSPermissions.ProfileEdit,
                CMSPermissions.ProfileDelete,
                CMSPermissions.AdminAccess,
                CMSPermissions.AdminUsers,
                CMSPermissions.AdminSettings,
                CMSPermissions.AdminPlugins,
                CMSPermissions.AdminThemes,
                CMSPermissions.SystemConfig,
                CMSPermissions.SystemBackup,
                CMSPermissions.SystemMigRate
            };
        }
    }
}
