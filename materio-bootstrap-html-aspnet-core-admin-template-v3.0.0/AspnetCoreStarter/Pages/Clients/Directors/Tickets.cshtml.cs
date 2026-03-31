using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

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
        public List<School> Schools { get; set; } = new();
        public List<Equipamento> AvailableEquipment { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? eqId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterArticle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        public string? MyAgrupamentoName { get; set; }

        [BindProperty]
        public Ticket NewTicket { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Diretor")) 
                return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Correctly query the Diretores table for AgrupamentoId
            var directorInfo = await _context.Diretores
                .Include(d => d.Agrupamento)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (directorInfo == null || directorInfo.AgrupamentoId == null)
            {
                Tickets = new List<Ticket>();
                return Page();
            }

            int directorAgrupamentoId = directorInfo.AgrupamentoId.Value;
            MyAgrupamentoName = directorInfo.Agrupamento?.Name;

            // Get schools in this agrupamento
            Schools = await _context.Schools
                .Where(s => s.AgrupamentoId == directorAgrupamentoId)
                .ToListAsync();

            var schoolIds = Schools.Select(s => s.Id).ToList();

            // Filter tickets
            var query = _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento)
                .Where(t => t.SchoolId != null && schoolIds.Contains(t.SchoolId.Value));

            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Todos os Estados")
            {
                query = query.Where(t => t.Status == FilterStatus);
            }

            var ticketList = await query.Include(t => t.Equipamento).ToListAsync();

            if (!string.IsNullOrEmpty(FilterArticle))
            {
                ticketList = ticketList.Where(t => t.Equipamento != null && NormalizeEquipmentName(t.Equipamento.Name) == FilterArticle).ToList();
            }

            if (!string.IsNullOrEmpty(FilterType))
            {
                ticketList = ticketList.Where(t => t.Equipamento != null && t.Equipamento.Type == FilterType).ToList();
            }

            Tickets = ticketList.OrderByDescending(t => t.CreatedAt).ToList();

            // Handle eqId if present
            if (eqId.HasValue)
            {
                var eq = await _context.Equipamentos
                    .Include(e => e.Room)
                        .ThenInclude(r => r.Block)
                            .ThenInclude(b => b.School)
                    .FirstOrDefaultAsync(e => e.Id == eqId.Value);

                if (eq != null && eq.Room?.Block?.School != null)
                {
                    NewTicket.EquipamentoId = eq.Id;
                    NewTicket.SchoolId = eq.Room.Block.School.Id;
                }
            }

            // Available equipment for the "Novo Ticket" modal - ONLY AVARIADO
            AvailableEquipment = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => e.Room != null && e.Room.Block != null && schoolIds.Contains(e.Room.Block.SchoolId))
                .Where(e => e.Status == "Avariado")
                .OrderBy(e => e.Room.Block.School.Name)
                .ThenBy(e => e.Type)
                .ToListAsync();

            return Page();
        }

        private static string NormalizeEquipmentName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Desconhecido";
            string normalized = name.Trim();
            var pluralMaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Computadores", "Computador" },
                { "Monitores", "Monitor" },
                { "Impressoras", "Impressora" },
                { "Projetores", "Projetor" },
                { "Televisores", "Televisão" },
                { "Portáteis", "Portátil" },
                { "Ratos", "Rato" },
                { "Teclados", "Teclado" },
                { "UPSs", "UPS" },
                { "Switches", "Switch" },
                { "Quadros Interativos", "Quadro Interativo" },
                { "Mesas Interativas", "Mesa Interativa" }
            };

            if (pluralMaps.TryGetValue(normalized, out var singular)) return singular;
            if (normalized.EndsWith("ores", StringComparison.OrdinalIgnoreCase)) return normalized.Substring(0, normalized.Length - 2);
            if (normalized.EndsWith("adores", StringComparison.OrdinalIgnoreCase)) return normalized.Substring(0, normalized.Length - 2);
            if (normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !normalized.EndsWith("ss", StringComparison.OrdinalIgnoreCase) && normalized.Length > 4)
                return normalized.Substring(0, normalized.Length - 1);

            return normalized;
        }

        public async Task<IActionResult> OnPostCreateTicketAsync()
        {
            // Standardizing status to "Pendente"
            NewTicket.Status = "Pendente";
            NewTicket.CreatedAt = System.DateTime.UtcNow;

            _context.Tickets.Add(NewTicket);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
