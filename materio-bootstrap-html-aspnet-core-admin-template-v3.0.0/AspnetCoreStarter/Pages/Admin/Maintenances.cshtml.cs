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
    public class MaintenancesModel : PageModel
    {
        private readonly AppDbContext _context;

        public MaintenancesModel(AppDbContext context)
        {
            _context = context;
        }

        public List<AgrupamentoGroup> MyActiveMaintenances { get; set; } = new();
        public List<AgrupamentoGroup> MyCompletedMaintenances { get; set; } = new();
        
        public List<AgrupamentoGroup> GlobalActiveMaintenances { get; set; } = new();
        public List<AgrupamentoGroup> GlobalCompletedMaintenances { get; set; } = new();
        public List<AgrupamentoGroup> AvailableMaintenances { get; set; } = new();
        public List<User> AvailableTechnicians { get; set; } = new();
        
        // Statistics
        public int TotalMaintenancesCount { get; set; }
        public int PendingMaintenancesCount { get; set; }
        public int InProgressMaintenancesCount { get; set; }
        public int CompletedMaintenancesCount { get; set; }

        public class AgrupamentoGroup
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            public List<SchoolGroup> Schools { get; set; } = new();
        }

        public class SchoolGroup
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            public List<Ticket> Tickets { get; set; } = new();
            public bool AllCompleted => Tickets.All(t => t.Status == "Concluído");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin")) 
                return RedirectToPage("/Auth/Login");

            var sessionUserId = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            // Fetch ALL preventive maintenance tickets
            var allPreventiveTickets = await _context.Tickets
                .Include(t => t.School).ThenInclude(s => s.Agrupamento)
                .Include(t => t.Equipamento).ThenInclude(e => e.Empresa)
                .Include(t => t.Equipamento).ThenInclude(e => e.Room)
                .Include(t => t.Technician)
                .Where(t => t.Type == "Manutenção Preventiva")
                .ToListAsync();

            // My Maintenances
            MyActiveMaintenances = GroupTickets(allPreventiveTickets.Where(t => t.TechnicianId == userId && t.Status != "Concluído").ToList());
            MyCompletedMaintenances = GroupTickets(allPreventiveTickets.Where(t => t.TechnicianId == userId && t.Status == "Concluído").ToList());

            // Global View
            GlobalActiveMaintenances = GroupTickets(allPreventiveTickets.Where(t => t.TechnicianId != null && t.Status != "Concluído").ToList());
            GlobalCompletedMaintenances = GroupTickets(allPreventiveTickets.Where(t => t.Status == "Concluído").ToList());
            AvailableMaintenances = GroupTickets(allPreventiveTickets.Where(t => t.TechnicianId == null).ToList());

            // Available Technicians
            AvailableTechnicians = await _context.Users
                .Join(_context.Tecnicos, u => u.Id, te => te.UserId, (u, te) => u)
                .ToListAsync();

            // Calculate stats
            TotalMaintenancesCount = allPreventiveTickets.Count;
            PendingMaintenancesCount = allPreventiveTickets.Count(t => (t.Status == "Pendente" || t.Status == "Aberto") && t.Status != "Concluído");
            InProgressMaintenancesCount = allPreventiveTickets.Count(t => (t.Status == "Aceite" || t.Status == "Em reparação" || t.Status == "Em Progresso") && t.Status != "Concluído");
            CompletedMaintenancesCount = allPreventiveTickets.Count(t => t.Status == "Concluído");

            return Page();
        }

        private List<AgrupamentoGroup> GroupTickets(List<Ticket> tickets)
        {
            return tickets
                .GroupBy(t => t.School?.AgrupamentoId)
                .Select(agGroup => new AgrupamentoGroup
                {
                    Id = agGroup.Key,
                    Name = agGroup.FirstOrDefault()?.School?.Agrupamento?.Name ?? "Outros / Sem Agrupamento",
                    Schools = agGroup
                        .GroupBy(t => t.SchoolId)
                        .Select(sGroup => new SchoolGroup
                        {
                            Id = sGroup.Key,
                            Name = sGroup.FirstOrDefault()?.School?.Name ?? "Sede / Geral",
                            Tickets = sGroup.OrderByDescending(t => t.CreatedAt).ToList()
                        })
                        .OrderBy(s => s.Name)
                        .ToList()
                })
                .OrderBy(a => a.Name)
                .ToList();
        }

        public async Task<IActionResult> OnPostAcceptAsync(int id)
        {
            var sessionUserId = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            await EnsureAdminIsRegisteredAsTech(userId);

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.TechnicianId == null);
            if (ticket != null)
            {
                ticket.TechnicianId = userId;
                ticket.Status = "Aceite";
                ticket.AcceptedAt = System.DateTime.UtcNow;
                await LogHistory(id, "Manutenção aceite pelo Administrador", TipoAcaoHistorico.Status);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Aceitou a manutenção #{id} como responsável.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignAgrupamentoAsync(int agrupamentoId, int? technicianId)
        {
            var sessionUserId = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            int targetUserId = technicianId ?? userId;
            
            // If target is Admin, ensure registered as tech
            if (targetUserId == userId) await EnsureAdminIsRegisteredAsTech(userId);

            // Find all unassigned preventive tickets in this agrupamento
            var tickets = await _context.Tickets
                .Where(t => t.School.AgrupamentoId == agrupamentoId && t.TechnicianId == null && t.Type == "Manutenção Preventiva")
                .ToListAsync();

            if (tickets.Any())
            {
                foreach (var ticket in tickets)
                {
                    ticket.TechnicianId = targetUserId;
                    ticket.Status = "Aceite";
                    ticket.AcceptedAt = System.DateTime.UtcNow;
                    await LogHistory(ticket.Id, $"Atribuído em massa por Agrupamento ao utilizador ID {targetUserId}", TipoAcaoHistorico.Status);
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Atribuiu {tickets.Count} manutenções do Agrupamento com sucesso!";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteSchoolAsync(int schoolId)
        {
            var sessionUserId = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            var tickets = await _context.Tickets
                .Where(t => t.SchoolId == schoolId && t.TechnicianId == userId && t.Type == "Manutenção Preventiva" && t.Status != "Concluído")
                .ToListAsync();

            if (tickets.Any())
            {
                foreach (var ticket in tickets)
                {
                    ticket.Status = "Concluído";
                    ticket.CompletedAt = System.DateTime.UtcNow;
                    await LogHistory(ticket.Id, "Manutenção concluída pelo Admin", TipoAcaoHistorico.Status);
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Concluiu {tickets.Count} manutenções sob sua responsabilidade!";
            }
            return RedirectToPage();
        }

        private async Task EnsureAdminIsRegisteredAsTech(int userId)
        {
            var isRegisteredAsTech = await _context.Tecnicos.AnyAsync(te => te.UserId == userId);
            if (!isRegisteredAsTech)
            {
                var newTechEntry = new Tecnico { UserId = userId, AreaTecnica = "Administração / Gestão", Nivel = "Admin" };
                _context.Tecnicos.Add(newTechEntry);
                await _context.SaveChangesAsync();
            }
        }

        private async Task LogHistory(int ticketId, string action, TipoAcaoHistorico type)
        {
            var history = new TicketHistorico
            {
                TicketId = ticketId,
                Acao = action,
                TipoAcao = type,
                Autor = User.Identity?.Name ?? "Admin",
                Data = System.DateTime.UtcNow
            };
            _context.TicketHistorico.Add(history);
        }
    }
}
