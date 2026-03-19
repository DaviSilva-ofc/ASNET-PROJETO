using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorChatModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Diretor" && userRole != "Admin") return RedirectToPage("/Index");

            return Page();
        }
    }
}
