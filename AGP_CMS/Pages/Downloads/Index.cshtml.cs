using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Downloads
{
    /// <summary>
    /// Downloads Index - Browse and download files
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _db;

        public List<DownloadCategory> Categories { get; set; } = new();
        public List<DownloadFile> RecentFiles { get; set; } = new();
        public List<DownloadFile> PopularFiles { get; set; } = new();

        public IndexModel(DatabaseService db)
        {
            _db = db;
        }

        public void OnGet()
        {
            LoadCategories();
            LoadRecentFiles();
            LoadPopularFiles();
        }

        private void LoadCategories()
        {
            var dbCategories = _db.GetDownloadCategories();
            Categories = dbCategories.Select(c => new DownloadCategory
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                FileCount = c.FileCount,
                Icon = c.Icon,
                IsPrivate = c.IsPrivate
            }).ToList();
        }

        private void LoadRecentFiles()
        {
            var dbFiles = _db.GetRecentDownloads(10);
            RecentFiles = dbFiles.Select(f => new DownloadFile
            {
                FileId = f.FileId,
                FileName = f.FileName,
                Description = f.Description,
                FilePath = f.FilePath,
                FileSize = f.FileSize,
                FileType = f.FileType,
                CategoryId = f.CategoryId,
                CategoryName = f.CategoryName,
                UploadedBy = f.UploadedBy,
                UploadedAt = f.UploadedAt,
                DownloadCount = f.DownloadCount,
                IsPublic = f.IsPublic
            }).ToList();
        }

        private void LoadPopularFiles()
        {
            var dbFiles = _db.GetPopularDownloads(10);
            PopularFiles = dbFiles.Select(f => new DownloadFile
            {
                FileId = f.FileId,
                FileName = f.FileName,
                Description = f.Description,
                FilePath = f.FilePath,
                FileSize = f.FileSize,
                FileType = f.FileType,
                CategoryId = f.CategoryId,
                CategoryName = f.CategoryName,
                UploadedBy = f.UploadedBy,
                UploadedAt = f.UploadedAt,
                DownloadCount = f.DownloadCount,
                IsPublic = f.IsPublic
            }).ToList();
        }
    }

    public class DownloadCategory
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "📁";
        public int FileCount { get; set; }
        public bool IsPrivate { get; set; }
        public List<string> AllowedRoles { get; set; } = new();
    }

    public class DownloadFile
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public int DownloadCount { get; set; }
        public bool IsPublic { get; set; }
        public List<string> AllowedRoles { get; set; } = new();
    }
}
