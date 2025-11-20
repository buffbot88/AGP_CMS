using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace AGP_CMS.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsAdminSetup { get; set; }

        public RegisterModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet(bool isAdmin = false)
        {
            IsAdminSetup = isAdmin && IsFirstRun();
        }

        public IActionResult OnPost(string username, string email, string password, string confirmPassword, bool isAdmin = false)
        {
            // Check if this is the first user (should be admin regardless of parameter)
            var isFirstUser = IsFirstRun();
            IsAdminSetup = isAdmin && isFirstUser;

            // Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ErrorMessage = "All fields are required.";
                return Page();
            }

            if (username.Length < 3)
            {
                ErrorMessage = "Username must be at least 3 characters long.";
                return Page();
            }

            if (password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters long.";
                return Page();
            }

            if (password != confirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            // Check if username or email already exists
            if (UserExists(username, email))
            {
                ErrorMessage = "Username or email already exists.";
                return Page();
            }

            // Create user - first user is always admin
            var role = isFirstUser ? "Admin" : "User";
            var userId = CreateUser(username, email, password, role);

            if (userId > 0)
            {
                // Create user profile
                CreateUserProfile(userId);

                // Auto-login for first user (admin) or admin setup
                if (isFirstUser || IsAdminSetup)
                {
                    CreateSession(userId, username);
                    return RedirectToPage("/Index");
                }

                // Redirect to login for regular users
                return RedirectToPage("/Login");
            }

            ErrorMessage = "Failed to create account. Please try again.";
            return Page();
        }

        private bool IsFirstRun()
        {
            var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users WHERE Role = 'Admin'";
            var count = Convert.ToInt32(command.ExecuteScalar());

            return count == 0;
        }

        private bool UserExists(string username, string email)
        {
            var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username OR Email = @email";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        private int CreateUser(string username, string email, string password, string role)
        {
            var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Users (Username, Email, PasswordHash, Role, CreatedAt, UpdatedAt, IsActive)
            VALUES (@username, @email, @passwordHash, @role, @createdAt, @updatedAt, 1);
            SELECT last_insert_rowid();
        ";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@passwordHash", HashPassword(password));
            command.Parameters.AddWithValue("@role", role);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));

            var userId = Convert.ToInt32(command.ExecuteScalar());
            return userId;
        }

        private void CreateUserProfile(int userId)
        {
            var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO UserProfiles (UserId, PostCount, LikesReceived)
            VALUES (@userId, 0, 0)
        ";
            command.Parameters.AddWithValue("@userId", userId);
            command.ExecuteNonQuery();
        }

        private void CreateSession(int userId, string username)
        {
            var sessionId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Sessions (Id, UserId, CreatedAt, ExpiresAt, IpAddress, UserAgent)
            VALUES (@id, @userId, @createdAt, @expiresAt, @ip, @userAgent)
        ";
            command.Parameters.AddWithValue("@id", sessionId);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@expiresAt", expiresAt.ToString("o"));
            command.Parameters.AddWithValue("@ip", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            command.Parameters.AddWithValue("@userAgent", HttpContext.Request.Headers["User-Agent"].ToString());
            command.ExecuteNonQuery();

            // Set cookie
            Response.Cookies.Append("AGPCMS_SESSION", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            });

            Response.Cookies.Append("AGPCMS_USER", username, new CookieOptions
            {
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "AGP_CMS_SALT"));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
