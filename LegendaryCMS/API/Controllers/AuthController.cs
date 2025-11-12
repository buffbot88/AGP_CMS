using System.Security.Cryptography;
using System.Text;
using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegendaryCMS.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly DatabaseService _db;

        public AuthController(DatabaseService db)
        {
            _db = db;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "Username and password are required" });
            }

            var user = _db.AuthenticateUser(request.Username, request.Password);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password" });
            }

            // Create session
            var sessionId = _db.CreateSession(user.Id, user.Username, HttpContext);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                sessionId = sessionId,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    role = user.Role
                }
            });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "Username, email, and password are required" });
            }

            if (request.Username.Length < 3)
            {
                return BadRequest(new { success = false, message = "Username must be at least 3 characters long" });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new { success = false, message = "Password must be at least 6 characters long" });
            }

            if (_db.UserExists(request.Username, request.Email))
            {
                return Conflict(new { success = false, message = "Username or email already exists" });
            }

            var userId = _db.CreateUser(request.Username, request.Email, request.Password);
            if (userId <= 0)
            {
                return StatusCode(500, new { success = false, message = "Failed to create account" });
            }

            // Create session for auto-login
            var user = _db.GetUserById(userId);
            var sessionId = _db.CreateSession(userId, request.Username, HttpContext);

            return Ok(new
            {
                success = true,
                message = "Registration successful",
                sessionId = sessionId,
                user = new
                {
                    id = userId,
                    username = request.Username,
                    email = request.Email,
                    role = user?.Role ?? "User"
                }
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Get session ID from cookie
            if (Request.Cookies.TryGetValue("AGPCMS_SESSION", out var sessionId))
            {
                try
                {
                    using var connection = _db.GetConnection();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Sessions WHERE Id = @sessionId";
                    command.Parameters.AddWithValue("@sessionId", sessionId);
                    command.ExecuteNonQuery();
                }
                catch
                {
                    // Ignore errors
                }
            }

            // Clear cookies
            Response.Cookies.Delete("AGPCMS_SESSION");
            Response.Cookies.Delete("AGPCMS_USER");

            return Ok(new { success = true, message = "Logged out successfully" });
        }

        [HttpGet("status")]
        public IActionResult GetAuthStatus()
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);

            if (userId == null)
            {
                return Ok(new { authenticated = false });
            }

            var user = _db.GetUserById(userId.Value);

            return Ok(new
            {
                authenticated = true,
                username = user?.Username,
                role = user?.Role
            });
        }

        [HttpPost("validate")]
        public IActionResult ValidateSession([FromBody] ValidateSessionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return BadRequest(new { success = false, message = "Session ID is required" });
            }

            var userId = _db.GetUserIdBySession(request.SessionId);
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Invalid or expired session" });
            }

            var user = _db.GetUserById(userId.Value);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "User not found" });
            }

            return Ok(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    role = user.Role
                }
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ValidateSessionRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }
}
