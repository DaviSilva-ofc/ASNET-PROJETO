using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public int TotalTicketsCount { get; set; }
        public int MyTicketsCount { get; set; }
        public int PendingTicketsCount { get; set; }
        public int MyStockAlertsCount { get; set; }
        public int UnreadMessagesCount { get; set; }

        public List<Ticket> RecentTickets { get; set; } = new();
        public List<StockTecnico> MyStock { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico"))
            {
                return RedirectToPage("/Auth/Login");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Counts
            TotalTicketsCount = await _context.Tickets.CountAsync();
            MyTicketsCount = await _context.Tickets.CountAsync(t => t.TechnicianId == userId);
            PendingTicketsCount = await _context.Tickets.CountAsync(t => t.Status == "Pendente");
            
            MyStockAlertsCount = await _context.StockTecnico
                .Where(s => s.TechnicianId == userId && !s.IsAvailable)
                .CountAsync();

            UnreadMessagesCount = await _context.Mensagens
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            // Fetch Recent Tickets (Global or Assigned)
            RecentTickets = await _context.Tickets
                .Include(t => t.School)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Fetch My Stock
            MyStock = await _context.StockTecnico
                .Where(s => s.TechnicianId == userId)
                .Take(5)
                .ToListAsync();

            return Page();
        }
    }
}
