using AGP_CMS.Configuration;
using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Admin
{
    /// <summary>
    /// Settings Management - Configure site settings, SEO, and privacy
    /// </summary>
    public class SettingsModel : PageModel
    {
        private readonly DatabaseService _db;

        [BindProperty]
        public string? SiteName { get; set; }

        [BindProperty]
        public string? SiteDescription { get; set; }

        [BindProperty]
        public string? SiteKeywords { get; set; }

        [BindProperty]
        public bool EnableSEO { get; set; }

        [BindProperty]
        public bool AllowSearchEngineIndexing { get; set; }

        [BindProperty]
        public bool HidePrivateFromSearch { get; set; }

        [BindProperty]
        public string? AdminEmail { get; set; }

        [BindProperty]
        public string? DefaultTheme { get; set; }

        [BindProperty]
        public bool EnableCSRF { get; set; }

        [BindProperty]
        public bool EnableXSSProtection { get; set; }

        [BindProperty]
        public int SessionTimeout { get; set; }

        [BindProperty]
        public string? HomePageTitle { get; set; }

        [BindProperty]
        public string? HomePageTagline { get; set; }

        [BindProperty]
        public string? HomePageWelcomeMessage { get; set; }

        public SettingsModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/settings" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to access settings.";
                return RedirectToPage("/Index");
            }

            LoadSettings();
            return Page();
        }

        public IActionResult OnPost()
        {
            // Check if user is logged in
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToPage("/Login", new { returnUrl = "/cms/admin/settings" });
            }

            // Check if user has admin permissions
            if (!_db.IsUserAdmin(userId.Value))
            {
                TempData["Error"] = "You do not have permission to modify settings.";
                return RedirectToPage("/Index");
            }

            // Save settings to database
            try
            {
                _db.SetSetting("SiteName", SiteName ?? "Legendary CMS");
                _db.SetSetting("SiteDescription", SiteDescription ?? "");
                _db.SetSetting("SiteKeywords", SiteKeywords ?? "");
                _db.SetSetting("EnableSEO", EnableSEO.ToString());
                _db.SetSetting("AllowSearchEngineIndexing", AllowSearchEngineIndexing.ToString());
                _db.SetSetting("HidePrivateFromSearch", HidePrivateFromSearch.ToString());
                _db.SetSetting("AdminEmail", AdminEmail ?? "");
                _db.SetSetting("DefaultTheme", DefaultTheme ?? "classic");
                _db.SetSetting("EnableCSRF", EnableCSRF.ToString());
                _db.SetSetting("EnableXSSProtection", EnableXSSProtection.ToString());
                _db.SetSetting("SessionTimeout", SessionTimeout.ToString());
                _db.SetSetting("HomePageTitle", HomePageTitle ?? "ASHAT OS CMS");
                _db.SetSetting("HomePageTagline", HomePageTagline ?? "Your Complete Content Management System");
                _db.SetSetting("HomePageWelcomeMessage", HomePageWelcomeMessage ?? "Blogs • Forums • Profiles • Downloads");

                TempData["Success"] = "Settings saved successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to save settings: {ex.Message}";
            }

            return RedirectToPage();
        }

        private void LoadSettings()
        {
            // Load from database with defaults
            var settings = _db.GetAllSettings();

            SiteName = settings.GetValueOrDefault("SiteName", "Legendary CMS");
            SiteDescription = settings.GetValueOrDefault("SiteDescription", "A professional, modular CMS platform");
            SiteKeywords = settings.GetValueOrDefault("SiteKeywords", "cms, content management, forums, blogs");
            EnableSEO = bool.Parse(settings.GetValueOrDefault("EnableSEO", "true"));
            AllowSearchEngineIndexing = bool.Parse(settings.GetValueOrDefault("AllowSearchEngineIndexing", "true"));
            HidePrivateFromSearch = bool.Parse(settings.GetValueOrDefault("HidePrivateFromSearch", "true"));
            AdminEmail = settings.GetValueOrDefault("AdminEmail", "admin@legendarycms.local");
            DefaultTheme = settings.GetValueOrDefault("DefaultTheme", "classic");
            EnableCSRF = bool.Parse(settings.GetValueOrDefault("EnableCSRF", "true"));
            EnableXSSProtection = bool.Parse(settings.GetValueOrDefault("EnableXSSProtection", "true"));
            SessionTimeout = int.Parse(settings.GetValueOrDefault("SessionTimeout", "3600"));
            HomePageTitle = settings.GetValueOrDefault("HomePageTitle", "ASHAT OS CMS");
            HomePageTagline = settings.GetValueOrDefault("HomePageTagline", "Your Complete Content Management System");
            HomePageWelcomeMessage = settings.GetValueOrDefault("HomePageWelcomeMessage", "Blogs • Forums • Profiles • Downloads");
        }
    }
}
