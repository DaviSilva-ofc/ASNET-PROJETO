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
    public class DirectorContractsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorContractsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Contrato> Contracts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterPeriod { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public string? MyAgrupamentoName { get; set; }

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
                Contracts = new List<Contrato>();
                return Page();
            }

            int myAgrupamentoId = director.AgrupamentoId.Value;
            MyAgrupamentoName = director.Agrupamento?.Name;

            var query = _context.Contratos
                .Include(c => c.Agrupamento)
                .Where(c => c.AgrupamentoId == myAgrupamentoId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(FilterType))
            {
                query = query.Where(c => c.ContractType.Contains(FilterType));
            }

            if (!string.IsNullOrEmpty(FilterPeriod))
            {
                query = query.Where(c => c.Period != null && c.Period.Contains(FilterPeriod));
            }

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                query = query.Where(c => c.ContractStatus == FilterStatus);
            }

            Contracts = await query.ToListAsync();

            return Page();
        }
    }
}
