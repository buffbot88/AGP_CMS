using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace AGP_CMS.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            // Check if this is first run
            if (IsFirstRun())
            {
                SuccessMessage = "Welcome! Please create your admin account.";
            }
        }

        public IActionResult OnPost(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Username and password are required.";
                return Page();
            }

            // Check if this is first run - create admin account
            if (IsFirstRun())
            {
                return RedirectToPage("/Register", new { isAdmin = true });
            }

            // Authenticate user
            var user = AuthenticateUser(username, password);
            if (user == null)
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }

            // Create session
            CreateSession(user.Id, user.Username);

            // Redirect to homepage
            return RedirectToPage("/Index");
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

        private UserInfo? AuthenticateUser(string usernameOrEmail, string password)
        {
            var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Id, Username, PasswordHash, Role 
            FROM Users 
            WHERE (Username = @username OR Email = @username) AND IsActive = 1
        ";
            command.Parameters.AddWithValue("@username", usernameOrEmail);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var storedHash = reader.GetString(2);
                var passwordHash = HashPassword(password);

                if (storedHash == passwordHash)
                {
                    return new UserInfo
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Role = reader.GetString(3)
                    };
                }
            }

            return null;
        }

        private void CreateSession(int userId, string username)
        {
            var sessionId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var connectionString = _configuration["AGP_CMS:Database:ConnectionString"] ?? "Data Source=agp_cms.db";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Create session in database
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

            // Update last login
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE Users SET LastLoginAt = @lastLogin WHERE Id = @userId";
            updateCommand.Parameters.AddWithValue("@lastLogin", DateTime.UtcNow.ToString("o"));
            updateCommand.Parameters.AddWithValue("@userId", userId);
            updateCommand.ExecuteNonQuery();

            // Set cookie
            Response.Cookies.Append("AGPCMS_SESSION", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            });

            // Also set username cookie for convenience
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

        private class UserInfo
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }
}
