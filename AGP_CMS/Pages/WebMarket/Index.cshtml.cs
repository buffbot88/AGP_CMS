using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AGP_CMS.Services;
using System.Collections.Generic;

namespace AGP_CMS.Pages.WebMarket
{
    public class IndexModel : PageModel
    {
        private readonly WebMarketService _webMarketService;
        private readonly DatabaseService _databaseService;

        public List<WebMarketPackage> Packages { get; set; } = new();
        public bool IsAuthenticated { get; set; }
        public bool HasExistingAccount { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IndexModel(WebMarketService webMarketService, DatabaseService databaseService)
        {
            _webMarketService = webMarketService;
            _databaseService = databaseService;
        }

        public IActionResult OnGet()
        {
            // Check if user is authenticated
            var userId = _databaseService.GetAuthenticatedUserId(HttpContext);
            IsAuthenticated = userId.HasValue;

            // Check if user already has a web market account
            if (IsAuthenticated)
            {
                var existingAccount = _webMarketService.GetAccountByUserId(userId!.Value);
                HasExistingAccount = existingAccount != null;
            }

            // Get available packages
            Packages = _webMarketService.GetPackages();

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
