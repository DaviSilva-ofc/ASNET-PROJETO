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

            EquipamentosDisponiveis = await _context.Equipamentos
                .Include(e => e.Room)
                .Where(e => e.Room != null && e.Room.Block.SchoolId == coord.SchoolId && (e.Status == "Avariado" || e.Status == "Indisponível"))
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

            return Page();
        }

        public async Task<IActionResult> OnPostCreateTicketAsync()
        {
            if (!ModelState.IsValid) return RedirectToPage();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");

            NovoTicket.AdminId = int.Parse(userIdStr);
            NovoTicket.Status = "Pedido";
            NovoTicket.CreatedAt = DateTime.Now;

            _context.Tickets.Add(NovoTicket);

            var equipment = await _context.Equipamentos.FindAsync(NovoTicket.EquipamentoId);
            if (equipment != null && (equipment.Status == "A funcionar" || equipment.Status == "Funcionando"))
            {
                equipment.Status = "Avariado";
            }

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
    }
}
