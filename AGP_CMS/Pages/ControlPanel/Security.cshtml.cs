using System.Security.Cryptography;
using System.Text;
using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.ControlPanel
{
    public class SecurityModel : PageModel
    {
        private readonly DatabaseService _db;

        public new UserInfo? User { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public SecurityModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            User = _db.GetUserById(userId.Value);
            return Page();
        }

        public IActionResult OnPost(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            User = _db.GetUserById(userId.Value);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ErrorMessage = "All password fields are required.";
                return Page();
            }

            if (newPassword.Length < 6)
            {
                ErrorMessage = "New password must be at least 6 characters long.";
                return Page();
            }

            if (newPassword != confirmPassword)
            {
                ErrorMessage = "New passwords do not match.";
                return Page();
            }

            if (currentPassword == newPassword)
            {
                ErrorMessage = "New password must be different from current password.";
                return Page();
            }

            // Verify current password
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT PasswordHash FROM Users WHERE Id = @userId";
                command.Parameters.AddWithValue("@userId", userId.Value);

                var storedHash = command.ExecuteScalar() as string;
                var currentPasswordHash = HashPassword(currentPassword);

                if (storedHash != currentPasswordHash)
                {
                    ErrorMessage = "Current password is incorrect.";
                    return Page();
                }

                // Update password
                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = @"
                UPDATE Users
                SET PasswordHash = @newPasswordHash, UpdatedAt = @updatedAt
                WHERE Id = @userId
            ";
                updateCommand.Parameters.AddWithValue("@newPasswordHash", HashPassword(newPassword));
                updateCommand.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
                updateCommand.Parameters.AddWithValue("@userId", userId.Value);

                updateCommand.ExecuteNonQuery();

                SuccessMessage = "Your password has been changed successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to change password: {ex.Message}";
            }

            return Page();
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "AGP_CMS_SALT"));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
