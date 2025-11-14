using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Admin
{
    /// <summary>
    /// Admin page for managing downloads
    /// </summary>
    public class DownloadsModel : PageModel
    {
        private readonly DatabaseService _db;

        public List<DownloadInfo> Downloads { get; set; } = new();

        public DownloadsModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/downloads" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to manage downloads.";
                return RedirectToPage("/Index");
            }

            LoadDownloads();
            return Page();
        }

        public IActionResult OnPostDelete(int downloadId)
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/downloads" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to delete downloads.";
                return RedirectToPage("/Index");
            }

            if (DeleteDownload(downloadId))
            {
                TempData["Success"] = "Download deleted successfully";
            }
            else
            {
                TempData["Error"] = "Failed to delete download";
            }

            return RedirectToPage();
        }

        private void LoadDownloads()
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, FileName, Description, FileSize, DownloadCount, UploadedAt
                    FROM Downloads
                    ORDER BY UploadedAt DESC
                ";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Downloads.Add(new DownloadInfo
                    {
                        Id = reader.GetInt32(0),
                        FileName = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        FileSize = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3),
                        DownloadCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        UploadedAt = DateTime.Parse(reader.GetString(5))
                    });
                }
            }
            catch
            {
                // Ignore errors - table might not exist
            }
        }

        private bool DeleteDownload(int downloadId)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Downloads WHERE Id = @id";
                command.Parameters.AddWithValue("@id", downloadId);
                return command.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public class DownloadInfo
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public int DownloadCount { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
