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

        // --- Panel Support ---
        public Ticket? SelectedTicket { get; set; }
        public List<TicketHistorico> TicketHistory { get; set; } = new();
        public List<Equipamento> AssociatedEquipment { get; set; } = new();
        public List<int> EqIdsWithActiveTickets { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedTicketId { get; set; }

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
                .Include(t => t.Technician)
                .Where(t => t.SchoolId != null && schoolIds.Contains(t.SchoolId.Value))
                .Where(t => t.Level != "Empréstimo"); // pedidos de empréstimo geridos no painel de Stocks

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

            // Available equipment for the "Novo Ticket" modal - ALL EQUIPMENT
            AvailableEquipment = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => e.Room != null && e.Room.Block != null && schoolIds.Contains(e.Room.Block.SchoolId))
                .OrderBy(e => e.Room.Block.School.Name)
                .ThenBy(e => e.Type)
                .ToListAsync();

            EqIdsWithActiveTickets = await _context.Tickets
                .Where(t => t.EquipamentoId.HasValue && t.Status != "Concluído" && t.Status != "Recusado")
                .Select(t => t.EquipamentoId!.Value)
                .ToListAsync();

            // --- Load Selected Ticket for Panel ---
            if (SelectedTicketId.HasValue)
            {
                SelectedTicket = await _context.Tickets
                    .Include(t => t.School).ThenInclude(s => s.Agrupamento)
                    .Include(t => t.RequestedBy)
                    .Include(t => t.Technician)
                    .Include(t => t.Equipamento)
                    .Include(t => t.UtilizedEquipments).ThenInclude(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School)
                    .FirstOrDefaultAsync(t => t.Id == SelectedTicketId.Value);

                if (SelectedTicket != null)
                {
                    TicketHistory = await _context.TicketHistorico
                        .Where(h => h.TicketId == SelectedTicketId.Value)
                        .OrderByDescending(h => h.Data)
                        .ToListAsync();

                    AssociatedEquipment = SelectedTicket.UtilizedEquipments.ToList();
                }
            }

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
            if (NewTicket.EquipamentoId.HasValue)
            {
                var alreadyOpen = await _context.Tickets.AnyAsync(t =>
                    t.EquipamentoId == NewTicket.EquipamentoId &&
                    t.Status != "Concluído" && t.Status != "Recusado" &&
                    t.Level != "Empréstimo");

                if (alreadyOpen)
                {
                    TempData["ErrorMessage"] = "Já existe um ticket ativo para este equipamento. Aguarde a conclusão antes de criar outro.";
                    return RedirectToPage();
                }
            }

            // Standardizing status to "Pedido"
            NewTicket.Status = "Pedido";
            NewTicket.CreatedAt = System.DateTime.UtcNow;
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr)) NewTicket.RequestedByUserId = int.Parse(userIdStr);
 
            _context.Tickets.Add(NewTicket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket criado com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int ticketId, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                var username = User.Identity?.Name ?? "Diretor";
                var history = new TicketHistorico
                {
                    TicketId = ticketId,
                    Acao = comment,
                    TipoAcao = TipoAcaoHistorico.Comentario,
                    Autor = username,
                    Data = DateTime.UtcNow
                };
                _context.TicketHistorico.Add(history);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { SelectedTicketId = ticketId });
        }
    }
}
