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
        public CoordinatorDashboardModel(AppDbContext context) => _context = context;

        public string? SchoolName { get; set; }
        public int TotalSalas { get; set; }
        public int TotalEquipamentos { get; set; }
        public int DamagedCount { get; set; }
        public int PendingTickets { get; set; }
        public int TotalProfessores { get; set; }
        public int TotalUnreadMessages { get; set; }
        public List<User>? RecentSenders { get; set; }
        public List<int>? ChartData { get; set; }
        public List<string>? ChartLabels { get; set; }
        public School? MySchool { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity!.IsAuthenticated) return RedirectToPage("/Auth/Login");
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");
            if (User.FindFirst(ClaimTypes.Role)?.Value != "Coordenador") return RedirectToPage("/Index");

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (coord?.SchoolId == null) return Page();

            int schoolId = coord.SchoolId.Value;
            MySchool = coord.School;
            SchoolName = coord.School?.Name ?? "Escola";

            var blocos = await _context.Blocos.Where(b => b.SchoolId == schoolId).ToListAsync();
            var blocoIds = blocos.Select(b => b.Id).ToList();

            var salas = await _context.Salas.Where(s => blocoIds.Contains(s.BlockId)).ToListAsync();
            var salaIds = salas.Select(s => s.Id).ToList();

            TotalSalas = salas.Count;
            TotalEquipamentos = await _context.Equipamentos.CountAsync(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value));
            DamagedCount = await _context.Equipamentos.CountAsync(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value) && e.Status == "Avariado");
            TotalProfessores = await _context.Professores.CountAsync(p => blocoIds.Contains(p.BlocoId ?? 0));
            PendingTickets = await _context.Tickets.CountAsync(t => t.SchoolId == schoolId && (t.Status == "Pendente" || t.Status == "Pedido"));

            var unread = await _context.Mensagens
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            TotalUnreadMessages = unread.Count;
            RecentSenders = unread.Select(m => m.Sender).GroupBy(u => u.Id).Select(g => g.First()).Take(5).ToList()!;

            ChartLabels = new List<string>();
            ChartData = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var d = DateTime.Now.AddMonths(-i);
                ChartLabels.Add(d.ToString("MMM"));
                ChartData.Add(await _context.Tickets.CountAsync(t => t.SchoolId == schoolId && t.CreatedAt.Month == d.Month && t.CreatedAt.Year == d.Year));
            }

            return Page();
        }
    }
}
