using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Professors
{
    public class ProfessorStocksModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProfessorStocksModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? FilterSalaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterArticle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public int CountSalas { get; set; }
        public int CountEquipamentos { get; set; }
        public int CountTickets { get; set; }
        public int CountHealthy { get; set; }
        
        public string? SuccessMessage { get; set; }

        public List<Sala> AvailableRooms { get; set; } = new();
        public List<string> UniqueStatuses { get; set; } = new();

        public List<ProfessorStockItemViewModel> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var prof = await _context.Professores.FirstOrDefaultAsync(p => p.UserId == userId);
            if (prof == null) return Page();

            if (Request.Query.ContainsKey("success"))
            {
                SuccessMessage = Request.Query["success"];
            }

            var salas = await _context.Salas
                .Include(s => s.Block)
                    .ThenInclude(b => b.School)
                .Where(s => s.ResponsibleProfessorId == userId)
                .ToListAsync();

            AvailableRooms = salas;
            var salaIds = salas.Select(s => s.Id).ToList();

            CountSalas = salas.Count;

            var equips = await _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
                .Where(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value))
                .ToListAsync();

            CountEquipamentos = equips.Count;
            CountHealthy = equips.Count(e => MapStatus(e) == "A funcionar");

            CountTickets = await _context.Tickets
                .Include(t => t.Equipamento)
                .CountAsync(t => t.Equipamento != null && t.Equipamento.RoomId.HasValue && salaIds.Contains(t.Equipamento.RoomId.Value));

            var baseQuery = _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value));

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

            Items = groupedItems.Select(g => new ProfessorStockItemViewModel {
                Name = g.Key.Name,
                Category = g.Key.Category,
                Location = g.Key.Location,
                RoomId = g.Key.RoomId,
                Quantity = g.Count(),
                Status = g.Key.Status
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
            var estado = e.StatusEquipamentos?.OrderByDescending(s => s.Id).FirstOrDefault()?.Estado ?? e.Status ?? "";

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

        public async Task<IActionResult> OnPostRequestLoanAsync(string? itemName, string? itemType, int quantity, string notes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var prof = await _context.Professores
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (prof == null) return Page();

            // Get standard school from professor's primary block (for routing the request)
            var room = await _context.Salas.Include(r => r.Block).ThenInclude(b => b.School).FirstOrDefaultAsync(r => r.ResponsibleProfessorId == userId);
            
            var ticket = new Ticket
            {
                Description = $"PEDIDO DE EMPRÉSTIMO DE STOCK:\nItem: {itemName}\nTipo: {itemType}\nQuantidade: {quantity}\nSala/Escola: {room?.Name} ({room?.Block?.School?.Name})\nNotas: {notes}\n\n[DATA:{{\"ItemName\":\"{itemName}\",\"ItemType\":\"{itemType}\",\"Quantity\":{quantity},\"SchoolId\":{(room?.Block?.SchoolId.ToString() ?? "null")},\"AgrupamentoId\":{(room?.Block?.School?.AgrupamentoId.ToString() ?? "null")}}}]",
                Status = "Pedido",
                CreatedAt = DateTime.UtcNow,
                Level = "Empréstimo",
                SchoolId = room?.Block?.SchoolId,
                RequestedByUserId = userId
            };

            var pedido = new PedidoStock
            {
                RequestedByUserId = userId,
                ItemName = itemName ?? "Desconhecido",
                ItemType = itemType ?? "Outro",
                Quantity = quantity,
                Notes = notes,
                Status = "Pendente_Diretor",
                CreatedAt = DateTime.UtcNow,
                SchoolId = room?.Block?.SchoolId,
                AgrupamentoId = room?.Block?.School?.AgrupamentoId
            };

            _context.Tickets.Add(ticket);
            _context.PedidosStock.Add(pedido);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = "Pedido de empréstimo enviado com sucesso à Coordenação!" });
        }

        public async Task<IActionResult> OnPostEditStatusAsync(string name, int? roomId, string currentStatus, string newStatus, int quantity)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var profRooms = await _context.Salas.Where(s => s.ResponsibleProfessorId == userId).Select(s => s.Id).ToListAsync();

            var items = await _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
                .Where(e => e.RoomId.HasValue && profRooms.Contains(e.RoomId.Value))
                .ToListAsync();

            if (roomId.HasValue && profRooms.Contains(roomId.Value))
            {
                items = items.Where(e => e.RoomId == roomId.Value).ToList();
            }

            var itemsToUpdate = items
                .Where(e => NormalizeEquipmentName(e.Name).Equals(name, StringComparison.OrdinalIgnoreCase) && 
                            MapStatus(e).Equals(currentStatus ?? "", StringComparison.OrdinalIgnoreCase))
                .Take(quantity)
                .ToList();

            foreach(var item in itemsToUpdate)
            {
                if (newStatus == "Armazenado" || newStatus == "Disponível") item.Status = "Disponível";
                else item.Status = newStatus;
                
                var statusChange = new StatusEquipamento
                {
                    EquipamentoId = item.Id,
                    Estado = item.Status
                };
                _context.StatusEquipamentos.Add(statusChange);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Estado atualizado com sucesso!" });
        }

        public async Task<IActionResult> OnPostDeleteStockAsync(string name, int? roomId, string currentStatus, int quantity)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var profRooms = await _context.Salas.Where(s => s.ResponsibleProfessorId == userId).Select(s => s.Id).ToListAsync();

            var items = await _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
                .Where(e => e.RoomId.HasValue && profRooms.Contains(e.RoomId.Value))
                .ToListAsync();

            if (roomId.HasValue && profRooms.Contains(roomId.Value))
            {
                items = items.Where(e => e.RoomId == roomId.Value).ToList();
            }

            var itemsToDelete = items
                .Where(e => NormalizeEquipmentName(e.Name).Equals(name, StringComparison.OrdinalIgnoreCase) && 
                            MapStatus(e).Equals(currentStatus ?? "", StringComparison.OrdinalIgnoreCase))
                .Take(quantity)
                .ToList();

            _context.Equipamentos.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();
            
            return RedirectToPage(new { success = "Equipamentos excluídos com sucesso!" });
        }
    }

    public class ProfessorStockItemViewModel
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int? RoomId { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
    }
}
