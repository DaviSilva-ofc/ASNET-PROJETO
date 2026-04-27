using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorStocksModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorStocksModel(AppDbContext context)
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

        public List<DirectorStockItemViewModel> Items { get; set; } = new();
        public List<PedidoStock> PendingSchoolRequests { get; set; } = new();
        public List<DirectorLendingItemViewModel> StoredItemsForLending { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Find director's agrupamento
            var director = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (director == null || director.AgrupamentoId == null)
            {
                return Page();
            }

            if (Request.Query.ContainsKey("success"))
            {
                SuccessMessage = Request.Query["success"];
            }

            int agrId = director.AgrupamentoId.Value;

            // Load pending requests from coordinators in this agrupamento (pending director or escalated to admin)
            PendingSchoolRequests = await _context.PedidosStock
                .Include(p => p.School)
                .Include(p => p.RequestedBy)
                .Where(p => p.AgrupamentoId == agrId && (p.Status == "Pendente_Diretor" || p.Status == "Pendente_Admin"))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Load stored stock items available in this agrupamento from both sources
            var centralStock = await _context.StockEmpresa
                .Include(s => s.School)
                .Where(s => s.AgrupamentoId == agrId && (s.Status == "Armazenado" || s.Status == "Disponível") && s.IsAvailable)
                .ToListAsync();

            var storedEquipments = await _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School)
                .Include(e => e.StatusEquipamentos)
                .Where(e => e.Room != null && e.Room.Block.School.AgrupamentoId == agrId)
                .ToListAsync();

            // Filter equipments using the MapStatus logic to find "Armazenado" items
            var onlyArmazenadoEquips = storedEquipments
                .Where(e => MapStatus(e) == "Armazenado")
                .ToList();

            StoredItemsForLending = centralStock.Select(s => new DirectorLendingItemViewModel
            {
                IdWithPrefix = $"se_{s.Id}",
                Name = s.EquipmentName,
                Type = s.Type,
                Source = "Stock Central",
                CurrentLocation = s.School?.Name ?? "Agrupamento"
            }).Concat(onlyArmazenadoEquips.Select(e => new DirectorLendingItemViewModel
            {
                IdWithPrefix = $"eq_{e.Id}",
                Name = NormalizeEquipmentName(e.Name),
                Type = e.Type,
                Source = "Armazenado em Sala",
                CurrentLocation = e.Room?.Name ?? "Desconhecido"
            })).ToList();

            // Stats for the director
            AvailableSchools = await _context.Schools.Where(s => s.AgrupamentoId == agrId).ToListAsync();
            var schoolIds = AvailableSchools.Select(s => s.Id).ToList();
            
            if (FilterEscolaId.HasValue)
            {
                schoolIds = new List<int> { FilterEscolaId.Value };
            }

            CountEscolas = AvailableSchools.Count;

            var blocks = await _context.Blocos.Where(b => schoolIds.Contains(b.SchoolId)).ToListAsync();
            var blockIds = blocks.Select(b => b.Id).ToList();

            var rooms = await _context.Salas.Where(r => r.BlockId.HasValue && blockIds.Contains(r.BlockId.Value)).ToListAsync();
            var roomIds = rooms.Select(r => r.Id).ToList();
            CountSalas = rooms.Count;

            var equips = await _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => e.RoomId.HasValue && roomIds.Contains(e.RoomId.Value))
                .ToListAsync();
            CountEquipamentos = equips.Count;

            CountTickets = await _context.Tickets.CountAsync(t => t.SchoolId.HasValue && schoolIds.Contains(t.SchoolId.Value));

            var schoolIdsRecursive = AvailableSchools.Select(s => s.Id).ToList();
            var blocksAll = await _context.Blocos.Where(b => schoolIdsRecursive.Contains(b.SchoolId)).ToListAsync();
            var blockIdsAll = blocksAll.Select(b => b.Id).ToList();
            AvailableRooms = await _context.Salas.Where(r => r.BlockId.HasValue && blockIdsAll.Contains(r.BlockId.Value)).ToListAsync();

            // Get all possible pairs for cascading filters before applying any other filters
            var baseQuery = _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => e.RoomId.HasValue && roomIds.Contains(e.RoomId.Value));

            var allEquips = await baseQuery.ToListAsync();
            var equipsQuery = baseQuery.AsQueryable();

            if (FilterSalaId.HasValue) equipsQuery = equipsQuery.Where(e => e.RoomId == FilterSalaId.Value);
            if (!string.IsNullOrEmpty(FilterType)) equipsQuery = equipsQuery.Where(e => e.Type == FilterType);
            
            var equipsTemp = await equipsQuery.ToListAsync();

            if (!string.IsNullOrEmpty(FilterArticle))
            {
                equipsTemp = equipsTemp.Where(e => NormalizeEquipmentName(e.Name) == FilterArticle).ToList();
            }

            UniqueStatuses = new List<string> { "A funcionar", "Armazenado", "Avariado", "Em reparação" };

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                equipsTemp = equipsTemp.Where(e => MapStatus(e) == FilterStatus).ToList();
            }

            // Load items for the table grouped by identical properties
            var groupedItems = equipsTemp.GroupBy(e => new {
                Name = NormalizeEquipmentName(e.Name),
                Category = e.Type ?? "Equipamento",
                Location = $"{e.Room?.Name} ({e.Room?.Block?.School?.Name})",
                RoomId = e.RoomId,
                Status = MapStatus(e)
            });

            Items = groupedItems.Select(g => new DirectorStockItemViewModel {
                Name = g.Key.Name,
                Category = g.Key.Category,
                Location = g.Key.Location,
                RoomId = g.Key.RoomId,
                Quantity = g.Count(),
                Status = g.Key.Status,
                SampleId = g.First().Id.ToString()
            }).OrderBy(i => i.Location).ThenBy(i => i.Name).ToList();

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
                "a funcionar" or "em uso" => "A funcionar",
                "avariado" or "indisponível" or "indisponivel" or "recolhido" => "Avariado",
                "em reparação" or "reparação" or "em manutenção" => "Em reparação",
                _ => "Armazenado"
            };
        }

        public async Task<IActionResult> OnPostRequestLoanAsync(string? itemName, string? itemType, int quantity, string notes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var director = await _context.Diretores
                .Include(d => d.Agrupamento)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (director == null) return Page();

            var ticket = new Ticket
            {
                Description = $"PEDIDO DE EMPRÉSTIMO DE STOCK:\nItem: {itemName}\nTipo: {itemType}\nQuantidade: {quantity}\nAgrupamento: {director.Agrupamento?.Name}\nNotas: {notes}\n\n[DATA:{{\"ItemName\":\"{itemName}\",\"ItemType\":\"{itemType}\",\"Quantity\":{quantity},\"AgrupamentoId\":{director.AgrupamentoId}}}]",
                Status = "Pendente",
                CreatedAt = DateTime.UtcNow,
                Level = "Empréstimo"
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = "Pedido de empréstimo enviado com sucesso!" });
        }

        public async Task<IActionResult> OnPostEditStatusAsync(string name, int? roomId, string currentStatus, string newStatus, int quantity)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var director = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (director == null || director.AgrupamentoId == null) return RedirectToPage();

            int myAgrupamentoId = director.AgrupamentoId.Value;

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School)
                .Where(e => e.Room != null && e.Room.Block.School.AgrupamentoId == myAgrupamentoId);

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

            var director = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (director == null || director.AgrupamentoId == null) return RedirectToPage();

            int myAgrupamentoId = director.AgrupamentoId.Value;

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School)
                .Where(e => e.Room != null && e.Room.Block.School.AgrupamentoId == myAgrupamentoId);

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
            
            return RedirectToPage(new { success = "Equipamentos eliminados com sucesso!" });
        }
        public async Task<IActionResult> OnPostFulfillRequestAsync(int requestId, string[] selectedIds)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var pedido = await _context.PedidosStock.FindAsync(requestId);
            if (pedido == null || pedido.Status != "Pendente_Diretor") return RedirectToPage();

            var director = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (director?.AgrupamentoId != pedido.AgrupamentoId) return RedirectToPage();

            int fulfilledCount = 0;

            foreach (var prefixedId in selectedIds)
            {
                if (prefixedId.StartsWith("se_"))
                {
                    int id = int.Parse(prefixedId.Substring(3));
                    var item = await _context.StockEmpresa.FindAsync(id);
                    if (item != null)
                    {
                        item.SchoolId = pedido.SchoolId;
                        item.Status = "Emprestado";
                        item.IsAvailable = false;
                        fulfilledCount++;
                    }
                }
                else if (prefixedId.StartsWith("eq_"))
                {
                    int id = int.Parse(prefixedId.Substring(3));
                    var item = await _context.Equipamentos
                        .Include(e => e.Room)
                        .FirstOrDefaultAsync(e => e.Id == id);

                    if (item != null)
                    {
                        // Transfer equipment to the school
                        // We set RoomId to null to indicate it is now with the school but not in a specific room yet,
                        // or we could assign it to a default "Entry" room if we had one.
                        // For now, let's keep it in "Armazenado" state but linked to the school via RoomId=null + school identity.
                        // Wait, Equipamento doesn't have a direct SchoolId. It's inferred via Room -> Block -> School.
                        // So we MUST assign it to a room in the target school.
                        
                        var targetSchoolRoom = await _context.Salas
                            .Include(r => r.Block)
                            .FirstOrDefaultAsync(r => r.Block != null && r.Block.SchoolId == pedido.SchoolId);

                        if (targetSchoolRoom != null)
                        {
                            item.RoomId = targetSchoolRoom.Id;
                            // Add status change
                            var statusChange = new StatusEquipamento
                            {
                                EquipamentoId = item.Id,
                                Estado = "Em Uso (Emprestado)"
                            };
                            _context.StatusEquipamentos.Add(statusChange);
                            fulfilledCount++;
                        }
                    }
                }
            }

            pedido.Status = "Atendido";
            pedido.UpdatedAt = DateTime.UtcNow;
            pedido.DirectorNotes = $"Atendido pelo Diretor em {DateTime.Now:dd/MM/yyyy}. Total de {fulfilledCount} itens enviados.";

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Emprestado com sucesso!" });
        }

        public async Task<IActionResult> OnPostForwardToAdminAsync(int requestId, string? directorNotes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var pedido = await _context.PedidosStock.FindAsync(requestId);
            if (pedido == null || pedido.Status != "Pendente_Diretor") return RedirectToPage();

            pedido.Status = "Pendente_Admin";
            pedido.DirectorNotes = directorNotes ?? "Sem stock disponível no agrupamento.";
            pedido.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = $"Pedido #{requestId} reencaminhado ao Administrador." });
        }
    }
    public class DirectorLendingItemViewModel
    {
        public string? IdWithPrefix { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Source { get; set; }
        public string? CurrentLocation { get; set; }
    }

    public class DirectorStockItemViewModel
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int? RoomId { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
        public string? SampleId { get; set; }
    }
}
