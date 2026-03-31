using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
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
                .AsQueryable();

            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Todos os Estados")
            {
                query = query.Where(t => t.Status == FilterStatus);
            }

            Tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            AvailableSchools = await _context.Schools.OrderBy(s => s.Name).ToListAsync();
            AvailableEquipment = await _context.Equipamentos
                .Include(e => e.Room)
                .ThenInclude(r => r.Block)
                .Where(e => e.Status == "Avariado")
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
    }
}
