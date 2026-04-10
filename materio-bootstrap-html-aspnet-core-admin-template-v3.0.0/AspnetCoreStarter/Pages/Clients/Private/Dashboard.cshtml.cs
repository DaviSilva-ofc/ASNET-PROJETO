using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using AspnetCoreStarter.Services;
using System;

namespace AspnetCoreStarter.Pages.Clients.Private
{
    public class PrivateDashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IStockService _stockService;

        public PrivateDashboardModel(AppDbContext context, IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public List<LowStockItemViewModel> StockAlerts { get; set; } = new();
        public int TotalUsers { get; set; }
        public int PendingTicketsCount { get; set; }
        public int LowStockAlertsCount { get; set; }
        public int TotalContratos { get; set; }
        public int DamagedEquipmentCount { get; set; }
        public int TotalUnreadMessages { get; set; }
        public List<AspnetCoreStarter.Models.User>? RecentMessageSenders { get; set; }

        public List<int>? LineChartMonthlyData { get; set; }
        public List<string>? LineChartLabels { get; set; }

        public string? ClientLocationsJson { get; set; }

        // Infrastructure data
        public Empresa? Empresa { get; set; }
        public List<string> EquipmentTypes { get; set; } = new();
        public List<Contrato> RecentContracts { get; set; } = new();
        public List<StockEmpresa> AvailableStock { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || userRole != "ClientePrivado")
                return RedirectToPage("/Index");

            int userId = int.Parse(userIdStr);

            // Fetch the User and their Empresa
            var user = await _context.Users
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.EmpresaId == null)
            {
                return Page();
            }

            int empresaId = user.EmpresaId.Value;
            Empresa = user.Empresa;

            // Totals
            TotalContratos = await _context.Contratos.CountAsync(c => c.EmpresaId == empresaId);
            TotalUsers = await _context.Users.CountAsync(u => u.EmpresaId == empresaId);
            
            DamagedEquipmentCount = await _context.Equipamentos
                .CountAsync(e => e.EmpresaId == empresaId && e.Status == "Avariado");

            // Tickets pendentes (Filtered by equipment belonging to this company)
            PendingTicketsCount = await _context.Tickets
                .Include(t => t.Equipamento)
                .Where(t => (t.Status == "Pendente" || t.TechnicianId == null) && t.Equipamento != null && t.Equipamento.EmpresaId == empresaId)
                .CountAsync();

            // 5. Low Stock Alerts Logic
            var allAvailableStock = await _context.StockEmpresa
                .Where(s => (s.IsAvailable || s.Status == "Disponível") && s.EmpresaId == empresaId)
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

            // Chat Notifications (Unread messages for the current user)
            var unreadMessages = await _context.Mensagens
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            TotalUnreadMessages = unreadMessages.Count;
            RecentMessageSenders = unreadMessages
                .Select(m => m.Sender)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .Take(5)
                .ToList();

            // Monthly Ticket Comparison Chart Data
            LineChartLabels = new List<string>();
            LineChartMonthlyData = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var label = date.ToString("MMM");
                LineChartLabels.Add(label);

                var count = await _context.Tickets
                    .Include(t => t.Equipamento)
                    .Where(t => t.Equipamento != null && t.Equipamento.EmpresaId == empresaId && t.CreatedAt.Month == date.Month && t.CreatedAt.Year == date.Year)
                    .CountAsync();
                LineChartMonthlyData.Add(count);
            }

            // Company Location for Map
            var locations = new List<object>();
            if (Empresa != null && !string.IsNullOrEmpty(Empresa.Location))
            {
                locations.Add(new { name = Empresa.Name, address = Empresa.Location });
            }
            ClientLocationsJson = System.Text.Json.JsonSerializer.Serialize(locations);

            // Load active contracts for this company to display in the dashboard
            RecentContracts = await _context.Contratos
                .Where(c => c.EmpresaId == empresaId)
                .OrderByDescending(c => c.ExpiryDate)
                .Take(5)
                .ToListAsync();

            // Load available unassigned stock for this company
            AvailableStock = await _context.StockEmpresa
                .Where(s => s.EmpresaId == empresaId && (s.IsAvailable || s.Status == "Armazenado" || s.Status == "Disponível"))
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostFulfillRequestAsync(int requestId, int[] selectedStockIds, string? directorNotes)
        {
            var request = await _context.PedidosStock.FindAsync(requestId);
            if (request == null) return RedirectToPage();

            if (selectedStockIds != null && selectedStockIds.Length > 0)
            {
                var items = await _context.StockEmpresa
                    .Where(s => selectedStockIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var item in items)
                {
                    item.Status = "Emprestado";
                    item.IsAvailable = false;
                }
            }

            request.Status = "Atendido";
            request.DirectorNotes = directorNotes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pedido atendido e stock atribuído.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEscalateRequestAsync(int requestId, string? directorNotes)
        {
            var request = await _context.PedidosStock.FindAsync(requestId);
            if (request == null) return RedirectToPage();

            request.Status = "Pendente_Admin";
            request.DirectorNotes = directorNotes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pedido escalado para o Administrador.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectRequestAsync(int requestId, string? directorNotes)
        {
            var request = await _context.PedidosStock.FindAsync(requestId);
            if (request == null) return RedirectToPage();

            request.Status = "Recusado";
            request.DirectorNotes = directorNotes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pedido recusado.";
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
