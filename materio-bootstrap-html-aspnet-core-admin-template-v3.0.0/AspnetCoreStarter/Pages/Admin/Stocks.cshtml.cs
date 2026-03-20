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

        public List<StockItemViewModel> Inventory { get; set; } = new();

        public List<Agrupamento> AvailableAgrupamentos { get; set; } = new();
        public List<School> AvailableSchools { get; set; } = new();
        public List<User> AvailableTecnicos { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterCategory { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin") return RedirectToPage("/Index");

            // 1. Calculations (Fixed to reflect actual DB state)
            CountAgrupamentos = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .CountAsync(e => e.Room != null && e.Room.Block != null && e.Room.Block.School != null && e.Room.Block.School.AgrupamentoId != null);

            CountEscolas = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                .CountAsync(e => e.Room != null && e.Room.Block != null && e.Room.Block.SchoolId != 0);

            CountTecnicos = await _context.StockTecnico.CountAsync();
            CountEmpresa = await _context.StockEmpresa.CountAsync();

            // Load lookup lists for the modal
            AvailableAgrupamentos = await _context.Agrupamentos.ToListAsync();
            AvailableSchools = await _context.Schools.ToListAsync();
            AvailableTecnicos = await _context.Users
                .Join(_context.Tecnicos, u => u.Id, t => t.UserId, (u, t) => u)
                .ToListAsync();

            // 2. Aggregate Inventory with Filters
            Inventory = new List<StockItemViewModel>();

            string? search = SearchName?.Trim().ToLower();

            // --- Stock Empresa / Agrupamento / Escola / Técnico (from StockEmpresa table) ---
            var empQuery = _context.StockEmpresa
                .Include(s => s.Technician)
                .Include(s => s.Agrupamento)
                .Include(s => s.School)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                empQuery = empQuery.Where(s => (s.EquipmentName != null && s.EquipmentName.ToLower().Contains(search)) 
                                            || (s.Description != null && s.Description.ToLower().Contains(search)));
            if (!string.IsNullOrEmpty(FilterType))
                empQuery = empQuery.Where(s => s.Type == FilterType);
            
            var empStockRecords = await empQuery.ToListAsync();

            foreach (var s in empStockRecords)
            {
                string loc = "Stock Empresa";
                string cat = "Empresa";
                if (s.AgrupamentoId != null) { loc = $"stock agrupamento ({s.Agrupamento?.Name})"; cat = "Agrupamento"; }
                else if (s.SchoolId != null) { loc = $"stock escola ({s.School?.Name})"; cat = "Escola"; }
                else if (s.TechnicianId != null) { loc = "com técnico"; cat = "Tecnico"; }

                if (!string.IsNullOrEmpty(FilterCategory) && cat != FilterCategory) continue;

                Inventory.Add(new StockItemViewModel {
                    Name = s.EquipmentName,
                    Type = s.Type,
                    Location = loc,
                    Quantity = 1,
                    Status = s.IsAvailable ? "Disponível" : "Em uso"
                });
            }

            // --- Stock Técnico (from StockTecnico table) ---
            if (string.IsNullOrEmpty(FilterCategory) || FilterCategory == "Tecnico")
            {
                var techQuery = _context.StockTecnico.AsQueryable();
                if (!string.IsNullOrEmpty(search))
                    techQuery = techQuery.Where(s => s.EquipmentName != null && s.EquipmentName.ToLower().Contains(search));
                
                // StockTecnico doesn't have a Type property, so we skip Type filtering here.
                // If the user is filtering by Type, we probably should either return nothing 
                // OR only return these if Type is something related.
                // For now, let's just only show these if FilterType is not set.
                if (string.IsNullOrEmpty(FilterType)) {
                    var techStock = await techQuery.Select(s => new StockItemViewModel {
                        Name = s.EquipmentName,
                        Type = "Técnico",
                        Location = "com técnico",
                        Quantity = 1,
                        Status = s.IsAvailable ? "Disponível" : "Em uso"
                    }).ToListAsync();
                    Inventory.AddRange(techStock);
                }
            }

            // --- Stock Agrupamento/Escola (from Equipamentos in rooms) ---
            var roomQuery = _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                            .ThenInclude(s => s.Agrupamento)
                .Where(e => e.RoomId != null)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                roomQuery = roomQuery.Where(e => (e.Name != null && e.Name.ToLower().Contains(search))
                                              || (e.Brand != null && e.Brand.ToLower().Contains(search))
                                              || (e.Model != null && e.Model.ToLower().Contains(search)));
            if (!string.IsNullOrEmpty(FilterType))
                roomQuery = roomQuery.Where(e => e.Type == FilterType);

            var roomStock = await roomQuery.ToListAsync();

            foreach (var item in roomStock)
            {
                string loc = "Equipamento";
                string cat = "Escola";
                if (item.Room?.Block?.School?.Agrupamento != null) { loc = $"stock agrupamento ({item.Room.Block.School.Agrupamento.Name})"; cat = "Agrupamento"; }
                else if (item.Room?.Block?.School != null) { loc = $"stock escola ({item.Room.Block.School.Name})"; cat = "Escola"; }

                if (!string.IsNullOrEmpty(FilterCategory) && cat != FilterCategory) continue;

                Inventory.Add(new StockItemViewModel {
                    Name = item.Name,
                    Type = item.Type,
                    Location = loc,
                    Quantity = 1,
                    Status = item.Status ?? "Funcionando"
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddStockAsync(string name, string type, string description, string locationCategory, int? technicianId, int? agrupamentoId, int? schoolId)
        {
            if (string.IsNullOrEmpty(name)) return RedirectToPage();

            var newStock = new StockEmpresa
            {
                EquipmentName = name,
                Type = type,
                Description = description,
                IsAvailable = true
            };

            // Reset IDs based on category for safety
            if (locationCategory == "Tecnico") newStock.TechnicianId = technicianId;
            else if (locationCategory == "Agrupamento") newStock.AgrupamentoId = agrupamentoId;
            else if (locationCategory == "Escola") newStock.SchoolId = schoolId;
            // "Empresa" means all are null

            _context.StockEmpresa.Add(newStock);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }

    public class StockItemViewModel
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Location { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
    }
}
