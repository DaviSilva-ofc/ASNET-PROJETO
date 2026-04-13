using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class TicketsModel : PageModel
    {
        private readonly AppDbContext _context;

        public TicketsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Ticket> Tickets { get; set; } = new();
        public string ClientLocationsJson { get; set; } = "[]";

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterArticle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterSchoolId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterAgrupamentoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterEmpresaId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            var query = _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento).ThenInclude(e => e.Empresa)
                .Where(t => t.TechnicianId == userId && t.Level != "Empréstimo")
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

            if (FilterSchoolId.HasValue)
            {
                query = query.Where(t => t.SchoolId == FilterSchoolId.Value);
            }

            if (FilterAgrupamentoId.HasValue)
            {
                query = query.Where(t => t.School != null && t.School.AgrupamentoId == FilterAgrupamentoId.Value);
            }

            if (FilterEmpresaId.HasValue)
            {
                query = query.Where(t => t.Equipamento != null && t.Equipamento.EmpresaId == FilterEmpresaId.Value);
            }

            Tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            // Prepare Map Data for active tickets
            var mapLocations = new List<object>();
            foreach (var t in Tickets.Where(t => t.Status != "Concluído"))
            {
                if (t.School != null && !string.IsNullOrEmpty(t.School.Address))
                {
                    mapLocations.Add(new { name = t.School.Name, address = t.School.Address, type = "Escola" });
                }
                else if (t.Equipamento != null && t.Equipamento.Empresa != null && !string.IsNullOrEmpty(t.Equipamento.Empresa.Location))
                {
                    mapLocations.Add(new { name = t.Equipamento.Empresa.Name, address = t.Equipamento.Empresa.Location, type = "Empresa" });
                }
            }
            // Distinct locations to avoid duplicate markers
            var uniqueLocations = mapLocations.GroupBy(l => new { ((dynamic)l).name, ((dynamic)l).address }).Select(g => g.First()).ToList();
            ClientLocationsJson = System.Text.Json.JsonSerializer.Serialize(uniqueLocations);

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.TechnicianId == userId);
            if (ticket != null)
            {
                ticket.Status = newStatus;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"O estado do trabalho #{id} foi atualizado para '{newStatus}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Ticket não encontrado ou sem permissões.";
            }

            return RedirectToPage(new { FilterStatus = FilterStatus, FilterArticle = FilterArticle, FilterType = FilterType });
        }
    }
}
