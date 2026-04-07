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

namespace AspnetCoreStarter.Pages.Clients.Professors
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public int TotalSalas { get; set; }
        public int TotalEquipamentos { get; set; }
        public int DamagedEquipmentCount { get; set; }
        public int PendingTicketsCount { get; set; }
        public int TotalUnreadMessages { get; set; }
        public List<AspnetCoreStarter.Models.User>? RecentMessageSenders { get; set; }
        public List<Ticket> RecentTickets { get; set; } = new();

        public List<int>? LineChartMonthlyData { get; set; }
        public List<string>? LineChartLabels { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || userRole != "Professor")
                return RedirectToPage("/Index");

            int userId = int.Parse(userIdStr);

            // Fetch the teacher's rooms
            var salas = await _context.Salas
                .Where(s => s.ResponsibleProfessorId == userId)
                .ToListAsync();
            
            var salaIds = salas.Select(s => s.Id).ToList();

            TotalSalas = salas.Count;
            
            TotalEquipamentos = await _context.Equipamentos
                .CountAsync(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value));

            DamagedEquipmentCount = await _context.Equipamentos
                .CountAsync(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value) && (e.Status == "Avariado" || e.Status == "Indisponível"));

            PendingTicketsCount = await _context.Tickets
                .Include(t => t.Equipamento)
                .CountAsync(t => t.Equipamento != null && t.Equipamento.RoomId.HasValue && salaIds.Contains(t.Equipamento.RoomId.Value) && (t.Status == "Pendente" || t.Status == "Pedido"));

            // Chat Notifications
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

            // Fetch Recent Tickets (Top 5)
            RecentTickets = await _context.Tickets
                .Include(t => t.Equipamento)
                .ThenInclude(e => e.Room)
                .Where(t => t.Equipamento != null && t.Equipamento.RoomId.HasValue && salaIds.Contains(t.Equipamento.RoomId.Value))
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Monthly Ticket Chart Data
            LineChartLabels = new List<string>();
            LineChartMonthlyData = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var label = date.ToString("MMM");
                LineChartLabels.Add(label);

                var count = await _context.Tickets
                    .Include(t => t.Equipamento)
                    .Where(t => t.Equipamento != null && t.Equipamento.RoomId.HasValue && salaIds.Contains(t.Equipamento.RoomId.Value) && t.CreatedAt.Month == date.Month && t.CreatedAt.Year == date.Year)
                    .CountAsync();
                LineChartMonthlyData.Add(count);
            }

            return Page();
        }
    }
}
