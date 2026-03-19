using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorTicketsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorTicketsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Ticket> Tickets { get; set; } = new();
        public string? MyAgrupamentoName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Find the director's agrupamento
            var director = await _context.Diretores
                .Include(d => d.Agrupamento)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (director == null || director.AgrupamentoId == null)
            {
                Tickets = new List<Ticket>();
                return Page();
            }

            int myAgrupamentoId = director.AgrupamentoId.Value;
            MyAgrupamentoName = director.Agrupamento?.Name;

            // Get schools in this agrupamento
            var schoolIds = await _context.Schools
                .Where(s => s.AgrupamentoId == myAgrupamentoId)
                .Select(s => s.Id)
                .ToListAsync();

            var query = _context.Tickets
                .Include(t => t.School)
                .Where(t => t.SchoolId.HasValue && schoolIds.Contains(t.SchoolId.Value))
                .AsQueryable();

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                query = query.Where(t => t.Status == FilterStatus);
            }

            Tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            return Page();
        }
    }
}
