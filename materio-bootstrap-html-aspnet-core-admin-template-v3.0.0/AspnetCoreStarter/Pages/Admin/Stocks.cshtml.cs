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

        public int CountStockEmpresa { get; set; }
        public int CountStockAgrupamentos { get; set; }
        public int CountStockEmpresasPrivadas { get; set; }

        public List<StockItemViewModel> Inventory { get; set; } = new();
        public List<StockItemViewModel> EmpresaInventory { get; set; } = new();
        public List<AgrupamentoStockGroup> AgrupamentoTree { get; set; } = new();
        public List<EmpresaStockGroup> EmpresasPrivadasTree { get; set; } = new();
        public List<Ticket> PendingRequests { get; set; } = new();
        public List<PedidoStock> EscalatedRequests { get; set; } = new();

        public List<Agrupamento> AvailableAgrupamentos { get; set; } = new();
        public List<Empresa> AvailableEmpresasPrivadas { get; set; } = new();
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
                // Ensure id_empresa column exists for private company stock
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN id_empresa INT NULL;");
            } catch { } // Ignore if exists

            try {
                // Add Foreign Key for id_empresa
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD CONSTRAINT FK_stock_empresa_empresas FOREIGN KEY (id_empresa) REFERENCES empresas(id_empresa);");
            } catch { } // Ignore if exists

            try {
                await _context.Database.ExecuteSqlRawAsync("UPDATE stock_empresa SET status = 'Disponível' WHERE status IS NULL;");
                await _context.Database.ExecuteSqlRawAsync("UPDATE stock_tecnico SET status = 'Disponível' WHERE status IS NULL;");
            } catch { }

            // 1. Base counts (only 2 "types" shown in UI)
            // Stock Empresa = central + com técnico (i.e., stock_empresa rows without agrupamento/escola).
            CountStockEmpresa = await _context.StockEmpresa.CountAsync(s => s.AgrupamentoId == null && s.SchoolId == null);

            // 0. Cleanup: Normalize existing names in database (one-time logic per request is fine here)
            var allStockRecords = await _context.StockEmpresa.Where(s => s.EquipmentName != null).ToListAsync();
            bool dbChanged = false;
            foreach (var s in allStockRecords)
            {
                var norm = NormalizeEquipmentName(s.EquipmentName);
                if (s.EquipmentName != norm) { s.EquipmentName = norm; dbChanged = true; }
            }
            var allEquipInventory = await _context.Equipamentos.Where(e => e.Name != null).ToListAsync();
            foreach (var e in allEquipInventory)
            {
                var norm = NormalizeEquipmentName(e.Name);
                if (e.Name != norm) { e.Name = norm; dbChanged = true; }
            }
            if (dbChanged) await _context.SaveChangesAsync();

            // Load lookup lists for the modal
            AvailableAgrupamentos = await _context.Agrupamentos.ToListAsync();
            AvailableSchools = await _context.Schools.ToListAsync();
            AvailableEmpresasPrivadas = await _context.Empresas.OrderBy(e => e.Name).ToListAsync();
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

            PendingRequests = await _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Admin)
                .Where(t => t.Level == "Empréstimo" && t.Status == "Pedido")
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            // Load PedidoStock escalated to admin (pending only)
            EscalatedRequests = await _context.PedidosStock
                .Include(p => p.Agrupamento)
                .Include(p => p.School)
                .Include(p => p.RequestedBy)
                .Where(p => p.Status == "Pendente_Admin")
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();

            // 2. Aggregate Inventory with Filters
            var rawInventory = new List<StockItemViewModel>();

            string? search = SearchName?.Trim().ToLower();

            // --- Stock Empresa / Agrupamento / Escola / Técnico (from StockEmpresa table) ---
            var empQuery = _context.StockEmpresa
                .Include(s => s.Technician)
                    .ThenInclude(t => t.User)
                .Include(s => s.Agrupamento)
                .Include(s => s.School)
                .Include(s => s.Empresa)
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
                else if (s.EmpresaId != null) { loc = $"stock empresa privada ({s.Empresa?.Name})"; cat = "EmpresaPrivada"; }
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
                    Status = normStatus,
                    TechnicianName = s.Technician?.User?.Username
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
                .GroupBy(i => new { i.Name, i.Type, i.Location, i.Status, i.TechnicianName })
                .Select(g => new StockItemViewModel
                {
                    SampleId = g.First().SampleId,
                    Name = g.Key.Name,
                    Type = g.Key.Type,
                    Status = g.Key.Status,
                    Location = g.Key.Location,
                    Quantity = g.Sum(i => i.Quantity),
                    TechnicianName = g.Key.TechnicianName
                })
                .OrderBy(i => i.Location)
                .ThenBy(i => i.Name)
                .ToList();

            // 4. Split into Empresa (central + técnico) vs Agrupamentos
            EmpresaInventory = Inventory
                .Where(i => i.Location == "Stock Empresa" || i.Location == "com técnico" || i.Status == "Emprestado")
                .ToList();

            // 5. Build Agrupamento tree from equipped rooms
            var agrupamentos = await _context.Agrupamentos
                .Include(a => a.Schools)
                .ToListAsync();

            var allEmpresaStock = await _context.StockEmpresa
                .Include(s => s.Agrupamento)
                .Include(s => s.School)
                .Where(s => s.AgrupamentoId != null || s.SchoolId != null)
                .ToListAsync();

            // Equipamentos registados nas escolas (inventário), para mostrar dentro de cada escola no stock de agrupamentos
            var equippedItems = await _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => e.RoomId != null && e.Room != null && e.Room.Block != null && e.Room.Block.School != null)
                .ToListAsync();

            var equipsBySchool = equippedItems
                .Where(e => e.Room?.Block?.School != null)
                .GroupBy(e => e.Room!.Block!.School!.Id)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(e => new
                        {
                            Name = e.Name ?? "Desconhecido",
                            Type = e.Type ?? "Equipamento",
                            Status = MapEquipamentoStatus(e)
                        })
                        .Select(gg => new StockItemViewModel
                        {
                            Name = gg.Key.Name,
                            Type = gg.Key.Type,
                            Status = gg.Key.Status,
                            Location = "Equipamento",
                            Quantity = gg.Count()
                        })
                        .OrderBy(i => i.Name)
                        .ToList()
                );

            foreach (var agr in agrupamentos)
            {
                var agrGroup = new AgrupamentoStockGroup { AgrupamentoName = agr.Name ?? "Sem Nome", AgrupamentoId = agr.Id };

                // Stock directly at agrupamento level
                var agrDirectStock = allEmpresaStock
                    .Where(s => s.AgrupamentoId == agr.Id && s.SchoolId == null)
                    .GroupBy(s => new { s.EquipmentName, s.Type, Status = NormalizeStatus(s.Status) })
                    .Select(g => new StockItemViewModel {
                        SampleId = g.First().Id,
                        Name = g.Key.EquipmentName,
                        Type = g.Key.Type,
                        Status = g.Key.Status,
                        Location = $"Agrupamento: {agr.Name}",
                        Quantity = g.Count()
                    }).ToList();
                agrGroup.DirectStock = agrDirectStock;

                if (agr.Schools != null)
                {
                    foreach (var esc in agr.Schools)
                    {
                        var escStock = allEmpresaStock
                            .Where(s => s.SchoolId == esc.Id)
                            .GroupBy(s => new { s.EquipmentName, s.Type, Status = NormalizeStatus(s.Status) })
                            .Select(g => new StockItemViewModel {
                                SampleId = g.First().Id,
                                Name = g.Key.EquipmentName,
                                Type = g.Key.Type,
                                Status = g.Key.Status,
                                Location = $"Escola: {esc.Name}",
                                Quantity = g.Count()
                            }).ToList();

                        equipsBySchool.TryGetValue(esc.Id, out var escEquipItems);

                        if (escStock.Any() || (escEquipItems != null && escEquipItems.Any()))
                        {
                            agrGroup.Schools.Add(new EscolaStockGroup {
                                EscolaName = esc.Name ?? "Sem Nome",
                                EscolaId = esc.Id,
                                Items = escStock,
                                EquipmentItems = escEquipItems ?? new List<StockItemViewModel>()
                            });
                        }
                    }
                }

                if (agrGroup.DirectStock.Any() || agrGroup.Schools.Any())
                    AgrupamentoTree.Add(agrGroup);
            }

            // Total count for Stock Agrupamentos (tree)
            CountStockAgrupamentos = AgrupamentoTree.Sum(a =>
                a.DirectStock.Sum(i => i.Quantity) +
                a.Schools.Sum(s => s.Items.Sum(i => i.Quantity) + s.EquipmentItems.Sum(i => i.Quantity))
            );

            // 6. Build Empresas Privadas tree
            var allEmpresaPrivadaStock = await _context.StockEmpresa
                .Include(s => s.Empresa)
                .Where(s => s.EmpresaId != null)
                .ToListAsync();

            var equipsByEmpresa = await _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
                .Include(e => e.Empresa)
                .Where(e => e.EmpresaId != null)
                .ToListAsync();

            var equipsByEmpresaDict = equipsByEmpresa
                .GroupBy(e => e.EmpresaId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(e => new
                        {
                            Name = e.Name ?? "Desconhecido",
                            Type = e.Type ?? "Equipamento",
                            Status = MapEquipamentoStatus(e)
                        })
                        .Select(gg => new StockItemViewModel
                        {
                            Name = gg.Key.Name,
                            Type = gg.Key.Type,
                            Status = gg.Key.Status,
                            Location = "Equipamento (Empresa)",
                            Quantity = gg.Count()
                        })
                        .OrderBy(i => i.Name)
                        .ToList()
                );

            foreach (var emp in AvailableEmpresasPrivadas)
            {
                var empGroup = new EmpresaStockGroup { EmpresaName = emp.Name ?? "Sem Nome", EmpresaId = emp.Id };

                var empDirectStock = allEmpresaPrivadaStock
                    .Where(s => s.EmpresaId == emp.Id)
                    .GroupBy(s => new { s.EquipmentName, s.Type, Status = NormalizeStatus(s.Status) })
                    .Select(g => new StockItemViewModel {
                        SampleId = g.First().Id,
                        Name = g.Key.EquipmentName,
                        Type = g.Key.Type,
                        Status = g.Key.Status,
                        Location = $"Empresa: {emp.Name}",
                        Quantity = g.Count()
                    }).ToList();
                
                empGroup.Items = empDirectStock;

                if (equipsByEmpresaDict.TryGetValue(emp.Id, out var empEquipItems))
                {
                    empGroup.EquipmentItems = empEquipItems;
                }

                if (empGroup.Items.Any() || empGroup.EquipmentItems.Any())
                    EmpresasPrivadasTree.Add(empGroup);
            }

            CountStockEmpresasPrivadas = EmpresasPrivadasTree.Sum(e =>
                e.Items.Sum(i => i.Quantity) + e.EquipmentItems.Sum(i => i.Quantity)
            );

            return Page();
        }

        private static string NormalizeEquipmentName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Desconhecido";
            
            string normalized = name.Trim();
            
            // Map common plurals to singulars (Portuguese)
            var pluralMaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Computadores", "Computador" },
                { "Monitores", "Monitor" },
                { "Impressoras", "Impressora" },
                { "Projetores", "Projetor" },
                { "Televisores", "Televisão" },
                { "Portáteis", "Portátil" },
                { "Ratos", "Rato" },
                { "Teclados", "Teclado" },
                { "UPSs", "UPS" },
                { "Switches", "Switch" },
                { "Quadros Interativos", "Quadro Interativo" },
                { "Mesas Interativas", "Mesa Interativa" }
            };

            if (pluralMaps.TryGetValue(normalized, out var singular))
                return singular;

            // Common suffix replacements if not in map
            if (normalized.EndsWith("ores", StringComparison.OrdinalIgnoreCase)) 
                return normalized.Substring(0, normalized.Length - 2); // Monitores -> Monitor
            if (normalized.EndsWith("adores", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring(0, normalized.Length - 2); // Computadores -> Computador
            if (normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !normalized.EndsWith("ss", StringComparison.OrdinalIgnoreCase) && normalized.Length > 4)
                return normalized.Substring(0, normalized.Length - 1); // Generic plural-s removal, cautious

            return normalized;
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

        private static string MapEquipamentoStatus(Equipamento e)
        {
            var estado = e.StatusEquipamentos?.OrderByDescending(s => s.Id).FirstOrDefault()?.Estado;
            return NormalizeStatus(estado);
        }

        public async Task<IActionResult> OnPostLendAsync(int sampleId, int quantity, int? agrupamentoId, int? schoolId, int? empresaPrivadaId)
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
                item.EmpresaId = empresaPrivadaId;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Emprestado com sucesso!";
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
                item.EmpresaId = null;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Devolvido com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddStockAsync(string name, string type, string description, string locationCategory, int? technicianId, int? agrupamentoId, int? schoolId, int? empresaPrivadaId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(name)) return RedirectToPage();
            if (quantity <= 0) quantity = 1;

            string normalizedName = NormalizeEquipmentName(name);

            for (int i = 0; i < quantity; i++)
            {
                var newStock = new StockEmpresa
                {
                    EquipmentName = normalizedName,
                    Type = type,
                    Description = description,
                    IsAvailable = true,
                    Status = "Disponível"
                };

                // Reset IDs based on category for safety
                if (locationCategory == "Tecnico") newStock.TechnicianId = technicianId;
                else if (locationCategory == "Agrupamento") newStock.AgrupamentoId = agrupamentoId;
                else if (locationCategory == "Escola") newStock.SchoolId = schoolId;
                else if (locationCategory == "EmpresaPrivada") newStock.EmpresaId = empresaPrivadaId;
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

        public async Task<IActionResult> OnPostApproveLoanRequestAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null || ticket.Status != "Pedido") return RedirectToPage();

            int dataStartIndex = ticket.Description?.IndexOf("[DATA:") ?? -1;
            if (dataStartIndex != -1)
            {
                var dataStr = ticket.Description!.Substring(dataStartIndex + 6);
                dataStr = dataStr.Substring(0, dataStr.IndexOf("]"));
                
                var data = System.Text.Json.JsonSerializer.Deserialize<LoanRequestData>(dataStr);
                if (data != null)
                {
                    // Find available items in Central Stock
                    var availableItems = await _context.StockEmpresa
                        .Where(s => s.EquipmentName == data.ItemName && s.Type == data.ItemType)
                        .Where(s => (s.SchoolId == null || s.SchoolId <= 0) && (s.AgrupamentoId == null || s.AgrupamentoId <= 0) && (s.TechnicianId == null || s.TechnicianId <= 0))
                        .Where(s => s.Status == "Disponível" || s.Status == "disponivel" || s.Status == null || s.IsAvailable == true)
                        .Take(data.Quantity)
                        .ToListAsync();

                    if (availableItems.Count < data.Quantity)
                    {
                        TempData["Error"] = $"Stock insuficiente no armazém central. Necessita {data.Quantity}, mas apenas {availableItems.Count} estão disponíveis.";
                        return RedirectToPage();
                    }

                    foreach (var item in availableItems)
                    {
                        item.Status = "Emprestado";
                        item.IsAvailable = false;
                        
                        // If it's a technician requesting, assign directly to them
                        if (data.RequestorRole == "Tecnico" && data.RequestorId > 0)
                        {
                            item.TechnicianId = data.RequestorId;
                            item.AgrupamentoId = null;
                            item.SchoolId = null;
                        }
                        else
                        {
                            item.AgrupamentoId = data.AgrupamentoId > 0 ? data.AgrupamentoId : null;
                        }
                    }

                    ticket.Status = "Concluído";
                    ticket.Description += "\n\n[RESOLVIDO: PEDIDO APROVADO - Equipamentos transferidos automaticamente.]";
                    await _context.SaveChangesAsync();
                    
                    if (data.RequestorRole == "Tecnico")
                        TempData["Success"] = "Pedido de Stock Aprovado! Os equipamentos foram atribuídos à mala do técnico.";
                    else
                        TempData["Success"] = "Pedido de Empréstimo Aprovado! Os equipamentos foram transferidos para o Agrupamento.";
                    
                    return RedirectToPage();
                }
            }

            TempData["Error"] = "Formato de Pedido Inválido.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectLoanRequestAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return RedirectToPage();
            
            ticket.Status = "Recusado";
            ticket.Description += "\n\n[RESOLVIDO: PEDIDO RECUSADO pelo Administrador.]";
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Pedido de Empréstimo Recusado com sucesso.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFulfillEscalatedRequestAsync(int requestId, int[] selectedStockIds)
        {
            var pedido = await _context.PedidosStock
                .Include(p => p.School)
                .Include(p => p.RequestedBy)
                .FirstOrDefaultAsync(p => p.Id == requestId);

            if (pedido == null || pedido.Status != "Pendente_Admin") return RedirectToPage();

            var items = await _context.StockEmpresa
                .Where(s => selectedStockIds.Contains(s.Id))
                .ToListAsync();

            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? currentAdminId = int.TryParse(userIdStr, out int id) ? id : null;

            pedido.AdminId = currentAdminId;

            foreach (var item in items)
            {
                item.SchoolId = pedido.SchoolId;
                item.AgrupamentoId = pedido.AgrupamentoId;
                item.EmpresaId = pedido.RequestedBy?.EmpresaId;
                item.Status = "Emprestado";
                item.IsAvailable = false;
                item.AdminId = currentAdminId;
            }

            pedido.Status = "Atendido";
            pedido.UpdatedAt = DateTime.UtcNow;
            pedido.DirectorNotes = (pedido.DirectorNotes ?? "") + $" | Atendido pelo Admin em {DateTime.Now:dd/MM/yyyy}.";

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Pedido #{requestId} atendido! {items.Count} item(s) enviados para {pedido.School?.Name}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectEscalatedRequestAsync(int requestId)
        {
            var pedido = await _context.PedidosStock.FindAsync(requestId);
            if (pedido == null) return RedirectToPage();

            pedido.Status = "Recusado";
            pedido.DirectorNotes = (pedido.DirectorNotes ?? "") + $" | Recusado pelo Admin em {DateTime.Now:dd/MM/yyyy}.";
            pedido.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Pedido recusado.";
            return RedirectToPage();
        }
    }

    public class LoanRequestData 
    {
        public string? ItemName { get; set; }
        public string? ItemType { get; set; }
        public int Quantity { get; set; }
        public int AgrupamentoId { get; set; }
        public int RequestorId { get; set; }
        public string? RequestorRole { get; set; }
    }

    public class StockItemViewModel
    {
        public int SampleId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public int Quantity { get; set; }
        public string? TechnicianName { get; set; }
    }

    public class AgrupamentoStockGroup
    {
        public int AgrupamentoId { get; set; }
        public string AgrupamentoName { get; set; } = "";
        public List<StockItemViewModel> DirectStock { get; set; } = new();
        public List<EscolaStockGroup> Schools { get; set; } = new();
    }

    public class EscolaStockGroup
    {
        public int EscolaId { get; set; }
        public string EscolaName { get; set; } = "";
        public List<StockItemViewModel> Items { get; set; } = new();
        public List<StockItemViewModel> EquipmentItems { get; set; } = new();
    }

    public class EmpresaStockGroup
    {
        public int EmpresaId { get; set; }
        public string EmpresaName { get; set; } = "";
        public List<StockItemViewModel> Items { get; set; } = new();
        public List<StockItemViewModel> EquipmentItems { get; set; } = new();
    }
}
