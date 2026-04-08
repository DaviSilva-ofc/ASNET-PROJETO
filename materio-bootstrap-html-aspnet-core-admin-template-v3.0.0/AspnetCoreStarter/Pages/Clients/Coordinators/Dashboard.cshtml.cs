using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Coordinators
{
    public class CoordinatorDashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly Services.IStockService _stockService;

        public CoordinatorDashboardModel(AppDbContext context, Services.IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public string? SchoolName { get; set; }
        public int TotalSalas { get; set; }
        public int TotalEquipamentos { get; set; }
        public int DamagedEquipmentCount { get; set; }
        public int PendingTicketsCount { get; set; }
        public int TotalProfessores { get; set; }
        public int TotalUnreadMessages { get; set; }
        public int LowStockAlertsCount { get; set; }
        public List<User>? RecentMessageSenders { get; set; }
        
        // Chart data
        public List<int>? LineChartMonthlyData { get; set; }
        public List<string>? LineChartLabels { get; set; }
        
        // Infrastructure context
        public School? MySchool { get; set; }
        public Agrupamento? MyAgrupamento { get; set; }
        public List<Bloco>? Blocos { get; set; }
        public List<Sala>? Salas { get; set; }
        public List<LowStockItemViewModel> StockAlerts { get; set; } = new();
        public List<PedidoStock> MyRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity!.IsAuthenticated) return RedirectToPage("/Auth/Login");
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");
            if (User.FindFirst(ClaimTypes.Role)?.Value != "Coordenador") return RedirectToPage("/Index");

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                    .ThenInclude(s => s.Agrupamento)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (coord?.SchoolId == null) return Page();

            int schoolId = coord.SchoolId.Value;
            MySchool = coord.School;
            MyAgrupamento = coord.School?.Agrupamento;
            SchoolName = coord.School?.Name ?? "Escola";

            // Infrastructure tree for this school
            Blocos = await _context.Blocos.Where(b => b.SchoolId == schoolId).ToListAsync();
            var blocoIds = Blocos.Select(b => b.Id).ToList();

            Salas = await _context.Salas.Where(s => blocoIds.Contains(s.BlockId)).ToListAsync();
            var salaIds = Salas.Select(s => s.Id).ToList();

            TotalSalas = Salas.Count;
            TotalEquipamentos = await _context.Equipamentos.CountAsync(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value));
            DamagedEquipmentCount = await _context.Equipamentos.CountAsync(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value) && e.Status == "Avariado");
            TotalProfessores = await _context.Professores.CountAsync(p => blocoIds.Contains(p.BlocoId ?? 0));
            
            // Tickets (only for this school)
            PendingTicketsCount = await _context.Tickets.CountAsync(t => t.SchoolId == schoolId && (t.Status == "Pendente" || t.TechnicianId == null));

            // Messages
            var unread = await _context.Mensagens
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            TotalUnreadMessages = unread.Count;
            RecentMessageSenders = unread.Select(m => m.Sender).GroupBy(u => u.Id).Select(g => g.First()).Take(5).ToList()!;

            // 5. Low Stock Alerts for this school
            var allAvailableStock = await _context.StockEmpresa
                .Where(s => (s.IsAvailable || s.Status == "Disponível") && s.SchoolId == schoolId)
                .ToListAsync();

            var groupedStock = allAvailableStock
                .GroupBy(s => new { Name = s.EquipmentName ?? "Sem Nome", Type = s.Type })
                .Select(g => new LowStockItemViewModel
                {
                    Name = g.Key.Name,
                    AvailableCount = g.Count(),
                    Type = g.Key.Type
                })
                .ToList();

            StockAlerts = groupedStock
                .Where(x => _stockService.IsLowStock(x.Name, x.Type, x.AvailableCount))
                .ToList();

            LowStockAlertsCount = StockAlerts.Count;

            // Monthly Chart Data (Bars)
            LineChartLabels = new List<string>();
            LineChartMonthlyData = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var d = DateTime.Now.AddMonths(-i);
                LineChartLabels.Add(d.ToString("MMM"));
                LineChartMonthlyData.Add(await _context.Tickets.CountAsync(t => t.SchoolId == schoolId && t.CreatedAt.Month == d.Month && t.CreatedAt.Year == d.Year));
            }

            // Load Stock Requests
            MyRequests = await _context.PedidosStock
                .Where(p => p.SchoolId == schoolId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRequestStockAsync(string itemName, string? itemType, int quantity, string? notes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (coord == null || coord.SchoolId == null) return RedirectToPage();

            var pedido = new PedidoStock
            {
                ItemName = itemName,
                ItemType = itemType,
                Quantity = quantity,
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

            TempData["SuccessMessage"] = "Pedido de stock enviado com sucesso ao Diretor.";
            return RedirectToPage();
        }

        public class LowStockItemViewModel
        {
            public string Name { get; set; } = string.Empty;
            public int AvailableCount { get; set; }
            public string? Type { get; set; }
        }
    }
}
