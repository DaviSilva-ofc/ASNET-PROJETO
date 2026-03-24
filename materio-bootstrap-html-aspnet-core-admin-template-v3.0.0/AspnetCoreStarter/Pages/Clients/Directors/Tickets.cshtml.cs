using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorTicketsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorTicketsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Ticket> Tickets { get; set; } = new();
        public string? MyAgrupamentoName { get; set; }
        public Agrupamento? Agrupamento { get; set; }
        public List<AspnetCoreStarter.Models.School>? Schools { get; set; }
        public List<Bloco>? Blocos { get; set; }
        public List<Sala>? Salas { get; set; }
        public List<string> EquipmentTypes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Find the director's agrupamento
            var director = await _context.Diretores
                .Include(d => d.Agrupamento)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (director == null || director.AgrupamentoId == null)
            {
                Tickets = new List<Ticket>();
                return Page();
            }

            int myAgrupamentoId = director.AgrupamentoId.Value;
            Agrupamento = director.Agrupamento;
            MyAgrupamentoName = Agrupamento?.Name;

            // Get schools in this agrupamento
            Schools = await _context.Schools
                .Where(s => s.AgrupamentoId == myAgrupamentoId)
                .ToListAsync();

            var schoolIds = Schools.Select(s => s.Id).ToList();

            Blocos = await _context.Blocos
                .Where(b => schoolIds.Contains(b.SchoolId))
                .ToListAsync();

            var blocoIds = Blocos.Select(b => b.Id).ToList();

            Salas = await _context.Salas
                .Where(s => blocoIds.Contains(s.BlockId))
                .ToListAsync();

            EquipmentTypes = await _context.Equipamentos
                .Where(e => !string.IsNullOrEmpty(e.Type))
                .Select(e => e.Type)
                .Distinct()
                .ToListAsync();

            var query = _context.Tickets
                .Include(t => t.School)
                .Where(t => t.SchoolId.HasValue && schoolIds.Contains(t.SchoolId.Value))
                .AsQueryable();

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                query = query.Where(t => t.Status == FilterStatus);
            }

            Tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateTicketAsync(string level, string equipmentType, int schoolId, int blockId, int roomId)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var school = await _context.Schools.FindAsync(schoolId);
            var block = await _context.Blocos.FindAsync(blockId);
            var room = await _context.Salas.FindAsync(roomId);

            string fullDescription = $"[Detalhes da Localização]\nTipo: {equipmentType}\nEscola: {school?.Name ?? "N/A"}\nBloco: {block?.Name ?? "N/A"}\nSala: {room?.Name ?? "N/A"}";

            var ticket = new Ticket
            {
                Level = level,
                Description = fullDescription,
                Status = "Pedido",
                SchoolId = schoolId,
                AdminId = 1, // Dummy admin if required by constraints
                CreatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket de suporte submetido com sucesso!";
            return RedirectToPage();
        }
    }
}
