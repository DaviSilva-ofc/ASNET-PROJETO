using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Admin
{
    public class ManageContractsModel : PageModel
    {
        private readonly AppDbContext _context;

        public ManageContractsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Contrato> Contracts { get; set; } = new();
        public List<Agrupamento> Agrupamentos { get; set; } = new();

        [BindProperty]
        public Contrato NewContract { get; set; } = new();

        [BindProperty]
        public IFormFile? ContractFile { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            Console.WriteLine($"[CONTRACTS] User: {User.Identity.Name}, Role: {userRole}");

            if (userRole != "Admin") return RedirectToPage("/Index");

            try 
            {
                Contracts = await _context.Contratos
                    .Include(c => c.Agrupamento)
                    .ToListAsync();

                Agrupamentos = await _context.Agrupamentos.ToListAsync();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[CONTRACTS ERROR] {ex.Message}");
                Contracts = new List<Contrato>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (ContractFile == null || ContractFile.Length == 0)
            {
                ModelState.AddModelError("ContractFile", "Por favor, selecione um arquivo PDF.");
                return Page();
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                NewContract.AdminId = userId;
                // Note: File saving logic can be added here (e.g., saving to wwwroot/uploads)
                _context.Contratos.Add(NewContract);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var item = await _context.Contratos.FindAsync(id);
            if (item != null)
            {
                _context.Contratos.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
