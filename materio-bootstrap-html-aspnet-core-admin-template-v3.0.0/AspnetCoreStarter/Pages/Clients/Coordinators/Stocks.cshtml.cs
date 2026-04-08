using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace AspnetCoreStarter.Pages.Clients.Coordinators
{
    public class CoordinatorStocksModel : PageModel
    {
        private readonly AppDbContext _context;

        public CoordinatorStocksModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? FilterEscolaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterSalaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterArticle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public int CountEscolas { get; set; }
        public int CountSalas { get; set; }
        public int CountEquipamentos { get; set; }
        public int CountTickets { get; set; }
        
        public string? SuccessMessage { get; set; }

        public List<School> AvailableSchools { get; set; } = new();
        public List<Sala> AvailableRooms { get; set; } = new();
        public List<string> UniqueStatuses { get; set; } = new();

        public List<CoordinatorStockTableItemViewModel> Items { get; set; } = new();
        public List<PedidoStock> MyRequests { get; set; } = new();
        public List<CoordinatorBorrowedItemViewModel> BorrowedItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (coord == null || coord.SchoolId == null) return Page();

            if (Request.Query.ContainsKey("success"))
            {
                SuccessMessage = Request.Query["success"];
            }

            int mySchoolId = coord.SchoolId.Value;

            // Ensure pedidos_stock table exists
            try { await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS pedidos_stock (
                    id_pedido INT AUTO_INCREMENT PRIMARY KEY,
                    nome_artigo VARCHAR(100) NOT NULL,
                    tipo_artigo VARCHAR(100),
                    quantidade INT NOT NULL DEFAULT 1,
                    notas TEXT,
                    id_coordenador INT,
                    id_escola INT,
                    id_agrupamento INT,
                    status VARCHAR(50) DEFAULT 'Pendente_Diretor',
                    notas_diretor TEXT,
                    data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP,
                    data_atualizacao DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    FOREIGN KEY (id_coordenador) REFERENCES utilizadores(id_utilizador),
                    FOREIGN KEY (id_escola) REFERENCES escolas(id_escola),
                    FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento)
                ) ENGINE=InnoDB;"); } catch { }

            // Load past requests by this coordinator for this school (hide fulfilled ones as they move to Borrowed)
            MyRequests = await _context.PedidosStock
                .Where(p => p.SchoolId == mySchoolId && p.Status != "Atendido")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            AvailableSchools = await _context.Schools.Where(s => s.Id == mySchoolId).ToListAsync();
            CountEscolas = 1;

            var blocks = await _context.Blocos.Where(b => b.SchoolId == mySchoolId).ToListAsync();
            var blockIds = blocks.Select(b => b.Id).ToList();

            AvailableRooms = await _context.Salas.Where(r => blockIds.Contains(r.BlockId)).ToListAsync();
            var roomIds = AvailableRooms.Select(r => r.Id).ToList();
            CountSalas = AvailableRooms.Count;

            var equips = await _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                .Where(e => e.RoomId.HasValue && roomIds.Contains(e.RoomId.Value))
                .ToListAsync();
            CountEquipamentos = equips.Count;

            CountTickets = await _context.Tickets.CountAsync(t => t.SchoolId.HasValue && t.SchoolId.Value == mySchoolId);

            var baseQuery = _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => e.RoomId.HasValue && roomIds.Contains(e.RoomId.Value));

            var equipsQuery = baseQuery.AsQueryable();

            if (FilterSalaId.HasValue) equipsQuery = equipsQuery.Where(e => e.RoomId == FilterSalaId.Value);
            if (!string.IsNullOrEmpty(FilterType)) equipsQuery = equipsQuery.Where(e => e.Type == FilterType);
            
            var equipsTemp = await equipsQuery.ToListAsync();

            if (!string.IsNullOrEmpty(FilterArticle))
            {
                equipsTemp = equipsTemp.Where(e => NormalizeEquipmentName(e.Name) == FilterArticle).ToList();
            }

            UniqueStatuses = new List<string> { "A funcionar", "Armazenado", "Avariado", "Em reparo" };

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                equipsTemp = equipsTemp.Where(e => MapStatus(e) == FilterStatus).ToList();
            }

            var groupedItems = equipsTemp.GroupBy(e => new {
                Name = NormalizeEquipmentName(e.Name),
                Category = e.Type ?? "Equipamento",
                Location = $"{e.Room?.Name} ({e.Room?.Block?.School?.Name})",
                RoomId = e.RoomId,
                Status = MapStatus(e)
            });

            Items = groupedItems.Select(g => new CoordinatorStockTableItemViewModel {
                Name = g.Key.Name,
                Category = g.Key.Category,
                Location = g.Key.Location,
                RoomId = g.Key.RoomId,
                Quantity = g.Count(),
                Status = g.Key.Status
            }).OrderBy(i => i.Location).ThenBy(i => i.Name).ToList();

            // Load items borrowed from the Agrupamento
            var borrowedCentralStock = await _context.StockEmpresa
                .Where(s => s.SchoolId == mySchoolId && s.Status == "Emprestado")
                .ToListAsync();

            var borrowedEquips = await _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block)
                .Include(e => e.StatusEquipamentos)
                .Where(e => e.Room != null && e.Room.Block.SchoolId == mySchoolId)
                .ToListAsync();

            var onlyBorrowedEquips = borrowedEquips
                .Where(e => {
                    var lastStatus = e.StatusEquipamentos?.OrderByDescending(s => s.Id).FirstOrDefault()?.Estado ?? "";
                    return lastStatus.Contains("Emprestado", StringComparison.OrdinalIgnoreCase);
                }).ToList();

            BorrowedItems = borrowedCentralStock.Select(s => new CoordinatorBorrowedItemViewModel
            {
                IdWithPrefix = $"se_{s.Id}",
                Name = s.EquipmentName,
                Type = s.Type,
                Details = (s.AdminId != null || s.AgrupamentoId == null || s.AgrupamentoId != coord.School?.AgrupamentoId) 
                          ? "Emprestado pelo Administrador" 
                          : "Do Stock do Agrupamento",
                IsAdminLoan = (s.AdminId != null || s.AgrupamentoId == null || s.AgrupamentoId != coord.School?.AgrupamentoId)
            }).Concat(onlyBorrowedEquips.Select(e => new CoordinatorBorrowedItemViewModel
            {
                IdWithPrefix = $"eq_{e.Id}",
                Name = NormalizeEquipmentName(e.Name),
                Type = e.Type,
                Details = $"Originalmente na sala {e.Room?.Name}",
                IsAdminLoan = false // Equipment is always school/agrupamento level in this context
            })).ToList();

            return Page();
        }

        private string NormalizeEquipmentName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Desconhecido";
            string normalized = name.Trim();
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

            if (pluralMaps.TryGetValue(normalized, out var singular)) return singular;

            if (normalized.EndsWith("ores", StringComparison.OrdinalIgnoreCase)) return normalized.Substring(0, normalized.Length - 2);
            if (normalized.EndsWith("adores", StringComparison.OrdinalIgnoreCase)) return normalized.Substring(0, normalized.Length - 2);
            if (normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !normalized.EndsWith("ss", StringComparison.OrdinalIgnoreCase) && normalized.Length > 4)
                return normalized.Substring(0, normalized.Length - 1);

            return normalized;
        }

        private string MapStatus(Equipamento e)
        {
            var estado = e.StatusEquipamentos?.OrderByDescending(s => s.Id).FirstOrDefault()?.Estado ?? "";

            return estado.ToLower() switch
            {
                var s when s.Contains("emprestado") => "Emprestado",
                "disponível" or "disponivel" or "armazenado" => "Armazenado",
                "a funcionar" or "funcionando" or "em uso" => "A funcionar",
                "avariado" or "indisponível" or "indisponivel" or "recolhido" => "Avariado",
                "em reparo" or "reparo" or "em manutenção" => "Em reparo",
                _ => "Armazenado"
            };
        }

        public async Task<IActionResult> OnPostCreateStockRequestAsync(string? itemName, string? itemType, int quantity, string? notes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var coord = await _context.Coordenadores
                .Include(d => d.School)
                    .ThenInclude(s => s.Agrupamento)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (coord?.SchoolId == null || string.IsNullOrWhiteSpace(itemName)) return RedirectToPage();

            var pedido = new PedidoStock
            {
                ItemName = itemName.Trim(),
                ItemType = itemType,
                Quantity = Math.Max(1, quantity),
                Notes = notes,
                RequestedByUserId = userId,
                SchoolId = coord.SchoolId,
                AgrupamentoId = coord.School?.AgrupamentoId,
                Status = "Pendente_Diretor",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PedidosStock.Add(pedido);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = $"Pedido de '{itemName}' enviado ao Diretor com sucesso!" });
        }


        public async Task<IActionResult> OnPostEditStatusAsync(string name, int? roomId, string currentStatus, string newStatus, int quantity)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var coord = await _context.Coordenadores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (coord == null || coord.SchoolId == null) return RedirectToPage();

            int mySchoolId = coord.SchoolId.Value;

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block)
                .Where(e => e.Room != null && e.Room.Block.SchoolId == mySchoolId);

            if (roomId.HasValue)
                query = query.Where(e => e.RoomId == roomId.Value);
            else
                query = query.Where(e => e.RoomId == null);

            var items = await query.ToListAsync();
            var itemsToUpdate = items
                .Where(e => NormalizeEquipmentName(e.Name).Equals(name, StringComparison.OrdinalIgnoreCase) && 
                            MapStatus(e).Equals(currentStatus ?? "", StringComparison.OrdinalIgnoreCase))
                .Take(quantity)
                .ToList();

            foreach(var item in itemsToUpdate)
            {
                if (newStatus == "Armazenado" || newStatus == "Disponível") item.Status = "Disponível";
                else item.Status = newStatus;
            }
            
            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Estado atualizado com sucesso!" });
        }

        public async Task<IActionResult> OnPostDeleteStockAsync(string name, int? roomId, string currentStatus, int quantity)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var coord = await _context.Coordenadores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (coord == null || coord.SchoolId == null) return RedirectToPage();

            int mySchoolId = coord.SchoolId.Value;

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block)
                .Where(e => e.Room != null && e.Room.Block.SchoolId == mySchoolId);

            if (roomId.HasValue)
                query = query.Where(e => e.RoomId == roomId.Value);
            else
                query = query.Where(e => e.RoomId == null);

            var items = await query.ToListAsync();
            var itemsToDelete = items
                .Where(e => NormalizeEquipmentName(e.Name).Equals(name, StringComparison.OrdinalIgnoreCase) && 
                            MapStatus(e).Equals(currentStatus ?? "", StringComparison.OrdinalIgnoreCase))
                .Take(quantity)
                .ToList();

            _context.Equipamentos.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();
            
            return RedirectToPage(new { success = "Equipamentos excluídos com sucesso!" });
        }
        public async Task<IActionResult> OnPostReturnItemAsync(string idWithPrefix)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var coord = await _context.Coordenadores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (coord == null) return RedirectToPage();

            string message = "Artigo devolvido com sucesso!";

            if (idWithPrefix.StartsWith("se_"))
            {
                int id = int.Parse(idWithPrefix.Substring(3));
                var item = await _context.StockEmpresa.FindAsync(id);
                if (item != null && item.SchoolId == coord.SchoolId)
                {
                    bool wasAdminLoan = (item.AdminId != null || item.AgrupamentoId == null || item.AgrupamentoId != coord.School?.AgrupamentoId);

                    // Find the related "Atendido" request by matching school and a case-insensitive name check
                    var allAtendidoForSchool = await _context.PedidosStock
                        .Where(p => p.SchoolId == coord.SchoolId && p.Status == "Atendido")
                        .ToListAsync();

                    var matchingRequest = allAtendidoForSchool
                        .FirstOrDefault(p => string.Equals(p.ItemName, item.EquipmentName, StringComparison.OrdinalIgnoreCase)
                                          || (item.EquipmentName != null && p.ItemName.Contains(item.EquipmentName, StringComparison.OrdinalIgnoreCase))
                                          || (item.EquipmentName != null && item.EquipmentName.Contains(p.ItemName, StringComparison.OrdinalIgnoreCase)));

                    if (matchingRequest != null)
                    {
                        matchingRequest.Quantity -= 1;
                        if (matchingRequest.Quantity <= 0)
                        {
                            _context.PedidosStock.Remove(matchingRequest);
                        }
                    }
                    else
                    {
                        // Fallback: check if no more borrowed items exist for this school; if so, wipe all Atendido records
                        var remainingBorrowedFromSchool = await _context.StockEmpresa
                            .CountAsync(s => s.SchoolId == coord.SchoolId && s.Status == "Emprestado" && s.Id != item.Id);
                        if (remainingBorrowedFromSchool == 0)
                        {
                            _context.PedidosStock.RemoveRange(allAtendidoForSchool);
                        }
                    }

                    item.SchoolId = null;
                    if (wasAdminLoan) {
                        item.AgrupamentoId = null; 
                        item.AdminId = null;
                    }
                    
                    item.Status = "Disponível";
                    item.IsAvailable = true;
                    message = "Artigo devolvido com sucesso!";
                }
            }
            else if (idWithPrefix.StartsWith("eq_"))
            {
                int id = int.Parse(idWithPrefix.Substring(3));
                var item = await _context.Equipamentos
                    .Include(e => e.Room).ThenInclude(r => r.Block)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (item != null && item.Room?.Block.SchoolId == coord.SchoolId)
                {
                    item.Status = "Disponível";
                    var statusChange = new StatusEquipamento
                    {
                        EquipamentoId = item.Id,
                        Estado = "Disponível"
                    };
                    _context.StatusEquipamentos.Add(statusChange);
                    message = "Artigo devolvido com sucesso!";
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = message });
        }
    }

    public class CoordinatorBorrowedItemViewModel
    {
        public string? IdWithPrefix { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Details { get; set; }
        public bool IsAdminLoan { get; set; }
    }

    public class CoordinatorStockTableItemViewModel
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int? RoomId { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
    }
}
