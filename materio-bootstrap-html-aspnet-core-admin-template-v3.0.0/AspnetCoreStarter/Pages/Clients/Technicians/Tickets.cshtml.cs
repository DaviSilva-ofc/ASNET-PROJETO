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
        public List<Ticket> AvailableTickets { get; set; } = new();
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

        // --- Panel Support ---
        public Ticket? SelectedTicket { get; set; }
        public List<TicketHistorico> TicketHistory { get; set; } = new();
        public List<Equipamento> AssociatedEquipment { get; set; } = new();
        public List<Equipamento> AvailableEquipment { get; set; } = new();
        public int CountStock { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedTicketId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string StockSearch { get; set; } = "";
        [BindProperty(SupportsGet = true)]
        public string StockScope { get; set; } = "Escola";

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            // Refined filtering to include ONLY breakdown/repair tickets
            var query = _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento).ThenInclude(e => e.Empresa)
                .Include(t => t.Equipamento).ThenInclude(e => e.Room)
                .Include(t => t.UtilizedStocks)
                .Include(t => t.UtilizedEquipments)
                .Where(t => t.TechnicianId == userId && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")))
                .AsQueryable();

            // Fetch available tickets (unassigned)
            var availableQuery = _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento).ThenInclude(e => e.Empresa)
                .Include(t => t.Equipamento).ThenInclude(e => e.Room)
                .Where(t => t.TechnicianId == null && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")))
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
            AvailableTickets = await availableQuery.OrderByDescending(t => t.CreatedAt).ToListAsync();

            // Prepare Map Data for active tickets (both assigned and available)
            var mapLocations = new List<object>();
            var allActiveTickets = Tickets.Concat(AvailableTickets).Where(t => t.Status != "Concluído");

            foreach (var t in allActiveTickets)
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

                    var stockQuery = _context.Equipamentos
                        .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School)
                        .Where(e => e.Status == "Armazenado" && e.TicketId == null)
                        .AsQueryable();

                    if (StockScope == "Escola" && SelectedTicket.SchoolId.HasValue)
                        stockQuery = stockQuery.Where(e => e.Room.Block.SchoolId == SelectedTicket.SchoolId);
                    else if (StockScope == "Agrupamento" && SelectedTicket.School?.AgrupamentoId != null)
                        stockQuery = stockQuery.Where(e => e.Room.Block.School.AgrupamentoId == SelectedTicket.School.AgrupamentoId);

                    if (!string.IsNullOrEmpty(StockSearch))
                        stockQuery = stockQuery.Where(e => e.Name.Contains(StockSearch) || e.SerialNumber.Contains(StockSearch));

                    CountStock = await stockQuery.CountAsync();
                    AvailableEquipment = await stockQuery.Take(20).ToListAsync();
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptTicketAsync(int id)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.TechnicianId == null && t.Level != "Empréstimo");
            if (ticket != null)
            {
                ticket.TechnicianId = userId;
                ticket.Status = "Em Reparação";
                ticket.AcceptedAt = DateTime.UtcNow;
                await LogHistory(id, "Trabalho aceite pelo técnico", TipoAcaoHistorico.Status);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Aceitou o trabalho #{id}. O estado foi alterado para 'Em Reparação'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Ticket não encontrado ou já foi atribuído.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.Include(t => t.Equipamento).FirstOrDefaultAsync(t => t.Id == id && t.TechnicianId == userId);
            if (ticket != null)
            {
                ticket.Status = newStatus;
                if (newStatus == "Concluído")
                {
                    ticket.CompletedAt = DateTime.UtcNow;
                    if (ticket.Equipamento != null)
                    {
                        ticket.Equipamento.Status = "A funcionar";
                    }
                }
                await LogHistory(id, $"Status atualizado para {newStatus}", TipoAcaoHistorico.Status);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"O estado do trabalho #{id} foi atualizado para '{newStatus}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Ticket não encontrado ou sem permissões.";
            }

            return RedirectToPage(new { FilterStatus = FilterStatus, FilterArticle = FilterArticle, FilterType = FilterType });
        }

        // --- Panel Actions (Technician Context) ---

        public async Task<IActionResult> OnPostAdvanceStatusAsync(int ticketId)
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
                return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.Include(t => t.Equipamento).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null || (ticket.TechnicianId != userId && ticket.TechnicianId != null)) return NotFound();

            string oldStatus = ticket.Status;
            string newStatus = oldStatus switch
            {
                "Aberto" or "Pedido" or "Pendente" => "Aceite",
                "Aceite" => "Em Reparação",
                // "Em Reparação" will now be handled by specific Complete methods for better granularity
                _ => oldStatus
            };

            if (newStatus != oldStatus)
            {
                ticket.Status = newStatus;
                if (newStatus == "Aceite") {
                    ticket.TechnicianId = userId;
                    ticket.AcceptedAt = DateTime.UtcNow;
                }

                await LogHistory(ticketId, $"Status alterado para {newStatus}", TipoAcaoHistorico.Status);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { SelectedTicketId = ticketId });
        }

        public async Task<IActionResult> OnPostCompleteRepairedAsync(int ticketId)
        {
            var ticket = await _context.Tickets.Include(t => t.Equipamento).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = "Concluído";
                ticket.CompletedAt = DateTime.UtcNow;
                
                if (ticket.Equipamento != null)
                {
                    ticket.Equipamento.Status = "A funcionar";
                }

                await LogHistory(ticketId, "Ticket Concluído: Equipamento Reparado", TipoAcaoHistorico.Status);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ticket concluído com sucesso. Equipamento marcado como 'A funcionar'.";
            }
            return RedirectToPage(new { SelectedTicketId = ticketId });
        }

        public async Task<IActionResult> OnPostCompleteUnrepairableAsync(int ticketId)
        {
            var ticket = await _context.Tickets.Include(t => t.Equipamento).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = "Concluído";
                ticket.CompletedAt = DateTime.UtcNow;
                
                if (ticket.Equipamento != null)
                {
                    ticket.Equipamento.Status = "Sem reparação";
                }

                await LogHistory(ticketId, "Ticket Concluído: Equipamento Sem Reparação", TipoAcaoHistorico.Status);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ticket concluído. Equipamento marcado como 'Sem reparação'.";
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

        public async Task<IActionResult> OnPostAddCommentAsync(int ticketId, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                await LogHistory(ticketId, comment, TipoAcaoHistorico.Comentario);
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
                Data = DateTime.UtcNow
            };
            _context.TicketHistorico.Add(history);
        }
    }
}
