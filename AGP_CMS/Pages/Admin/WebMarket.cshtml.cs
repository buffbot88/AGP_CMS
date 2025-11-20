using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AGP_CMS.Services;
using System.Collections.Generic;
using System.Linq;

namespace AGP_CMS.Pages.Admin
{
    public class WebMarketModel : PageModel
    {
        private readonly WebMarketService _webMarketService;
        private readonly DatabaseService _databaseService;

        public List<WebMarketAccount> Accounts { get; set; } = new();
        public List<WebMarketPackage> Packages { get; set; } = new();
        public bool IsAdmin { get; set; }
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public int FtpEnabledAccounts { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public WebMarketModel(WebMarketService webMarketService, DatabaseService databaseService)
        {
            _webMarketService = webMarketService;
            _databaseService = databaseService;
        }

        public IActionResult OnGet()
        {
            // Check if user is authenticated
            var userId = _databaseService.GetAuthenticatedUserId(HttpContext);
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            // Check if user is admin
            var user = _databaseService.GetUserById(userId.Value);
            IsAdmin = user?.Role == "Admin";

            if (!IsAdmin)
            {
                return Page();
            }

            // Get all accounts
            Accounts = _webMarketService.GetAllAccounts();

            // Get all packages
            Packages = _webMarketService.GetPackages();

            // Calculate statistics
            TotalAccounts = Accounts.Count;
            ActiveAccounts = Accounts.Count(a => a.Status == "active");
            FtpEnabledAccounts = Accounts.Count(a => a.FtpEnabled && a.Status == "active");

            // Check for messages in TempData
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }
            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]?.ToString();
            }

            return Page();
        }
    }
}
