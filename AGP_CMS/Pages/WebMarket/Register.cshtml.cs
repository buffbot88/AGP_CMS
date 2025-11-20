using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AGP_CMS.Services;
using System.ComponentModel.DataAnnotations;

namespace AGP_CMS.Pages.WebMarket
{
    public class RegisterModel : PageModel
    {
        private readonly WebMarketService _webMarketService;
        private readonly DatabaseService _databaseService;

        [BindProperty(SupportsGet = true)]
        public int PackageId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Site name is required")]
        [StringLength(100, ErrorMessage = "Site name must be between 3 and 100 characters", MinimumLength = 3)]
        public string SiteName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "FTP password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least 6 characters", MinimumLength = 6)]
        public string FtpPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please confirm your FTP password")]
        [Compare(nameof(FtpPassword), ErrorMessage = "Passwords do not match")]
        public string FtpPasswordConfirm { get; set; } = string.Empty;

        public WebMarketPackage? SelectedPackage { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool HasExistingAccount { get; set; }
        public string? CurrentUsername { get; set; }
        public string? CurrentEmail { get; set; }
        public string? ErrorMessage { get; set; }

        public RegisterModel(WebMarketService webMarketService, DatabaseService databaseService)
        {
            _webMarketService = webMarketService;
            _databaseService = databaseService;
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

            // Check if user already has an account
            var existingAccount = _webMarketService.GetAccountByUserId(userId!.Value);
            if (existingAccount != null)
            {
                HasExistingAccount = true;
                return Page();
            }

            // Get user info
            var user = _databaseService.GetUserById(userId!.Value);
            if (user != null)
            {
                CurrentUsername = user.Username;
                CurrentEmail = user.Email;
            }

            // Get selected package
            SelectedPackage = _webMarketService.GetPackageById(PackageId);

            return Page();
        }

        public IActionResult OnPost()
        {
            // Check if user is authenticated
            var userId = _databaseService.GetAuthenticatedUserId(HttpContext);
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "You must be logged in to register for web hosting";
                return RedirectToPage("/Login");
            }

            // Get user info
            var user = _databaseService.GetUserById(userId.Value);
            if (user == null)
            {
                ErrorMessage = "User not found";
                return Page();
            }

            // Set current user info for display
            CurrentUsername = user.Username;
            CurrentEmail = user.Email;

            // Get package
            SelectedPackage = _webMarketService.GetPackageById(PackageId);
            IsAuthenticated = true;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if user already has an account
            var existingAccount = _webMarketService.GetAccountByUserId(userId.Value);
            if (existingAccount != null)
            {
                TempData["ErrorMessage"] = "You already have a web hosting account";
                return RedirectToPage("/WebMarket/Dashboard");
            }

            // Create account
            var result = _webMarketService.CreateAccount(
                userId.Value,
                user.Username,
                user.Email,
                SiteName,
                PackageId,
                FtpPassword
            );

            if (result.success)
            {
                TempData["SuccessMessage"] = "Your web hosting account has been created successfully!";
                return RedirectToPage("/WebMarket/Dashboard");
            }
            else
            {
                ErrorMessage = result.message;
                return Page();
            }
        }
    }
}
