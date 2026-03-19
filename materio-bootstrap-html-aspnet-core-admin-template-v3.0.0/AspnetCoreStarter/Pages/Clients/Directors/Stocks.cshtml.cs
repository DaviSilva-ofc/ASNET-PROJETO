using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorStocksModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorStocksModel(AppDbContext context)
        {
            _context = context;
        }

        public int CountEscolas { get; set; }
        public int CountSalas { get; set; }
        public int CountEquipamentos { get; set; }
        public int CountTickets { get; set; }

        public List<StockItemViewModel> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Find director's agrupamento
            var director = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (director == null || director.AgrupamentoId == null)
            {
                return Page();
            }

            int agrId = director.AgrupamentoId.Value;

            // Stats for the director
            var schools = await _context.Schools.Where(s => s.AgrupamentoId == agrId).ToListAsync();
            var schoolIds = schools.Select(s => s.Id).ToList();
            CountEscolas = schools.Count;

            var blocks = await _context.Blocos.Where(b => schoolIds.Contains(b.SchoolId)).ToListAsync();
            var blockIds = blocks.Select(b => b.Id).ToList();

            var rooms = await _context.Salas.Where(r => blockIds.Contains(r.BlockId)).ToListAsync();
            var roomIds = rooms.Select(r => r.Id).ToList();
            CountSalas = rooms.Count;

            var equips = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => roomIds.Contains(e.RoomId))
                .ToListAsync();
            CountEquipamentos = equips.Count;

            CountTickets = await _context.Tickets.CountAsync(t => t.SchoolId.HasValue && schoolIds.Contains(t.SchoolId.Value));

            // Load items for the table
            Items = equips.Select(e => new StockItemViewModel {
                Name = e.Name,
                Category = e.Type ?? "Equipamento",
                Location = $"{e.Room?.Name} ({e.Room?.Block?.School?.Name})",
                Quantity = 1,
                Status = "Em uso" // Simplified
            }).ToList();

            return Page();
        }
    }

    public class StockItemViewModel
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
    }
}
