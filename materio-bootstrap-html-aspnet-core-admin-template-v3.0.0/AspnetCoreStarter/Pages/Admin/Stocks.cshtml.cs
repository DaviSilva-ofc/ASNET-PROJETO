using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;

namespace AspnetCoreStarter.Pages.Admin
{
    public class StocksModel : PageModel
    {
        private readonly AppDbContext _context;

        public StocksModel(AppDbContext context)
        {
            _context = context;
        }

        public int CountAgrupamentos { get; set; }
        public int CountEscolas { get; set; }
        public int CountTecnicos { get; set; }
        public int CountEmpresa { get; set; }

        public List<StockItemViewModel> RecentItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin") return RedirectToPage("/Index");

            // 1. Calculations (Simplified for now)
            // Agrupamentos: Items in rooms that belong to schools with agrupamento
            CountAgrupamentos = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .CountAsync(e => e.Room != null && e.Room.Block != null && e.Room.Block.School != null && e.Room.Block.School.AgrupamentoId != null);

            // Escolas: Items in rooms that belong to schools
            CountEscolas = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                .CountAsync(e => e.Room != null && e.Room.Block != null && e.Room.Block.SchoolId != 0);

            // Tecnicos: Items in StockTecnico
            CountTecnicos = await _context.StockTecnico.CountAsync();

            // Empresa: Items in StockEmpresa
            CountEmpresa = await _context.StockEmpresa.CountAsync();

            // 2. Load recent items for the table
            var empStock = await _context.StockEmpresa.Take(5).Select(s => new StockItemViewModel {
                Name = s.EquipmentName ?? "Sem Nome",
                Category = s.Type ?? "Empresa",
                Location = "Stock Central",
                Quantity = 1,
                Status = s.IsAvailable ? "Disponível" : "Em uso"
            }).ToListAsync();

            var techStock = await _context.StockTecnico.Take(5).Select(s => new StockItemViewModel {
                Name = s.EquipmentName ?? "Sem Nome",
                Category = "Técnico",
                Location = "Com Técnico",
                Quantity = 1,
                Status = s.IsAvailable ? "Disponível" : "Em uso"
            }).ToListAsync();

            RecentItems.AddRange(empStock);
            RecentItems.AddRange(techStock);

            return Page();
        }
    }

    public class StockItemViewModel
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
    }
}
