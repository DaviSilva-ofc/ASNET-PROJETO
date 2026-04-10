using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Private
{
    public class PrivateContractsModel : PageModel
    {
        private readonly AppDbContext _context;

        public PrivateContractsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Contrato> Contracts { get; set; } = new();
        public Empresa Empresa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var user = await _context.Users.Include(u => u.Empresa).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.EmpresaId == null) return RedirectToPage("/Auth/Login");

            Empresa = user.Empresa;
            int empresaId = user.EmpresaId.Value;

            var query = _context.Contratos
                .Include(c => c.Empresa)
                .Include(c => c.Admin)
                    .ThenInclude(a => a.User)
                .Where(c => c.EmpresaId == empresaId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(FilterType))
            {
                query = query.Where(c => c.ContractType.Contains(FilterType));
            }

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                query = query.Where(c => c.ContractStatus == FilterStatus);
            }

            Contracts = await query.OrderByDescending(c => c.ExpiryDate).ToListAsync();

            return Page();
        }
    }
}
