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
    public class MaintenancesModel : PageModel
    {
        private readonly AppDbContext _context;

        public MaintenancesModel(AppDbContext context)
        {
            _context = context;
        }

        public List<AgrupamentoGroup> MyActiveMaintenances { get; set; } = new();
        public List<AgrupamentoGroup> MyCompletedMaintenances { get; set; } = new();
        
        // Statistics
        public int TotalMaintenances { get; set; }
        public int PendingMaintenances { get; set; }
        public int InProgressMaintenances { get; set; }
        public int CompletedMaintenances { get; set; }

        // --- Panel Support ---
        public Ticket? SelectedTicket { get; set; }
        public List<TicketHistorico> TicketHistory { get; set; } = new();
        public List<Equipamento> AssociatedEquipment { get; set; } = new();
        public List<Equipamento> AvailableEquipment { get; set; } = new();
        public List<Equipamento> SchoolEquipment { get; set; } = new();
        public int CountStock { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedTicketId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string StockSearch { get; set; } = "";
        [BindProperty(SupportsGet = true)]
        public string StockScope { get; set; } = "Escola";

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
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            // Fetch maintenance tickets assigned to this technician
            var myTickets = await _context.Tickets
                .Include(t => t.School).ThenInclude(s => s.Agrupamento)
                .Include(t => t.Equipamento).ThenInclude(e => e.Empresa)
                .Include(t => t.Equipamento).ThenInclude(e => e.Room)
                .Where(t => t.TechnicianId == userId && t.Type == "Manutenção Preventiva")
                .ToListAsync();

            // Grouping logic
            MyActiveMaintenances = GroupTickets(myTickets.Where(t => t.Status != "Concluído").ToList());
            MyCompletedMaintenances = GroupTickets(myTickets.Where(t => t.Status == "Concluído").ToList());

            // Calculate stats
            TotalMaintenances = myTickets.Count;
            PendingMaintenances = myTickets.Count(t => t.Status == "Pendente" || t.Status == "Aberto");
            InProgressMaintenances = myTickets.Count(t => t.Status == "Aceite" || t.Status == "Em reparação" || t.Status == "Em Progresso");
            CompletedMaintenances = myTickets.Count(t => t.Status == "Concluído");

            // --- Load Selected Ticket for Panel ---
            if (SelectedTicketId.HasValue)
            {
                SelectedTicket = await _context.Tickets
                    .Include(t => t.School).ThenInclude(s => s.Agrupamento)
                    .Include(t => t.RequestedBy)
                    .Include(t => t.Technician)
                    .Include(t => t.UtilizedEquipments).ThenInclude(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School)
                    .FirstOrDefaultAsync(t => t.Id == SelectedTicketId.Value);

                if (SelectedTicket != null && (SelectedTicket.TechnicianId == userId || SelectedTicket.TechnicianId == null))
                {
                    TicketHistory = await _context.TicketHistorico
                        .Where(h => h.TicketId == SelectedTicketId.Value)
                        .OrderByDescending(h => h.Data)
                        .ToListAsync();

                    AssociatedEquipment = SelectedTicket.UtilizedEquipments.ToList();

                    // Load all school/company equipment for Preventive Maintenance checklists (Resilient Hierarchy)
                    if ((SelectedTicket.Type != null && SelectedTicket.Type.Contains("Preventiva")) || SelectedTicket.Type == "Manutenção Preventiva")
                    {
                        int? targetSchoolId = SelectedTicket.SchoolId;
                        if (!targetSchoolId.HasValue && SelectedTicket.Equipamento?.Room?.Block != null)
                            targetSchoolId = SelectedTicket.Equipamento.Room.Block.SchoolId;

                        if (targetSchoolId.HasValue)
                        {
                            var schoolBlockIds = await _context.Blocos.Where(b => b.SchoolId == targetSchoolId.Value).Select(b => b.Id).ToListAsync();
                            var roomIds = await _context.Salas.Where(r => r.BlockId.HasValue && schoolBlockIds.Contains(r.BlockId.Value)).Select(r => r.Id).ToListAsync();
                            SchoolEquipment = await _context.Equipamentos.Where(e => e.RoomId.HasValue && roomIds.Contains(e.RoomId.Value) && !e.IsDeleted).OrderBy(e => e.Name).ToListAsync();
                        }
                        else if (SelectedTicket.Equipamento?.EmpresaId != null)
                        {
                            SchoolEquipment = await _context.Equipamentos.Where(e => e.EmpresaId == SelectedTicket.Equipamento.EmpresaId && !e.IsDeleted).OrderBy(e => e.Name).ToListAsync();
                        }
                    }

                    // Stock support for panel
                    var stockQuery = _context.Equipamentos
                        .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School)
                        .Where(e => e.Status == "Armazenado" && e.TicketId == null)
                        .AsQueryable();

                    if (StockScope == "Escola" && SelectedTicket.SchoolId.HasValue)
                        stockQuery = stockQuery.Where(e => e.Room.Block.SchoolId == SelectedTicket.SchoolId);

                    if (!string.IsNullOrEmpty(StockSearch))
                        stockQuery = stockQuery.Where(e => e.Name.Contains(StockSearch) || e.SerialNumber.Contains(StockSearch));

                    CountStock = await stockQuery.CountAsync();
                    AvailableEquipment = await stockQuery.Take(20).ToListAsync();
                }
            }

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

        public async Task<IActionResult> OnPostCompleteSchoolAsync(int schoolId)
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");
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
                    
                    var history = new TicketHistorico
                    {
                        TicketId = ticket.Id,
                        Acao = "Manutenção concluída via finalização de visita à escola",
                        TipoAcao = TipoAcaoHistorico.Status,
                        Autor = User.Identity?.Name ?? "Técnico",
                        Data = System.DateTime.UtcNow
                    };
                    _context.TicketHistorico.Add(history);
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Concluiu {tickets.Count} manutenções na escola!";
            }
            return RedirectToPage();
        }
        // --- Panel Actions ---

        public async Task<IActionResult> OnPostAdvanceStatusAsync(int ticketId)
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId)) return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.Include(t => t.Equipamento).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null || (ticket.TechnicianId != userId && ticket.TechnicianId != null)) return NotFound();

            string oldStatus = ticket.Status;
            string newStatus = oldStatus switch
            {
                "Pendente" or "Aberto" or "Pedido" => "Aceite",
                "Aceite" => "Em reparação",
                "Em reparação" or "Em Resolução" or "Em Progresso" or "Em andamento" => "Concluído",
                _ => oldStatus
            };

            if (newStatus != oldStatus)
            {
                ticket.Status = newStatus;
                if (newStatus == "Aceite") {
                    ticket.TechnicianId = userId;
                    ticket.AcceptedAt = System.DateTime.UtcNow;
                }
                else if (newStatus == "Concluído") {
                    ticket.CompletedAt = System.DateTime.UtcNow;
                    if (ticket.Equipamento != null) ticket.Equipamento.Status = "A funcionar";
                }

                await LogHistory(ticketId, $"Status alterado para {newStatus}", TipoAcaoHistorico.Status);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { SelectedTicketId = ticketId });
        }

        public async Task<IActionResult> OnPostAddAnomalyAsync(int ticketId, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                await LogHistory(ticketId, "[AVARIA] " + comment, TipoAcaoHistorico.Comentario);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { SelectedTicketId = ticketId });
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int ticketId, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                await LogHistory(ticketId, comment, TipoAcaoHistorico.Comentario);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { SelectedTicketId = ticketId });
        }

        public async Task<IActionResult> OnPostAssociateEquipmentAsync(int ticketId, int equipmentId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            var equipment = await _context.Equipamentos.FindAsync(equipmentId);

            if (ticket != null && equipment != null && equipment.Status == "Armazenado")
            {
                equipment.TicketId = ticketId;
                equipment.Status = "Em uso/Alocado";
                await LogHistory(ticketId, $"Equipamento associado: {equipment.Name}", TipoAcaoHistorico.Equipamento);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { SelectedTicketId = ticketId });
        }

        public async Task<IActionResult> OnPostRemoveEquipmentAsync(int ticketId, int equipmentId)
        {
            var equipment = await _context.Equipamentos.FindAsync(equipmentId);
            if (equipment != null && equipment.TicketId == ticketId)
            {
                equipment.TicketId = null;
                equipment.Status = "Armazenado";
                await LogHistory(ticketId, $"Equipamento removido: {equipment.Name}", TipoAcaoHistorico.Equipamento);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { SelectedTicketId = ticketId });
        }

        private async Task LogHistory(int ticketId, string action, TipoAcaoHistorico type)
        {
            var username = User.Identity?.Name ?? "Sistema";
            var history = new TicketHistorico
            {
                TicketId = ticketId,
                Acao = action,
                TipoAcao = type,
                Autor = username,
                Data = System.DateTime.UtcNow
            };
            _context.TicketHistorico.Add(history);
        }
    }
}
