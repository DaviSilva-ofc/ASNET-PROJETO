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

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public int MyTicketsCount { get; set; }
        public int PendingTicketsCount { get; set; }
        public int MyStockAlertsCount { get; set; }
        public int UnreadMessagesCount { get; set; }
        public int GlobalAvailableStockCount { get; set; }

        public List<Ticket> RecentTickets { get; set; } = new();
        public List<StockTecnico> MyStock { get; set; } = new();

        public List<School> LocaisAtendimento { get; set; } = new();
        public List<AspnetCoreStarter.Models.User> RecentMessageSenders { get; set; } = new();
        
        public List<string> LineChartLabels { get; set; } = new();
        public (List<int> Pendente, List<int> EmResolucao, List<int> Concluido) LineChartMonthlyData { get; set; } 
            = (new List<int>(), new List<int>(), new List<int>());

        public List<string> BarChartLabels { get; set; } = new() { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };
        public List<int> BarChartData { get; set; } = new();

        public string? ClientLocationsJson { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico"))
            {
                return RedirectToPage("/Auth/Login");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Counts
            MyTicketsCount = await _context.Tickets.CountAsync(t => t.TechnicianId == userId);
            PendingTicketsCount = await _context.Tickets.CountAsync(t => t.TechnicianId == userId && (t.Status == "Pendente" || t.Status == "Em Resolução"));
            
            MyStockAlertsCount = await _context.StockTecnico
                .Where(s => s.TechnicianId == userId && !s.IsAvailable)
                .CountAsync();

            UnreadMessagesCount = await _context.Mensagens
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            GlobalAvailableStockCount = await _context.StockEmpresa
                .CountAsync(s => s.Status == "Armazenado" || s.Status == "Disponível");

            // Fetch Recent Tickets (Assigned)
            RecentTickets = await _context.Tickets
                .Include(t => t.School)
                .Where(t => t.TechnicianId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Fetch My Stock
            MyStock = await _context.StockTecnico
                .Where(s => s.TechnicianId == userId)
                .OrderByDescending(s => !s.IsAvailable)
                .ToListAsync();

            // Fetch Locais Atendimento (Schools or Empresas where the technician has assigned tickets)
            var schoolIds = await _context.Tickets
                .Where(t => t.TechnicianId == userId && t.SchoolId.HasValue && (t.Status == "Pendente" || t.Status == "Em Resolução"))
                .Select(t => t.SchoolId.Value)
                .Distinct()
                .ToListAsync();

            LocaisAtendimento = await _context.Schools
                .Where(s => schoolIds.Contains(s.Id))
                .ToListAsync();

            // Chat Senders
            var unreadMessages = await _context.Mensagens
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();
            RecentMessageSenders = unreadMessages.Select(m => m.Sender).GroupBy(s => s.Id).Select(g => g.First()).Take(5).ToList();

            // Line Chart Analytics (Últimos 6 meses contínuos)
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                LineChartLabels.Add(date.ToString("MMM"));

                var ticketsThisMonth = await _context.Tickets
                    .Where(t => t.TechnicianId == userId && t.CreatedAt.Month == date.Month && t.CreatedAt.Year == date.Year)
                    .ToListAsync();

                LineChartMonthlyData.Pendente.Add(ticketsThisMonth.Count(t => t.Status == "Pendente"));
                LineChartMonthlyData.EmResolucao.Add(ticketsThisMonth.Count(t => t.Status == "Em Resolução"));
                LineChartMonthlyData.Concluido.Add(ticketsThisMonth.Count(t => t.Status == "Concluído"));
            }

            // Bar Chart Analytics (Visão Anual - Janeiro a Dezembro do ano atual)
            int currentYear = DateTime.Now.Year;
            var completedTicketsThisYear = await _context.Tickets
                .Where(t => t.TechnicianId == userId && t.Status == "Concluído" && t.CreatedAt.Year == currentYear)
                .ToListAsync();

            for (int month = 1; month <= 12; month++)
            {
                BarChartData.Add(completedTicketsThisYear.Count(t => t.CreatedAt.Month == month));
            }

            // Map Locations (Escolas associadas aos tickets pendentes deles)
            var locations = LocaisAtendimento
                .Where(s => !string.IsNullOrEmpty(s.Address))
                .Select(s => new { name = s.Name, address = s.Address })
                .ToList();
            ClientLocationsJson = System.Text.Json.JsonSerializer.Serialize(locations);

            return Page();
        }
    }
}
