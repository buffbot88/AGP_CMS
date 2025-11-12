using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.ControlPanel
{
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _db;

        public new UserInfo? User { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public IndexModel(DatabaseService db)
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

        public IActionResult OnPost(string email, string title, string bio)
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            User = _db.GetUserById(userId.Value);

            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorMessage = "Email address is required.";
                return Page();
            }

            // Update user settings
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE Users
                SET Email = @email, Title = @title, Bio = @bio, UpdatedAt = @updatedAt
                WHERE Id = @userId
            ";
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@title", title ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@bio", bio ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@userId", userId.Value);

                command.ExecuteNonQuery();

                SuccessMessage = "Your account settings have been updated successfully!";
                User = _db.GetUserById(userId.Value); // Reload user data
            }
            catch
            {
                ErrorMessage = "Failed to update account settings. Please try again.";
            }

            return Page();
        }
    }
}
