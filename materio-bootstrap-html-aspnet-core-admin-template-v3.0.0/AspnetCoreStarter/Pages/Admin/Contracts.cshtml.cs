using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        public List<School> Schools { get; set; } = new();
        public List<Empresa> Empresas { get; set; } = new();

        [BindProperty]
        public Contrato NewContract { get; set; } = new();

        [BindProperty]
        public Contrato? EditContract { get; set; }

        [BindProperty]
        public IFormFile? ContractFile { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterPeriod { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterAgrupamento { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            Console.WriteLine($"[CONTRACTS] User: {User.Identity.Name}, Role: {userRole}");

            if (userRole != "Admin") return RedirectToPage("/Index");

            try 
            {
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contratos ADD COLUMN nivel_urgencia VARCHAR(20) NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contratos ADD COLUMN data_expiracao DATETIME NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contratos ADD COLUMN id_escola INT NULL, ADD COLUMN id_empresa INT NULL;"); } catch { }

                var query = _context.Contratos
                    .Include(c => c.Agrupamento)
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

                if (FilterAgrupamento.HasValue)
                {
                    query = query.Where(c => c.AgrupamentoId == FilterAgrupamento);
                }

                Contracts = await query.ToListAsync();
                Agrupamentos = await _context.Agrupamentos.ToListAsync();
                Schools = await _context.Schools.ToListAsync();
                Empresas = await _context.Empresas.ToListAsync();
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
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contratos ADD COLUMN nivel_urgencia VARCHAR(20) NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contratos ADD COLUMN data_expiracao DATETIME NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contratos ADD COLUMN id_escola INT NULL, ADD COLUMN id_empresa INT NULL;"); } catch { }

            if (ContractFile == null || ContractFile.Length == 0)
            {
                ModelState.AddModelError("ContractFile", "Por favor, selecione um arquivo PDF.");
                return Page();
            }

            if (!NewContract.AgrupamentoId.HasValue && !NewContract.SchoolId.HasValue && !NewContract.EmpresaId.HasValue)
            {
                TempData["ErrorMessage"] = "É obrigatório selecionar um Agrupamento, Escola ou Empresa.";
                return RedirectToPage();
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                NewContract.AdminId = userId;
                _context.Contratos.Add(NewContract);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Contrato carregado com sucesso.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (EditContract == null) return RedirectToPage();

            var existing = await _context.Contratos.FindAsync(EditContract.Id);
            if (existing != null)
            {
                existing.ContractType = EditContract.ContractType;
                existing.Period = EditContract.Period;
                existing.ContractStatus = EditContract.ContractStatus;
                existing.AgrupamentoId = EditContract.AgrupamentoId;
                existing.UrgencyLevel = EditContract.UrgencyLevel;
                existing.Description = EditContract.Description;
                existing.ExpiryDate = EditContract.ExpiryDate;
                existing.SchoolId = EditContract.SchoolId;
                existing.EmpresaId = EditContract.EmpresaId;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Contrato atualizado com sucesso.";
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
                TempData["SuccessMessage"] = "Contrato eliminado com sucesso.";
            }
            return RedirectToPage();
        }
    }
}
