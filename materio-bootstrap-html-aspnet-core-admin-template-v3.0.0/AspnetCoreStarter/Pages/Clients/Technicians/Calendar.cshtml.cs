using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class CalendarModel : PageModel
    {
        private readonly AppDbContext _context;

        public CalendarModel(AppDbContext context)
        {
            _context = context;
        }

        public string? CalendarEventsJson { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico"))
            {
                return RedirectToPage("/Auth/Login");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // 0. Schema Maintenance
            try {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS technician_activities (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        technician_id INT NOT NULL,
                        title VARCHAR(255) NOT NULL,
                        description TEXT,
                        activity_date DATETIME NOT NULL,
                        color VARCHAR(50),
                        data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP
                    );");
            } catch { }

            // Fetch ALL repair tickets for the calendar (including history)
            var allTickets = await _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento)
                .Where(t => t.TechnicianId == userId && t.Level != "Empréstimo")
                .ToListAsync();

            var calendarEvents = new List<object>();
            foreach (var t in allTickets)
            {
                string baseTitle = $"#{t.Id} - {(t.Equipamento?.Name ?? "Equip.")}";
                
                // 1. Entry Event (Created)
                calendarEvents.Add(new {
                   title = "📥 " + baseTitle,
                   start = t.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                   className = "bg-label-warning",
                   description = $"Equipamento: {t.Equipamento?.Name} na {t.School?.Name}. Criado em {t.CreatedAt:dd/MM HH:mm}."
                });

                // 2. Acceptance Event
                if (t.AcceptedAt.HasValue)
                {
                    calendarEvents.Add(new {
                        title = "🛠️ " + baseTitle,
                        start = t.AcceptedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                        className = "bg-label-primary",
                        description = $"Início da reparação em {t.AcceptedAt.Value:dd/MM HH:mm}."
                    });
                }

                // 3. Completion Event
                if (t.CompletedAt.HasValue)
                {
                    calendarEvents.Add(new {
                        title = "✅ " + baseTitle,
                        start = t.CompletedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                        className = "bg-label-success",
                        description = $"Resolvido/Concluído em {t.CompletedAt.Value:dd/MM HH:mm}."
                    });
                }
            }

            // Fetch Manual Activities
            var manualActivities = await _context.TechnicianActivities
                .Where(t => t.TechnicianId == userId)
                .ToListAsync();

            foreach (var act in manualActivities)
            {
                calendarEvents.Add(new {
                    id = "act_" + act.Id,
                    title = "📌 " + act.Title,
                    start = act.ActivityDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    className = "bg-label-primary", // Use the orange label style
                    description = act.Description ?? "Atividade registada manualmente."
                });
            }

            CalendarEventsJson = System.Text.Json.JsonSerializer.Serialize(calendarEvents);

            return Page();
        }

        public async Task<IActionResult> OnPostAddActivityAsync(string title, string? description, DateTime date)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var activity = new TechnicianActivity
            {
                TechnicianId = userId,
                Title = title,
                Description = description,
                ActivityDate = date,
                Color = "#f89223"
            };

            _context.TechnicianActivities.Add(activity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Atividade adicionada com sucesso!";
            return RedirectToPage();
        }
    }
}
