using AGP_CMS.Security;
using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// Edit User - Modify user account details and permissions
    /// </summary>
    public class EditUserModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public int UserId { get; set; }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Role { get; set; } = string.Empty;

        [BindProperty]
        public bool IsActive { get; set; }

        public new Services.UserInfo? User { get; set; }
        public List<string> AvailableRoles { get; set; } = new();

        public EditUserModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet(int id)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = $"/cms/admin/users/edit?id={id}" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to edit users.";
                return RedirectToPage("/Index");
            }

            UserId = id;
            LoadUser();
            LoadAvailableRoles();

            return Page();
        }

        public IActionResult OnPostUpdateUser()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to edit users.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email))
            {
                TempData["Error"] = "Username and email are required";
                LoadUser();
                LoadAvailableRoles();
                return Page();
            }

            try
            {
                var success = _db.UpdateUser(UserId, Username, Email, Role, IsActive);

                if (success)
                {
                    TempData["Success"] = "User updated successfully";
                    return RedirectToPage("/Admin/Users");
                }
                else
                {
                    TempData["Error"] = "Failed to update user";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating user: {ex.Message}";
            }

            LoadUser();
            LoadAvailableRoles();
            return Page();
        }

        private void LoadUser()
        {
            User = _db.GetUserForAdmin(UserId);
            if (User != null)
            {
                Username = User.Username;
                Email = User.Email;
                Role = User.Role;
                IsActive = User.IsActive;
            }
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
}
