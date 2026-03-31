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

        public List<StockItemViewModel> Items { get; set; } = new();

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

            var rooms = await _context.Salas.Where(r => blockIds.Contains(r.BlockId)).ToListAsync();
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
            AvailableRooms = await _context.Salas.Where(r => blockIdsAll.Contains(r.BlockId)).ToListAsync();

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

            UniqueStatuses = new List<string> { "A funcionar", "Avariado", "Em reparo" };

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                equipsTemp = equipsTemp.Where(e => MapStatus(e) == FilterStatus).ToList();
            }

            // Load items for the table grouped by identical properties
            var groupedItems = equipsTemp.GroupBy(e => new {
                Name = NormalizeEquipmentName(e.Name),
                Category = e.Type ?? "Equipamento",
                Location = $"{e.Room?.Name} ({e.Room?.Block?.School?.Name})",
                Status = MapStatus(e)
            });

            Items = groupedItems.Select(g => new StockItemViewModel {
                Name = g.Key.Name,
                Category = g.Key.Category,
                Location = g.Key.Location,
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
                "a funcionar" or "disponível" or "disponivel" or "funcionando" or "em uso" => "A funcionar",
                "avariado" or "indisponível" or "indisponivel" or "recolhido" => "Avariado",
                "em reparo" or "reparo" or "em manutenção" => "Em reparo",
                _ => "A funcionar"
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
                Description = $"PEDIDO DE EMPRÉSTIMO DE STOCK:\nItem: {itemName}\nTipo: {itemType}\nQuantidade: {quantity}\nAgrupamento: {director.Agrupamento?.Name}\nNotas: {notes}",
                Status = "Pedido",
                CreatedAt = DateTime.UtcNow,
                Level = "Empréstimo"
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = "Pedido de empréstimo enviado com sucesso!" });
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
