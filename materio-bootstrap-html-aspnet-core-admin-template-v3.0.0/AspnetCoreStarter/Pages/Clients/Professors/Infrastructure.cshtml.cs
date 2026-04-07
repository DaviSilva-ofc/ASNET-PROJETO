using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Professors
{
    public class InfrastructureModel : PageModel
    {
        private readonly AppDbContext _context;

        public InfrastructureModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Sala> MySalas { get; set; } = new();
        public Agrupamento? Agrupamento { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");

            int userId = int.Parse(userIdStr);

            // Fetch rooms with full hierarchy
            MySalas = await _context.Salas
                .Include(s => s.Block)
                    .ThenInclude(b => b.School)
                        .ThenInclude(sch => sch.Agrupamento)
                .Include(s => s.Equipments)
                .Where(s => s.ResponsibleProfessorId == userId)
                .ToListAsync();

            if (MySalas.Any())
            {
                Agrupamento = MySalas.First().Block.School.Agrupamento;
                
                var mySalaIds = MySalas.Select(s => s.Id).Cast<int?>().ToList();
                
                TotalTickets = await _context.Tickets
                    .Include(t => t.Equipamento)
                    .Where(t => t.EquipamentoId.HasValue && mySalaIds.Contains(t.Equipamento.RoomId) && t.Status != "Fechado")
                    .CountAsync();
                    
                TotalEquipments = await _context.Equipamentos
                    .Where(e => e.RoomId.HasValue && mySalaIds.Contains(e.RoomId))
                    .CountAsync();
            }

            return Page();
        }

        public int TotalTickets { get; set; }
        public int TotalEquipments { get; set; }
    }
}
