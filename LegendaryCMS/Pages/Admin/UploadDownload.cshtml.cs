using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace LegendaryCMS.Pages.Admin
{
    /// <summary>
    /// Admin page for uploading downloadable files
    /// </summary>
    public class UploadDownloadModel : PageModel
    {
        private readonly DatabaseService _db;
        private readonly IWebHostEnvironment _environment;

        [BindProperty]
        public new IFormFile? File { get; set; }

        [BindProperty]
        public string FileName { get; set; } = string.Empty;

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public UploadDownloadModel(DatabaseService db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/downloads/upload" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to upload files.";
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/downloads/upload" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to upload files.";
                return RedirectToPage("/Index");
            }

            if (File == null || File.Length == 0)
            {
                ErrorMessage = "Please select a file to upload.";
                return Page();
            }

            try
            {
                // Get the web root path, fallback to ContentRootPath/wwwroot if null
                var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(webRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Use provided filename or original filename
                var fileName = string.IsNullOrWhiteSpace(FileName) ? File.FileName : FileName;
                
                // Generate unique filename to avoid collisions
                var fileExtension = Path.GetExtension(fileName);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var uniqueFileName = $"{fileNameWithoutExt}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await File.CopyToAsync(stream);
                }

                // Calculate file size
                var fileSize = FormatFileSize(File.Length);

                // Save to database
                if (SaveDownload(fileName, uniqueFileName, Description, fileSize, userId.Value))
                {
                    TempData["Success"] = $"File '{fileName}' uploaded successfully!";
                    return RedirectToPage("/cms/admin/downloads");
                }
                else
                {
                    // Clean up file if database save failed
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    ErrorMessage = "Failed to save file information to database.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to upload file: {ex.Message}";
                return Page();
            }
        }

        private bool SaveDownload(string fileName, string uniqueFileName, string description, string fileSize, int uploaderId)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Downloads (FileName, UniqueFileName, Description, FileSize, UploaderId, UploadedAt, DownloadCount)
                    VALUES (@fileName, @uniqueFileName, @description, @fileSize, @uploaderId, @uploadedAt, 0)
                ";
                command.Parameters.AddWithValue("@fileName", fileName);
                command.Parameters.AddWithValue("@uniqueFileName", uniqueFileName);
                command.Parameters.AddWithValue("@description", description ?? string.Empty);
                command.Parameters.AddWithValue("@fileSize", fileSize);
                command.Parameters.AddWithValue("@uploaderId", uploaderId);
                command.Parameters.AddWithValue("@uploadedAt", DateTime.UtcNow.ToString("o"));

                return command.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
