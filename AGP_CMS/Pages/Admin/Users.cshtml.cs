using AGP_CMS.Security;
using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// User Management - Manage users, roles, and permissions
    /// </summary>
    public class UsersModel : PageModel
    {
        private readonly DatabaseService _db;

        public List<UserInfo> Users { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();

        [BindProperty]
        public string? SelectedUserId { get; set; }

        [BindProperty]
        public string? SelectedRole { get; set; }

        public UsersModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/users" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to access user management.";
                return RedirectToPage("/Index");
            }

            LoadUsers();
            LoadAvailableRoles();
            return Page();
        }

        public IActionResult OnPostAssignRole()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/users" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to manage user roles.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrEmpty(SelectedUserId) || string.IsNullOrEmpty(SelectedRole))
            {
                TempData["Error"] = "User ID and Role are required";
                return RedirectToPage();
            }

            // Parse user ID and update role in database
            if (int.TryParse(SelectedUserId, out int targetUserId))
            {
                if (_db.UpdateUserRole(targetUserId, SelectedRole))
                {
                    TempData["Success"] = $"Role '{SelectedRole}' assigned to user successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to assign role";
                }
            }
            else
            {
                TempData["Error"] = "Invalid user ID";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostRemoveRole(string userId, string role)
        {
            // Check if user is logged in
            var authenticatedUserId = _db.GetAuthenticatedUserId(HttpContext);
            if (authenticatedUserId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/users" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(authenticatedUserId.Value))
            {
                TempData["Error"] = "You do not have permission to manage user roles.";
                return RedirectToPage("/Index");
            }

            // Parse user ID and update role to default 'User' role in database
            if (int.TryParse(userId, out int targetUserId))
            {
                if (_db.UpdateUserRole(targetUserId, "User"))
                {
                    TempData["Success"] = $"Role '{role}' removed from user successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to remove role";
                }
            }
            else
            {
                TempData["Error"] = "Invalid user ID";
            }

            return RedirectToPage();
        }

        private void LoadUsers()
        {
            Users = _db.GetAllUsers().Select(u => new UserInfo
            {
                UserId = u.Id.ToString(),
                Username = u.Username,
                Email = u.Email,
                Roles = new List<string> { u.Role },
                Permissions = new List<string>(), // Can be enhanced later
                CreatedAt = u.CreatedAt,
                LastLoginAt = DateTime.MinValue, // Can be enhanced later
                IsActive = true
            }).ToList();
        }

        private void LoadAvailableRoles()
        {
            AvailableRoles = new List<string>
            {
                CMSRoles.SuperAdmin,
                CMSRoles.Admin,
                CMSRoles.Moderator,
                CMSRoles.User,
                CMSRoles.Guest
            };
        }
    }

    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsActive { get; set; }
    }
}
