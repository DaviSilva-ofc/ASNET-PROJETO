using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using System;

namespace AspnetCoreStarter.Pages.Clients.Coordinators
{
    public class CoordinatorTicketsModel : PageModel
    {
        private readonly AppDbContext _context;

        public CoordinatorTicketsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Ticket> Tickets { get; set; } = new();
        public School? MySchool { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EqId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterArticle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty]
        public Ticket NovoTicket { get; set; } = new();

        public List<Equipamento> EquipamentosDisponiveis { get; set; } = new();

        // --- Panel Support ---
        public Ticket? SelectedTicket { get; set; }
        public List<TicketHistorico> TicketHistory { get; set; } = new();
        public List<Equipamento> AssociatedEquipment { get; set; } = new();
        public List<int> EqIdsWithActiveTickets { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedTicketId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || User.FindFirst(ClaimTypes.Role)?.Value != "Coordenador")
                return RedirectToPage("/Index");

            int userId = int.Parse(userIdStr);

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (coord?.SchoolId == null) return Page();

            MySchool = coord.School;

            var query = _context.Tickets
                .Include(t => t.Equipamento)
                .Include(t => t.Admin)
                .Include(t => t.Technician)
                .Where(t => t.SchoolId == coord.SchoolId)
                .Where(t => t.Level != "Empréstimo") // pedidos de empréstimo geridos no painel de Stocks
                .AsQueryable();

            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Todos os Estados")
            {
                query = query.Where(t => t.Status == FilterStatus);
            }

            if (!string.IsNullOrEmpty(FilterArticle))
            {
                query = query.Where(t => t.Equipamento != null && t.Equipamento.Name == FilterArticle);
            }

            if (!string.IsNullOrEmpty(FilterType))
            {
                query = query.Where(t => t.Equipamento != null && t.Equipamento.Type == FilterType);
            }

            Tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            var eqIdsWithActiveTickets = await _context.Tickets
                .Where(t => t.EquipamentoId.HasValue && t.Status != "Concluído" && t.Status != "Recusado")
                .Select(t => t.EquipamentoId!.Value)
                .ToListAsync();

            EquipamentosDisponiveis = await _context.Equipamentos
                .Include(e => e.Room)
                .Where(e => e.Room != null && e.Room.Block.SchoolId == coord.SchoolId)
                .Where(e => e.Status == null || (!e.Status.Contains("repara") && !e.Status.Contains("manutenção")))
                .OrderBy(e => e.Name)
                .ToListAsync();

            if (EqId.HasValue)
            {
                NovoTicket.EquipamentoId = EqId.Value;
                var eq = await _context.Equipamentos
                    .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                    .FirstOrDefaultAsync(e => e.Id == EqId.Value);

                if (eq != null)
                {
                    NovoTicket.SchoolId = coord.SchoolId;
                    NovoTicket.Description = $"Reparação de {eq.Name} (S/N: {eq.SerialNumber}) em {eq.Room?.Name}";
                }
            }

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

            EqIdsWithActiveTickets = await _context.Tickets
                .Where(t => t.EquipamentoId.HasValue && t.Status != "Concluído" && t.Status != "Recusado")
                .Select(t => t.EquipamentoId!.Value)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateTicketAsync()
        {
            if (!ModelState.IsValid) return RedirectToPage();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");

            if (NovoTicket.EquipamentoId.HasValue)
            {
                var alreadyOpen = await _context.Tickets.AnyAsync(t =>
                    t.EquipamentoId == NovoTicket.EquipamentoId &&
                    t.Status != "Concluído" && t.Status != "Recusado" &&
                    t.Level != "Empréstimo");

                if (alreadyOpen)
                {
                    TempData["ErrorMessage"] = "Já existe um ticket ativo para este equipamento. Aguarde a conclusão antes de criar outro.";
                    return RedirectToPage();
                }
            }

            NovoTicket.AdminId = int.Parse(userIdStr);
            NovoTicket.RequestedByUserId = int.Parse(userIdStr);
            NovoTicket.Status = "Pendente";
            NovoTicket.CreatedAt = DateTime.Now;

            _context.Tickets.Add(NovoTicket);

            // Only auto-mark as Avariado if creating a repair-type ticket and equipment is currently working
            // For other ticket types (software, etc.), preserve the current status

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ticket de assistência criado com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int ticketId, string status)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSubmitEvaluationAsync(int ticketId, int rating, string? feedback)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null || ticket.Status != "Concluído" || ticket.SatisfacaoRating != null)
            {
                return RedirectToPage();
            }

            ticket.SatisfacaoRating = rating;
            ticket.SatisfacaoFeedback = feedback;
            ticket.DataAvaliacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Agradecemos o seu feedback sobre o suporte técnico!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int ticketId, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                var username = User.Identity?.Name ?? "Coordenador";
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
