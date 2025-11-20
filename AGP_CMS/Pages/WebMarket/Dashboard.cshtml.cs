using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AGP_CMS.Services;

namespace AGP_CMS.Pages.WebMarket
{
    public class DashboardModel : PageModel
    {
        private readonly WebMarketService _webMarketService;
        private readonly DatabaseService _databaseService;
        private readonly FTPServerService _ftpService;

        public WebMarketAccount? Account { get; set; }
        public WebMarketPackage? Package { get; set; }
        public FTPConnectionInfo? FTPInfo { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public DashboardModel(WebMarketService webMarketService, DatabaseService databaseService, FTPServerService ftpService)
        {
            _webMarketService = webMarketService;
            _databaseService = databaseService;
            _ftpService = ftpService;
        }

        public IActionResult OnGet()
        {
            // Check if user is authenticated
            var userId = _databaseService.GetAuthenticatedUserId(HttpContext);
            IsAuthenticated = userId.HasValue;

            if (!IsAuthenticated)
            {
                return Page();
            }

            // Get user's web market account
            Account = _webMarketService.GetAccountByUserId(userId!.Value);

            if (Account == null)
            {
                return Page();
            }

            // Get package details
            Package = _webMarketService.GetPackageById(Account.PackageId);

            // Get FTP connection info
            FTPInfo = _ftpService.GetFTPConnectionInfo(userId!.Value);

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
