using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.ControlPanel
{
    public class ProfileModel : PageModel
    {
        private readonly DatabaseService _db;

        public string? Username { get; set; }
        public bool IsAdmin { get; set; }
        public string? DisplayName { get; set; }
        public string? Location { get; set; }
        public string? Website { get; set; }
        public string? Twitter { get; set; }
        public string? Github { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public ProfileModel(DatabaseService db)
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

            var user = _db.GetUserById(userId.Value);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            Username = user.Username;
            IsAdmin = user.Role == "Admin";

            // Load user profile data
            LoadUserProfile(userId.Value);

            return Page();
        }

        public IActionResult OnPost(string? displayName, string? location, string? website, string? twitter, string? github)
        {
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            var user = _db.GetUserById(userId.Value);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            Username = user.Username;
            IsAdmin = user.Role == "Admin";

            try
            {
                using var connection = _db.GetConnection();

                // Check if profile exists
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM UserProfiles WHERE UserId = @userId";
                checkCommand.Parameters.AddWithValue("@userId", userId.Value);
                var profileExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;

                if (profileExists)
                {
                    // Update existing profile
                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                    UPDATE UserProfiles
                    SET DisplayName = @displayName, 
                        Location = @location, 
                        Website = @website,
                        Twitter = @twitter, 
                        Github = @github
                    WHERE UserId = @userId
                ";
                    updateCommand.Parameters.AddWithValue("@displayName", displayName ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@location", location ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@website", website ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@twitter", twitter ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@github", github ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@userId", userId.Value);
                    updateCommand.ExecuteNonQuery();
                }
                else
                {
                    // Insert new profile
                    var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                    INSERT INTO UserProfiles (UserId, DisplayName, Location, Website, Twitter, Github, PostCount, LikesReceived)
                    VALUES (@userId, @displayName, @location, @website, @twitter, @github, 0, 0)
                ";
                    insertCommand.Parameters.AddWithValue("@userId", userId.Value);
                    insertCommand.Parameters.AddWithValue("@displayName", displayName ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@location", location ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@website", website ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@twitter", twitter ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@github", github ?? (object)DBNull.Value);
                    insertCommand.ExecuteNonQuery();
                }

                SuccessMessage = "Your profile has been updated successfully!";

                // Reload profile data
                LoadUserProfile(userId.Value);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update profile: {ex.Message}";

                // Keep form values
                DisplayName = displayName;
                Location = location;
                Website = website;
                Twitter = twitter;
                Github = github;
            }

            return Page();
        }

        private void LoadUserProfile(int userId)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT DisplayName, Location, Website, Twitter, Github
                FROM UserProfiles
                WHERE UserId = @userId
            ";
                command.Parameters.AddWithValue("@userId", userId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    DisplayName = reader.IsDBNull(0) ? null : reader.GetString(0);
                    Location = reader.IsDBNull(1) ? null : reader.GetString(1);
                    Website = reader.IsDBNull(2) ? null : reader.GetString(2);
                    Twitter = reader.IsDBNull(3) ? null : reader.GetString(3);
                    Github = reader.IsDBNull(4) ? null : reader.GetString(4);
                }
            }
            catch
            {
                // Profile doesn't exist yet, that's ok
            }
        }
    }
}
