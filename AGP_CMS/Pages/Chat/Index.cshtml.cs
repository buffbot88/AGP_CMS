using AGP_CMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AGP_CMS.Pages.Chat
{
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _db;

        public UserInfo? CurrentUser { get; set; }
        public bool IsAuthenticated { get; set; }

        public IndexModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is authenticated
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            IsAuthenticated = userId.HasValue;

            if (userId.HasValue)
            {
                CurrentUser = _db.GetUserById(userId.Value);
            }

            return Page();
        }
    }
}
