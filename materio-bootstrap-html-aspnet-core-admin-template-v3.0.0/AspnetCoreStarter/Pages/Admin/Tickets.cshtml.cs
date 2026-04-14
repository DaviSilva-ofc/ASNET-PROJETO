using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Admin
{
    public class TicketsModel : PageModel
    {
        private readonly AppDbContext _context;

        public TicketsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Ticket> Tickets { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? eqId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? FilterArticle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty]
        public Ticket NewTicket { get; set; } = new();

        public List<School> AvailableSchools { get; set; } = new();
        public List<Equipamento> AvailableEquipment { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && userRole != "Tecnico") return RedirectToPage("/Index");

            var query = _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Admin)
                .Include(t => t.Equipamento)
                .Include(t => t.Technician)
                .Where(t => t.Level != "Empréstimo") // pedidos de stock são geridos no painel de Stocks
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

            AvailableSchools = await _context.Schools.OrderBy(s => s.Name).ToListAsync();
            var eqIdsWithActiveTickets = await _context.Tickets
                .Where(t => t.EquipamentoId.HasValue && t.Status != "Concluído" && t.Status != "Recusado")
                .Select(t => t.EquipamentoId!.Value)
                .ToListAsync();

            AvailableEquipment = await _context.Equipamentos
                .Include(e => e.Room)
                .ThenInclude(r => r.Block)
                .Where(e => e.Status == null || (!e.Status.Contains("repara") && !e.Status.Contains("manutenção")))
                .OrderBy(e => e.Name)
                .ToListAsync();

            if (eqId.HasValue)
            {
                NewTicket.EquipamentoId = eqId.Value;
                var eq = await _context.Equipamentos.Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School).FirstOrDefaultAsync(e => e.Id == eqId.Value);
                if (eq != null)
                {
                    NewTicket.SchoolId = eq.Room?.Block?.SchoolId;
                    NewTicket.Description = $"Reparação de {eq.Name} (S/N: {eq.SerialNumber})";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                ticket.Status = newStatus;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"O estado do ticket #{id} foi atualizado para '{newStatus}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Ticket não encontrado.";
            }

            return RedirectToPage(new { FilterStatus = FilterStatus });
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Falta preencher campos obrigatórios.";
                return RedirectToPage();
            }

            // Block if there's already an active ticket for this equipment
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

            var userId = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int adminId))
            {
                NewTicket.AdminId = adminId;
            }

            NewTicket.Status = "Pendente";
            _context.Tickets.Add(NewTicket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket criado com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostHandleLoanRequestAsync(int id, string actionType)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null || ticket.Level != "Empréstimo" || ticket.Status != "Pedido") 
                return RedirectToPage();

            if (actionType == "Aprovar")
            {
                ticket.Status = "Concluído";
                ticket.Description += "\n\n[RESOLVIDO: PEDIDO APROVADO - O Administrador irá providenciar o stock.]";
            }
            else
            {
                ticket.Status = "Recusado";
                ticket.Description += "\n\n[RESOLVIDO: PEDIDO RECUSADO]";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Pedido de Stock {actionType.ToLower()} com sucesso!";
            return RedirectToPage(new { FilterStatus = FilterStatus });
        }
    }
}
