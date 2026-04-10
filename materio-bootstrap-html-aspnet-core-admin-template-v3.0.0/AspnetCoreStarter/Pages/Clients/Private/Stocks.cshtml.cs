using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Private
{
    public class PrivateStocksModel : PageModel
    {
        private readonly AppDbContext _context;

        public PrivateStocksModel(AppDbContext context)
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
        
        public string? SuccessMessage { get; set; }

        public List<Sala> AvailableRooms { get; set; } = new();
        public List<string> UniqueStatuses { get; set; } = new();

        public List<PrivateStockItemViewModel> Items { get; set; } = new();
        public List<PrivateStockItemViewModel> BorrowedItems { get; set; } = new();
        public List<PedidoStock> MyRequests { get; set; } = new();
        
        public Empresa Empresa { get; set; }

        [BindProperty]
        public string NewRequestEquipmentName { get; set; }
        
        [BindProperty]
        public string NewRequestItemType { get; set; }
        
        [BindProperty]
        public int NewRequestQuantity { get; set; }
        
        [BindProperty]
        public string NewRequestReason { get; set; }


        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var user = await _context.Users.Include(u => u.Empresa).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.EmpresaId == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            if (Request.Query.ContainsKey("success"))
            {
                SuccessMessage = Request.Query["success"];
            }

            int empresaId = user.EmpresaId.Value;
            Empresa = user.Empresa;

            // Load rooms associated with this company
            var equipsForRooms = await _context.Equipamentos
                .Include(e => e.Room)
                .Where(e => e.EmpresaId == empresaId && e.Room != null)
                .ToListAsync();
            
            var roomIds = equipsForRooms.Select(e => e.RoomId).Distinct().Where(r => r.HasValue).Select(r => r.Value).ToList();
            AvailableRooms = await _context.Salas.Where(r => roomIds.Contains(r.Id)).ToListAsync();
            CountSalas = AvailableRooms.Count;

            // Load all equipments and central stock for this company
            var allEquipsQuery = _context.Equipamentos
                .Include(e => e.Room)
                .Where(e => e.EmpresaId == empresaId)
                .AsQueryable();

            var allStockEmpresaQuery = _context.StockEmpresa
                .Where(s => s.EmpresaId == empresaId)
                .AsQueryable();

            if (FilterSalaId.HasValue) 
            {
                allEquipsQuery = allEquipsQuery.Where(e => e.RoomId == FilterSalaId.Value);
                // Central stock doesn't have a room, but we might want to hide it if a specific room is selected
            }
            if (!string.IsNullOrEmpty(FilterType)) 
            {
                allEquipsQuery = allEquipsQuery.Where(e => e.Type == FilterType);
                allStockEmpresaQuery = allStockEmpresaQuery.Where(s => s.Type == FilterType);
            }

            var equipsTemp = await allEquipsQuery.ToListAsync();
            var stockTemp = FilterSalaId.HasValue ? new List<StockEmpresa>() : await allStockEmpresaQuery.ToListAsync();

            if (!string.IsNullOrEmpty(FilterArticle))
            {
                equipsTemp = equipsTemp.Where(e => NormalizeEquipmentName(e.Name) == FilterArticle).ToList();
                stockTemp = stockTemp.Where(s => NormalizeEquipmentName(s.EquipmentName) == FilterArticle).ToList();
            }

            UniqueStatuses = new List<string> { "A funcionar", "Armazenado", "Avariado", "Em reparo" };

            var equipsMappedList = equipsTemp.Select(e => new PrivateStockItemViewModel
            {
                Prefix = "eq",
                Id = e.Id,
                Name = NormalizeEquipmentName(e.Name),
                Category = e.Type ?? "Equipamento",
                Location = e.Room?.Name ?? "Agrupamento/Edifício Central",
                RoomId = e.RoomId,
                Status = MapStatus(e)
            });

            var stockMappedList = stockTemp.Select(s => new PrivateStockItemViewModel
            {
                Prefix = "se",
                Id = s.Id,
                Name = NormalizeEquipmentName(s.EquipmentName),
                Category = s.Type ?? "Equipamento",
                Location = "Stock de Armazém",
                RoomId = null,
                Status = s.Status == "Disponível" || s.IsAvailable ? "Armazenado" : s.Status
            });

            var fullList = equipsMappedList.Concat(stockMappedList).ToList();

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                fullList = fullList.Where(i => i.Status == FilterStatus).ToList();
            }

            // Group identically named and located items
            var groupedItems = fullList.GroupBy(e => new {
                Name = e.Name,
                Category = e.Category,
                Location = e.Location,
                RoomId = e.RoomId,
                Status = e.Status
            });

            Items = groupedItems.Select(g => new PrivateStockItemViewModel {
                Id = g.First().Id,
                Name = g.Key.Name,
                Category = g.Key.Category,
                Location = g.Key.Location,
                RoomId = g.Key.RoomId,
                Quantity = g.Count(),
                Status = g.Key.Status
            }).OrderBy(i => i.Location).ThenBy(i => i.Name).ToList();

            // Stats
            var allEquipsAndStockForCount = (await _context.Equipamentos.Where(e => e.EmpresaId == empresaId).CountAsync()) + 
                                            (await _context.StockEmpresa.Where(s => s.EmpresaId == empresaId).CountAsync());
            CountEquipamentos = allEquipsAndStockForCount;

            CountTickets = await _context.Tickets.Include(t => t.Equipamento).CountAsync(t => t.Equipamento != null && t.Equipamento.EmpresaId == empresaId);

            BorrowedItems = Items.Where(i => i.Status == "Emprestado").ToList();

            MyRequests = await _context.PedidosStock
                .Where(p => p.RequestedByUserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRequestStockAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(int.Parse(userIdStr!));
            if (user == null || user.EmpresaId == null) return RedirectToPage("/Auth/Login");

            var pedido = new PedidoStock
            {
                ItemType = NewRequestItemType ?? "Aquisição",
                ItemName = NewRequestEquipmentName,
                Quantity = NewRequestQuantity,
                Notes = NewRequestReason,
                Status = "Pendente_Admin", // Goes directly to admin
                RequestedByUserId = user.Id,
                CreatedAt = System.DateTime.UtcNow,
                UpdatedAt = System.DateTime.UtcNow
            };

            _context.PedidosStock.Add(pedido);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = "O seu pedido de stock foi submetido e será revisto pela administração." });
        }

        public async Task<IActionResult> OnPostReturnLoanAsync(int sampleId, int quantity)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(int.Parse(userIdStr!));
            if (user == null || user.EmpresaId == null) return RedirectToPage("/Auth/Login");

            if (quantity <= 0) return RedirectToPage();

            var sample = await _context.StockEmpresa.FindAsync(sampleId);
            if (sample == null || sample.EmpresaId != user.EmpresaId)
            {
                // Can't return if it doesn't belong to their company or doesn't exist
                return RedirectToPage();
            }

            var lentItems = await _context.StockEmpresa
                .Where(s => s.EquipmentName == sample.EquipmentName && s.Type == sample.Type && s.EmpresaId == user.EmpresaId && s.Status == "Emprestado")
                .Take(quantity)
                .ToListAsync();

            if (lentItems.Count == 0) return RedirectToPage();

            foreach (var item in lentItems)
            {
                item.Status = "Disponível";
                item.IsAvailable = true;
                item.EmpresaId = null; // Remove the binding to Private Company, returns it to Central Stock
                item.SchoolId = null;
                item.AgrupamentoId = null;
            }

            // Let's check if there is an associated PedidoStock we can mark as returned or let the audit trail stay.
            // Improve matching because PedidoStock.ItemName may refer to a generic category instead of the explicit EquipmentName
            var relatedPedido = await _context.PedidosStock
                .Where(p => p.RequestedByUserId == user.Id && p.Status == "Atendido" && 
                            (p.ItemName == sample.EquipmentName || p.ItemType == sample.Type))
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();

            if (relatedPedido != null)
            {
                // We're returning it, just close the loop on the Admin screen
                relatedPedido.Status = "Devolvido";
                relatedPedido.UpdatedAt = System.DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = $"Devolveu {lentItems.Count} unidade(s) de {sample.EquipmentName} à Administração ASNET." });
        }

        private string NormalizeEquipmentName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Desconhecido";
            string normalized = name.Trim();
            var pluralMaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"monitores", "monitor"},
                {"computadores", "computador"},
                {"portáteis", "portátil"},
                {"teclados", "teclado"},
                {"ratos", "rato"},
                {"projetores", "projetor"},
                {"cabos", "cabo"}
            };
            foreach(var kv in pluralMaps)
            {
                if(normalized.Equals(kv.Key, System.StringComparison.OrdinalIgnoreCase)) return char.ToUpper(kv.Value[0]) + kv.Value.Substring(1).ToLower();
            }
            return normalized;
        }

        private string MapStatus(Equipamento eq)
        {
            if (eq.Status == "Avariado") return "Avariado";
            if (eq.Status == "Reparação" || eq.Status == "Em reparo") return "Em reparo";
            if (eq.Status == "A funcionar") return "A funcionar";
            if (eq.Status == "Armazenado" || eq.Status == "Disponível") return "Armazenado";
            return "A funcionar"; 
        }
    }

    public class PrivateStockItemViewModel
    {
        public string? Prefix { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int? RoomId { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
