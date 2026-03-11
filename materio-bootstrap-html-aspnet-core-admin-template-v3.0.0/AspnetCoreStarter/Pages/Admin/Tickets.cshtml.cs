using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Admin
{
    public class TicketsModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin") return RedirectToPage("/Index");

            return Page();
        }
    }
}
