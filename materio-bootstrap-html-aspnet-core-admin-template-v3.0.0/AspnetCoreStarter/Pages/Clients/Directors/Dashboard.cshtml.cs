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

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorDashboardModel : PageModel
    {
        // Technician Dashboard Workflow Refinement - Force rebuild
        private readonly AppDbContext _context;
        private readonly IStockService _stockService;

        public DirectorDashboardModel(AppDbContext context, IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public bool HasPreventiveMaintenanceToday { get; set; }
        public List<Ticket> TodaysPreventiveMaintenances { get; set; } = new();

        public List<LowStockItemViewModel> StockAlerts { get; set; } = new();
        public int TotalProfessores { get; set; }
        public int PendingTicketsCount { get; set; }
        public int LowStockAlertsCount { get; set; }
        public int TotalEscolas { get; set; }
        public int TotalSalas { get; set; }
        public int TotalContratos { get; set; }
        public int DamagedEquipmentCount { get; set; }
        public int TotalUnreadMessages { get; set; }
        public List<AspnetCoreStarter.Models.User>? RecentMessageSenders { get; set; }
        

        public List<int>? LineChartMonthlyData { get; set; }
        public List<string>? LineChartLabels { get; set; }

        public string? ClientLocationsJson { get; set; }

        // Infrastructure tree data (filtered)
        public Agrupamento? Agrupamento { get; set; }
        public List<AspnetCoreStarter.Models.School>? Schools { get; set; }
        public List<Bloco>? Blocos { get; set; }
        public List<Sala>? Salas { get; set; }
        public List<string> EquipmentTypes { get; set; } = new();
        public List<PedidoStock> SchoolRequests { get; set; } = new();
        public List<StockEmpresa> AvailableStock { get; set; } = new();
        public List<Ticket> RecentTickets { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || userRole != "Diretor")
                return RedirectToPage("/Index");

            int userId = int.Parse(userIdStr);

            // Temporary fix for missing tables and columns
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS contratos (
                        id_contrato INT AUTO_INCREMENT PRIMARY KEY,
                        periodo VARCHAR(50),
                        tipo_contrato VARCHAR(50),
                        status_contrato VARCHAR(50),
                        descricao TEXT,
                        id_agrupamento INT,
                        id_admin INT,
                        FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento),
                        FOREIGN KEY (id_admin) REFERENCES utilizadores(id_utilizador)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE utilizadores ADD COLUMN password_hash VARCHAR(255) NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE salas ADD COLUMN id_professor_responsavel INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN id_equipamento INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN status VARCHAR(50) DEFAULT 'Pedido';"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE equipamentos ADD COLUMN status VARCHAR(50) DEFAULT 'A funcionar';"); } catch { }

            // Fetch the Director's Agrupamento
            var diretor = await _context.Diretores
                .Include(d => d.Agrupamento)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (diretor == null || diretor.AgrupamentoId == null)
            {
                // If not found or No Agrupamento assigned, they see nothing or redirect
                return Page();
            }

            int agrupamentoId = diretor.AgrupamentoId.Value;
            Agrupamento = diretor.Agrupamento;

            // Infrastructure tree data (filtered by Agrupamento)
            Schools = await _context.Schools
                .Where(s => s.AgrupamentoId == agrupamentoId)
                .ToListAsync();

            var schoolIds = Schools.Select(s => s.Id).ToList();

            Blocos = await _context.Blocos
                .Where(b => schoolIds.Contains(b.SchoolId))
                .ToListAsync();

            var blocoIds = Blocos.Select(b => b.Id).ToList();

            Salas = await _context.Salas
                .Where(s => s.BlockId.HasValue && blocoIds.Contains(s.BlockId.Value))
                .ToListAsync();

            var salaIds = Salas.Select(s => s.Id).ToList();

            // Totals
            TotalEscolas = Schools.Count;
            TotalSalas = Salas.Count;
            TotalContratos = await _context.Contratos.CountAsync(c => c.AgrupamentoId == agrupamentoId);
            
            DamagedEquipmentCount = await _context.Equipamentos
                .CountAsync(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value) && e.Status == "Avariado");

            // Total Professores belonging to blocks in this Agrupamento
            TotalProfessores = await _context.Professores
                .CountAsync(p => blocoIds.Contains(p.BlocoId ?? 0));
            
            // Tickets pendentes (Filtered by schools in this Agrupamento)
            PendingTicketsCount = await _context.Tickets
                .Where(t => (t.Status == "Pendente" || t.TechnicianId == null) && t.SchoolId != null && schoolIds.Contains(t.SchoolId.Value))
                .CountAsync();

            // 5. Low Stock Alerts Logic
            var allAvailableStock = await _context.StockEmpresa
                .Where(s => (s.IsAvailable || s.Status == "Disponível") && (s.AgrupamentoId == agrupamentoId || (s.SchoolId != null && schoolIds.Contains(s.SchoolId.Value))))
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

            // Chat Notifications (Unread messages for the current director)
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
            // We'll show the last 6 months
            LineChartLabels = new List<string>();
            LineChartMonthlyData = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var label = date.ToString("MMM");
                LineChartLabels.Add(label);

                var count = await _context.Tickets
                    .Where(t => t.SchoolId != null && schoolIds.Contains(t.SchoolId.Value) && t.CreatedAt.Month == date.Month && t.CreatedAt.Year == date.Year)
                    .CountAsync();
                LineChartMonthlyData.Add(count);
            }

            // Client Locations for Map (Only for this Agrupamento)
            var locations = Schools
                .Where(s => !string.IsNullOrEmpty(s.Address))
                .Select(s => new { name = s.Name, address = s.Address })
                .ToList();
            ClientLocationsJson = System.Text.Json.JsonSerializer.Serialize(locations);

            // Load school requests (pending director or escalated to admin)
            SchoolRequests = await _context.PedidosStock
                .Include(p => p.School)
                .Include(p => p.RequestedBy)
                .Where(p => p.AgrupamentoId == agrupamentoId && (p.Status == "Pendente_Diretor" || p.Status == "Pendente_Admin"))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Load available unassigned stock for this agrupamento
            AvailableStock = await _context.StockEmpresa
                .Where(s => s.AgrupamentoId == agrupamentoId && s.SchoolId == null && (s.IsAvailable || s.Status == "Armazenado" || s.Status == "Disponível"))
                .ToListAsync();

            // Recent Maintenance Tickets for this grouping (excluding technician stock loans)
            RecentTickets = await _context.Tickets
                .Include(t => t.Technician)
                .Include(t => t.Equipamento)
                    .ThenInclude(e => e.Room)
                .Where(t => t.SchoolId != null && schoolIds.Contains(t.SchoolId.Value) && t.Level != "Empréstimo")
                .OrderByDescending(t => t.CreatedAt)
                .Take(3)
                .ToListAsync();

            // Check for today's preventive maintenance
            var today = DateTime.Today;
            TodaysPreventiveMaintenances = await _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Technician)
                .Where(t => t.Type == "Manutenção Preventiva" 
                         && t.ScheduledDate.HasValue 
                         && t.ScheduledDate.Value.Date == today
                         && t.SchoolId != null && schoolIds.Contains(t.SchoolId.Value))
                .ToListAsync();

            HasPreventiveMaintenanceToday = TodaysPreventiveMaintenances.Any();

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
                    item.SchoolId = request.SchoolId;
                    item.Status = "Emprestado";
                    item.IsAvailable = false;
                }
            }

            request.Status = "Atendido";
            request.DirectorNotes = directorNotes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pedido atendido e stock atribuído à escola.";
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
            public string Name { get; set; }
            public int AvailableCount { get; set; }
            public string? Type { get; set; }
        }
    }
}
