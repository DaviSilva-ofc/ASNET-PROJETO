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

        public List<CoordinatorStockItemViewModel> Items { get; set; } = new();

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

            Items = groupedItems.Select(g => new CoordinatorStockItemViewModel {
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
            var estado = e.StatusEquipamentos?.OrderByDescending(s => s.Id).FirstOrDefault()?.Estado ?? "";

            return estado.ToLower() switch
            {
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

            var coord = await _context.Coordenadores
                .Include(d => d.School)
                    .ThenInclude(s => s.Agrupamento)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (coord == null) return Page();

            var ticket = new Ticket
            {
                Description = $"PEDIDO DE EMPRÉSTIMO DE STOCK:\nItem: {itemName}\nTipo: {itemType}\nQuantidade: {quantity}\nAgrupamento: {coord.School?.Agrupamento?.Name}\nEscola Solicitante: {coord.School?.Name}\nNotas: {notes}\n\n[DATA:{{\"ItemName\":\"{itemName}\",\"ItemType\":\"{itemType}\",\"Quantity\":{quantity},\"AgrupamentoId\":{coord.School?.AgrupamentoId}}}]",
                Status = "Pedido",
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
    }

    public class CoordinatorStockItemViewModel
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int? RoomId { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
    }
}
