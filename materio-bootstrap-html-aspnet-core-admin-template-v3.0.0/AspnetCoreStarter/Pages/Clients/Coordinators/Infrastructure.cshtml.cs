using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Coordinators
{
    public class CoordinatorInfrastructureModel : PageModel
    {
        private readonly AppDbContext _context;

        public CoordinatorInfrastructureModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Bloco> MyBlocks { get; set; } = new();
        public School? MySchool { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || User.FindFirst(ClaimTypes.Role)?.Value != "Coordenador")
                return RedirectToPage("/Index");

            int userId = int.Parse(userIdStr);

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (coord?.SchoolId == null) return Page();

             MySchool = coord.School;

            MyBlocks = await _context.Blocos
                .Include(b => b.Rooms)
                    .ThenInclude(r => r.Equipments)
                .Where(b => b.SchoolId == coord.SchoolId)
                .ToListAsync();

            return Page();
        }
    }
}
