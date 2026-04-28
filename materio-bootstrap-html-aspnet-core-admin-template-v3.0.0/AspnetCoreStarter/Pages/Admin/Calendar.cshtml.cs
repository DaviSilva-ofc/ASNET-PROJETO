using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System;

namespace AspnetCoreStarter.Pages.Admin
{
    public class CalendarModel : PageModel
    {
        private readonly AppDbContext _context;

        public CalendarModel(AppDbContext context)
        {
            _context = context;
        }

        public string? CalendarEventsJson { get; set; }
        public List<User> Technicians { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Load Technicians
            Technicians = await _context.Users
                .Join(_context.Set<Tecnico>(), u => u.Id, t => t.UserId, (u, t) => u)
                .Where(u => !u.IsDeleted)
                .ToListAsync();

            // Fetch ALL tickets (Repairs + Administrative Tasks)
            // --- Schema Maintenance ---
            try {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET @dbname = DATABASE();
                    SET @tablename = 'tickets';
                    SET @columnname = 'data_agendamento';
                    SET @preparedStatement = (SELECT IF(
                      (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                       WHERE TABLE_SCHEMA = @dbname 
                         AND TABLE_NAME = @tablename 
                         AND COLUMN_NAME = @columnname) > 0,
                      'SELECT 1',
                      'ALTER TABLE tickets ADD COLUMN data_agendamento DATETIME NULL'
                    ));
                    PREPARE stmt FROM @preparedStatement;
                    EXECUTE stmt;
                    DEALLOCATE PREPARE stmt;
                ");
            } catch { }

            var tickets = await _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento)
                .Include(t => t.Technician)
                .Where(t => !t.IsDeleted)
                .ToListAsync();

            var calendarEvents = new List<object>();

            // Group Preventive Maintenances by date
            var preventiveGrouped = tickets
                .Where(t => t.Type == "Manutenção Preventiva" && t.ScheduledDate.HasValue)
                .GroupBy(t => t.ScheduledDate.Value.Date)
                .ToList();

            foreach (var group in preventiveGrouped)
            {
                var date = group.Key;
                calendarEvents.Add(new {
                    id = "preventive-" + date.ToString("yyyyMMdd"),
                    title = "🛡️ Semana de Manutenções",
                    start = date.ToString("yyyy-MM-dd"),
                    end = date.AddDays(7).ToString("yyyy-MM-dd"), // Extends for a full week (7 days)
                    allDay = true,
                    description = $"Semana dedicada a {group.Count()} manutenções preventivas.",
                    className = "bg-label-warning fw-bold"
                });
            }

            foreach (var t in tickets)
            {
                if (t.Type == "Manutenção Preventiva") continue;

                var techName = t.Technician?.Username ?? "Não atribuído";
                var isTask = t.Type == "Tarefa Administrativa";
                
                var type = t.Equipamento?.Type ?? "";
                var name = t.Equipamento?.Name ?? "";
                var emoji = isTask ? "📋" : type switch {
                    var s when s.Contains("Monitor") => "🖥️",
                    var s when s.Contains("Computador") || s.Contains("Desktop") || s.Contains("Portátil") || s.Contains("PC") => "💻",
                    var s when s.Contains("Impressora") => "🖨️",
                    var s when s.Contains("Rede") || s.Contains("Network") || s.Contains("Switch") || s.Contains("Router") => "🌐",
                    var s when s.Contains("Projetor") => "📽️",
                    var s when s.Contains("Rato") || s.Contains("Mouse") => "🖱️",
                    var s when s.Contains("Teclado") || s.Contains("Keyboard") => "⌨️",
                    var s when s.Contains("Servidor") || s.Contains("Server") => "🗄️",
                    var s when s.Contains("UPS") || s.Contains("Bateria") => "🔋",
                    var s when s.Contains("Som") || s.Contains("Coluna") || s.Contains("Speaker") => "🔊",
                    var s when s.Contains("Câmara") || s.Contains("Camera") => "📷",
                    _ => name.Contains("Monitor") ? "🖥️" : (name.Contains("Computador") ? "💻" : (name.Contains("Impressora") ? "🖨️" : "🔧"))
                };

                // Entry Date
                calendarEvents.Add(new {
                    id = t.Id,
                    title = isTask ? $"{emoji} Tarefa: {t.Description}" : $"{emoji} Pendente: {t.Equipamento?.Name ?? "Avaria"}",
                    start = t.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    description = isTask ? t.Description : $"Ticket #{t.Id}: {t.Description} em {t.School?.Name ?? "Sede"}",
                    className = isTask ? "bg-label-info" : "bg-label-primary",
                    extendedProps = new {
                        ticketId = t.Id,
                        technicianId = t.TechnicianId,
                        technicianName = techName,
                        isRepair = !isTask,
                        status = t.Status
                    }
                });

                // Completion Date
                if (t.CompletedAt.HasValue)
                {
                    calendarEvents.Add(new {
                        id = t.Id + 1000000,
                        title = $"✅ {emoji} Concluído: {(isTask ? "Tarefa" : t.Equipamento?.Name ?? "Avaria")}",
                        start = t.CompletedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                        description = $"{(isTask ? "Tarefa" : "Ticket")} #{t.Id} finalizado por {techName}.",
                        className = "bg-label-success",
                        extendedProps = new { ticketId = t.Id, status = "Concluído" }
                    });
                }
            }
            
            CalendarEventsJson = JsonSerializer.Serialize(calendarEvents);
            return Page();
        }

        public async Task<IActionResult> OnPostAssignTechnicianAsync(int ticketId, int technicianId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            ticket.TechnicianId = technicianId;
            // If it was just a "Pedido", move it to "Aceite" automatically if assigned by admin
            if (ticket.Status == "Pedido" || ticket.Status == "Pendente" || ticket.Status == "Aberto")
            {
                ticket.Status = "Aceite";
                ticket.AcceptedAt = DateTime.UtcNow;
            }

            var tech = await _context.Users.FindAsync(technicianId);
            
            // Log History
            _context.TicketHistorico.Add(new TicketHistorico {
                TicketId = ticketId,
                Acao = $"Técnico atribuído pelo Administrador: {tech?.Username}",
                TipoAcao = TipoAcaoHistorico.Status,
                Autor = User.Identity?.Name ?? "Admin",
                Data = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Técnico {tech?.Username} atribuído ao Ticket #{ticketId}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateTaskAsync(string description, DateTime date, int? technicianId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdStr, out int adminId);

            var newTask = new Ticket
            {
                Description = description,
                Type = "Tarefa Administrativa",
                Status = technicianId.HasValue ? "Aceite" : "Pendente",
                CreatedAt = date,
                AcceptedAt = technicianId.HasValue ? date : null,
                TechnicianId = technicianId,
                AdminId = adminId,
                Level = "Médio"
            };

            _context.Tickets.Add(newTask);
            await _context.SaveChangesAsync();

            if (technicianId.HasValue)
            {
                var tech = await _context.Users.FindAsync(technicianId);
                TempData["SuccessMessage"] = $"Tarefa criada e atribuída a {tech?.Username}.";
            }
            else
            {
                TempData["SuccessMessage"] = "Tarefa administrativa criada com sucesso.";
            }

            return RedirectToPage();
        }
    }
}
