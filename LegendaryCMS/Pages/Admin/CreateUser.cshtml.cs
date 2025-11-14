using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text;

namespace LegendaryCMS.Pages.Admin
{
    /// <summary>
    /// Admin page for creating new users
    /// </summary>
    public class CreateUserModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string Role { get; set; } = "User";

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public CreateUserModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/users/create" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create users.";
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/users/create" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to create users.";
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Username) || 
                string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please fill in all required fields.";
                return Page();
            }

            // Validate username doesn't already exist
            if (UsernameExists(Username))
            {
                ErrorMessage = "Username already exists.";
                return Page();
            }

            // Validate email doesn't already exist
            if (EmailExists(Email))
            {
                ErrorMessage = "Email already exists.";
                return Page();
            }

            // Create the user
            if (CreateUser(Username, Email, Password, Role))
            {
                TempData["Success"] = $"User '{Username}' created successfully!";
                return RedirectToPage("/cms/admin/users");
            }
            else
            {
                ErrorMessage = "Failed to create user. Please try again.";
                return Page();
            }
        }

        private bool UsernameExists(string username)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username";
                command.Parameters.AddWithValue("@username", username);
                var count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool EmailExists(string email)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Users WHERE Email = @email";
                command.Parameters.AddWithValue("@email", email);
                var count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool CreateUser(string username, string email, string password, string role)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                
                // Hash the password
                var passwordHash = HashPassword(password);
                
                command.CommandText = @"
                    INSERT INTO Users (Username, Email, PasswordHash, Role, CreatedAt)
                    VALUES (@username, @email, @passwordHash, @role, @createdAt)
                ";
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);
                command.Parameters.AddWithValue("@role", role);
                command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));

                return command.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
