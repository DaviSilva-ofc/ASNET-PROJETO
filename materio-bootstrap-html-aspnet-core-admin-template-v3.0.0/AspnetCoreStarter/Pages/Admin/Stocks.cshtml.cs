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
        public List<string> ExistingEquipmentNames { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin") return RedirectToPage("/Index");

            // 0. Schema Maintenance (Ensure 'status' column exists - MySQL compatible)
            try {
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN status VARCHAR(50) DEFAULT 'Disponível';");
            } catch { } // Ignore if exists
            
            try {
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_tecnico ADD COLUMN status VARCHAR(50) DEFAULT 'Disponível';");
            } catch { } // Ignore if exists

            try {
                await _context.Database.ExecuteSqlRawAsync("UPDATE stock_empresa SET status = 'Disponível' WHERE status IS NULL;");
                await _context.Database.ExecuteSqlRawAsync("UPDATE stock_tecnico SET status = 'Disponível' WHERE status IS NULL;");
            } catch { }

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

            ExistingEquipmentNames = await _context.StockEmpresa
                .Where(s => s.EquipmentName != null)
                .Select(s => s.EquipmentName!)
                .Union(_context.Equipamentos.Where(e => e.Name != null).Select(e => e.Name!))
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            // 2. Aggregate Inventory with Filters
            var rawInventory = new List<StockItemViewModel>();

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
                string normStatus = NormalizeStatus(s.Status);
                string loc = "Stock Empresa";
                string cat = "Empresa";
                if (s.AgrupamentoId != null) { loc = $"stock agrupamento ({s.Agrupamento?.Name})"; cat = "Agrupamento"; }
                else if (s.SchoolId != null) { loc = $"stock escola ({s.School?.Name})"; cat = "Escola"; }
                else if (s.TechnicianId != null || s.Technician != null) { loc = "com técnico"; cat = "Tecnico"; }

                // Category Filter
                if (!string.IsNullOrEmpty(FilterCategory))
                {
                    if (FilterCategory == "Empresa")
                    {
                        if (cat != "Empresa" && normStatus != "Emprestado") continue;
                    }
                    else if (cat != FilterCategory) continue;
                }

                // Status Filter
                if (!string.IsNullOrEmpty(FilterStatus) && normStatus != FilterStatus) continue;

                rawInventory.Add(new StockItemViewModel {
                    SampleId = s.Id,
                    Name = s.EquipmentName,
                    Type = s.Type,
                    Location = loc,
                    Quantity = 1,
                    Status = normStatus
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
                if (string.IsNullOrEmpty(FilterType) || FilterType == "Outro") {
                    var techRecords = await techQuery.ToListAsync();
                    foreach (var s in techRecords)
                    {
                        string normStatus = NormalizeStatus(s.Status);
                        if (!string.IsNullOrEmpty(FilterStatus) && normStatus != FilterStatus) continue;

                        rawInventory.Add(new StockItemViewModel {
                            Name = s.EquipmentName,
                            Type = "Outro (Técnico)",
                            Location = "com técnico",
                            Quantity = 1,
                            Status = normStatus
                        });
                    }
                }
            }

            // --- Stock Agrupamento/Escola (from Equipamentos in rooms) ---
            var roomQuery = _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
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

                string normStatus = NormalizeStatus(item.Status);
                if (!string.IsNullOrEmpty(FilterStatus) && normStatus != FilterStatus) continue;

                rawInventory.Add(new StockItemViewModel {
                    Name = item.Name,
                    Type = item.Type,
                    Location = loc,
                    Quantity = 1,
                    Status = normStatus
                });
            }

            // 3. Group by identical properties and sum quantity
            Inventory = rawInventory
                .GroupBy(i => new { i.Name, i.Type, i.Location, i.Status })
                .Select(g => new StockItemViewModel
                {
                    SampleId = g.First().SampleId,
                    Name = g.Key.Name,
                    Type = g.Key.Type,
                    Status = g.Key.Status,
                    Location = g.Key.Location,
                    Quantity = g.Sum(i => i.Quantity)
                })
                .OrderBy(i => i.Location)
                .ThenBy(i => i.Name)
                .ToList();

            return Page();
        }

        private static string NormalizeStatus(string? raw)
        {
            return (raw ?? "").ToLower() switch
            {
                "disponível" or "disponivel" or "funcionando" => "Disponível",
                "emprestado" => "Emprestado",
                "em uso" => "Em uso",
                "avariado" or "indisponível" or "indisponivel" or "recolhido" => "Indisponível",
                _ => "Disponível"
            };
        }

        public async Task<IActionResult> OnPostLendAsync(int sampleId, int quantity, int? agrupamentoId, int? schoolId)
        {
            if (quantity <= 0) return RedirectToPage();

            var sample = await _context.StockEmpresa.FindAsync(sampleId);
            if (sample == null)
            {
                TempData["Error"] = "Equipamento de referência não encontrado.";
                return RedirectToPage();
            }

            // Find available items matching the EXACT name and type from the database
            var availableItems = await _context.StockEmpresa
                .Where(s => s.EquipmentName == sample.EquipmentName && s.Type == sample.Type)
                .Where(s => (s.SchoolId == null || s.SchoolId <= 0) && (s.AgrupamentoId == null || s.AgrupamentoId <= 0) && (s.TechnicianId == null || s.TechnicianId <= 0))
                .Where(s => s.Status == "Disponível" || s.Status == "disponivel" || s.Status == null || s.IsAvailable == true)
                .Take(quantity)
                .ToListAsync();

            if (availableItems.Count == 0)
            {
                TempData["Error"] = "Nenhum equipamento disponível encontrado para emprestar.";
                return RedirectToPage();
            }

            foreach (var item in availableItems)
            {
                item.Status = "Emprestado";
                item.IsAvailable = false;
                item.AgrupamentoId = agrupamentoId;
                item.SchoolId = schoolId;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sucesso: {availableItems.Count} item(ns) emprestado(s).";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReturnAsync(int sampleId, int quantity)
        {
            if (quantity <= 0) return RedirectToPage();

            var sample = await _context.StockEmpresa.FindAsync(sampleId);
            if (sample == null)
            {
                TempData["Error"] = "Equipamento de referência não encontrado.";
                return RedirectToPage();
            }

            // Find lent items matching the criteria
            var lentItems = await _context.StockEmpresa
                .Where(s => s.EquipmentName == sample.EquipmentName && s.Type == sample.Type)
                .Where(s => s.Status == "Emprestado")
                .Take(quantity)
                .ToListAsync();

            if (lentItems.Count == 0)
            {
                TempData["Error"] = "Nenhum equipamento emprestado encontrado para devolução.";
                return RedirectToPage();
            }

            foreach (var item in lentItems)
            {
                item.Status = "Disponível";
                item.IsAvailable = true;
                item.AgrupamentoId = null;
                item.SchoolId = null;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sucesso: {lentItems.Count} item(ns) devolvido(s).";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddStockAsync(string name, string type, string description, string locationCategory, int? technicianId, int? agrupamentoId, int? schoolId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(name)) return RedirectToPage();
            if (quantity <= 0) quantity = 1;

            for (int i = 0; i < quantity; i++)
            {
                var newStock = new StockEmpresa
                {
                    EquipmentName = name,
                    Type = type,
                    Description = description,
                    IsAvailable = true,
                    Status = "Disponível"
                };

                // Reset IDs based on category for safety
                if (locationCategory == "Tecnico") newStock.TechnicianId = technicianId;
                else if (locationCategory == "Agrupamento") newStock.AgrupamentoId = agrupamentoId;
                else if (locationCategory == "Escola") newStock.SchoolId = schoolId;
                // "Empresa" means all are null

                _context.StockEmpresa.Add(newStock);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteStockAsync(int sampleId, int quantity)
        {
            if (quantity <= 0) return RedirectToPage();

            var sample = await _context.StockEmpresa.FindAsync(sampleId);
            if (sample == null) return RedirectToPage();

            // Find matching items in the SAME location as the sample
            var query = _context.StockEmpresa
                .Where(s => s.EquipmentName == sample.EquipmentName && s.Type == sample.Type && s.AgrupamentoId == sample.AgrupamentoId && s.SchoolId == sample.SchoolId && s.TechnicianId == sample.TechnicianId);

            var itemsToDelete = await query.Take(quantity).ToListAsync();
            _context.StockEmpresa.RemoveRange(itemsToDelete);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sucesso: {itemsToDelete.Count} item(ns) excluído(s).";
            return RedirectToPage();
        }
    }

    public class StockItemViewModel
    {
        public int SampleId { get; set; } // Reference ID for lookups
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public int Quantity { get; set; }
    }
}
