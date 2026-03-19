using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorDashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorDashboardModel(AppDbContext context)
        {
            _context = context;
        }

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

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || userRole != "Diretor")
                return RedirectToPage("/Index");

            int userId = int.Parse(userIdStr);

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
                .Where(s => blocoIds.Contains(s.BlockId))
                .ToListAsync();

            var salaIds = Salas.Select(s => s.Id).ToList();

            // Totals
            TotalEscolas = Schools.Count;
            TotalSalas = Salas.Count;
            TotalContratos = await _context.Contratos.CountAsync(c => c.AgrupamentoId == agrupamentoId);
            
            DamagedEquipmentCount = await _context.Equipamentos
                .CountAsync(e => salaIds.Contains(e.RoomId) && e.Status == "Avariado");

            // Total Professores belonging to blocks in this Agrupamento
            TotalProfessores = await _context.Professores
                .CountAsync(p => blocoIds.Contains(p.BlocoId ?? 0));
            
            // Tickets pendentes (Filtered by schools in this Agrupamento)
            PendingTicketsCount = await _context.Tickets
                .Where(t => (t.Status == "Pendente" || t.TechnicianId == null) && t.SchoolId != null && schoolIds.Contains(t.SchoolId.Value))
                .CountAsync();

            // Low Stock alerts (Global as no school link exists in model)
            LowStockAlertsCount = await _context.StockEmpresa
                .Where(s => !s.IsAvailable)
                .CountAsync();

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

            return Page();
        }
    }
}
