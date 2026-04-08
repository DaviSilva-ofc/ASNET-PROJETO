using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Coordinators
{
    public class CoordinatorInfrastructureModel : PageModel
    {
        private readonly AppDbContext _context;

        public CoordinatorInfrastructureModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Bloco> MyBlocks { get; set; } = new();
        public School? MySchool { get; set; }
        public Agrupamento? MyAgrupamento { get; set; }
        public string? DirectorName { get; set; }
        public int TicketCount { get; set; }
        public int RoomCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity!.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || User.FindFirst(ClaimTypes.Role)?.Value != "Coordenador")
                return RedirectToPage("/Index");

            int userId = int.Parse(userIdStr);

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                    .ThenInclude(s => s.Agrupamento)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (coord?.SchoolId == null) return Page();

            MySchool = coord.School;
            MyAgrupamento = coord.School?.Agrupamento;

            // Fetch Director Name for the Agrupamento
            if (MyAgrupamento != null)
            {
                var director = await _context.Diretores
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.AgrupamentoId == MyAgrupamento.Id);
                DirectorName = director?.User?.Username ?? "Sem Diretor Atribuído";
            }

            // Ticket Count (Active for this school)
            TicketCount = await _context.Tickets
                .CountAsync(t => t.SchoolId == coord.SchoolId && t.Status != "Fechado" && t.Status != "Resolvido");

            // Infrastructure tree
            MyBlocks = await _context.Blocos
                .Include(b => b.Rooms.OrderBy(r => r.Name))
                    .ThenInclude(r => r.Equipments)
                .Where(b => b.SchoolId == coord.SchoolId)
                .OrderBy(b => b.Name)
                .ToListAsync();

            RoomCount = MyBlocks.SelectMany(b => b.Rooms).Count();

            return Page();
        }
    }
}
