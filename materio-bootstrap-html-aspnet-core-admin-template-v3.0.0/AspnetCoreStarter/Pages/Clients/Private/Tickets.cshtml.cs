using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Private
{
    public class PrivateTicketsModel : PageModel
    {
        private readonly AppDbContext _context;

        public PrivateTicketsModel(AppDbContext context)
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

        public List<Equipamento> AvailableEquipment { get; set; } = new();
        public Empresa Empresa { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var user = await _context.Users.Include(u => u.Empresa).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.EmpresaId == null) return RedirectToPage("/Auth/Login");

            Empresa = user.Empresa;
            int empresaId = user.EmpresaId.Value;

            var query = _context.Tickets
                .Include(t => t.Equipamento)
                    .ThenInclude(e => e.Room)
                        .ThenInclude(r => r.Setor)
                .Include(t => t.Technician)
                .Where(t => t.Equipamento != null && t.Equipamento.EmpresaId == empresaId)
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

            AvailableEquipment = await _context.Equipamentos
                .Include(e => e.Room)
                .Where(e => e.EmpresaId == empresaId)
                .Where(e => e.Status == null || (!e.Status.Contains("repara") && !e.Status.Contains("manutenção")))
                .OrderBy(e => e.Name)
                .ToListAsync();

            if (eqId.HasValue)
            {
                NewTicket.EquipamentoId = eqId.Value;
                var eq = await _context.Equipamentos.Include(e => e.Room).FirstOrDefaultAsync(e => e.Id == eqId.Value);
                if (eq != null)
                {
                    NewTicket.Description = $"Reparação de {eq.Name} (S/N: {eq.SerialNumber})";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
        {
            var ticket = await _context.Tickets.Include(t => t.Equipamento).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket != null && !string.IsNullOrEmpty(newStatus))
            {
                ticket.Status = newStatus;
                
                // Complete lifecycle: If resolved, fix the equipment
                if (newStatus == "Concluído" && ticket.Equipamento != null)
                {
                    ticket.Equipamento.Status = "A funcionar";
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"O estado do ticket #{id} foi atualizado para '{newStatus}'.";
            }
            return RedirectToPage(new { FilterStatus = FilterStatus });
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(int.Parse(userIdStr!));
            
            if (NewTicket.EquipamentoId.HasValue)
            {
                var alreadyOpen = await _context.Tickets.AnyAsync(t =>
                    t.EquipamentoId == NewTicket.EquipamentoId &&
                    t.Status != "Concluído" && t.Status != "Recusado");

                if (alreadyOpen)
                {
                    TempData["ErrorMessage"] = "Já existe um ticket ativo para este equipamento.";
                    return RedirectToPage();
                }
            }

            NewTicket.AdminId = user!.Id; // Using the logged in user as reporter
            NewTicket.Status = "Pendente";
            NewTicket.CreatedAt = DateTime.UtcNow;
            
            _context.Tickets.Add(NewTicket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket criado com sucesso!";
            return RedirectToPage();
        }
    }
}
