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

        // --- View Data ---
        public List<Ticket> Tickets { get; set; } = new();
        public Ticket? SelectedTicket { get; set; }
        public List<TicketHistorico> TicketHistory { get; set; } = new();
        public List<Equipamento> AssociatedEquipment { get; set; } = new();
        public List<Equipamento> AvailableEquipment { get; set; } = new();
        public List<User> AvailableTechnicians { get; set; } = new();
        public int CountStock { get; set; }

        // --- Stats ---
        public int CountPendente { get; set; }
        public int CountAceite { get; set; }
        public int CountEmReparacao { get; set; }
        public int CountConcluido { get; set; }

        // --- Filters ---
        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterPriority { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? SelectedTicketId { get; set; }

        // --- Stock Filters (Ajax/Partial) ---
        [BindProperty(SupportsGet = true)]
        public string StockSearch { get; set; } = "";
        [BindProperty(SupportsGet = true)]
        public string StockScope { get; set; } = "Escola"; // Escola, Agrupamento, Todos

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            // Base Query
            var query = _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento)
                .Include(t => t.Technician)
                .Include(t => t.RequestedBy)
                .AsQueryable();

            // Calculate Stats (Unfiltered)
            CountPendente = await _context.Tickets.CountAsync(t => t.Status == "Pendente" || t.Status == "Aberto" || t.Status == "Pedido");
            CountAceite = await _context.Tickets.CountAsync(t => t.Status == "Aceite");
            CountEmReparacao = await _context.Tickets.CountAsync(t => t.Status == "Em reparação" || t.Status == "Em Progresso" || t.Status == "Em Reparação" || t.Status == "Em andamento");
            CountConcluido = await _context.Tickets.CountAsync(t => t.Status == "Concluído");

            // Apply Filters
            if (!string.IsNullOrEmpty(FilterStatus))
            {
                if (FilterStatus == "Aberto") query = query.Where(t => t.Status == "Aberto" || t.Status == "Pedido" || t.Status == "Pendente");
                else if (FilterStatus == "Em Progresso") query = query.Where(t => t.Status == "Em Progresso" || t.Status == "Em Reparação" || t.Status == "Em andamento");
                else query = query.Where(t => t.Status == FilterStatus);
            }

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                query = query.Where(t => t.Description.Contains(SearchQuery) || t.Id.ToString() == SearchQuery);
            }

            if (!string.IsNullOrEmpty(FilterType))
            {
                query = query.Where(t => t.Type == FilterType);
            }

            if (!string.IsNullOrEmpty(FilterPriority))
            {
                query = query.Where(t => t.Priority == FilterPriority);
            }

            Tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            AvailableTechnicians = await _context.Users
                .Join(_context.Tecnicos, u => u.Id, t => t.UserId, (u, t) => u)
                .ToListAsync();

            // If a ticket is selected, load its details
            if (SelectedTicketId.HasValue)
            {
                SelectedTicket = await _context.Tickets
                    .Include(t => t.School).ThenInclude(s => s.Agrupamento)
                    .Include(t => t.RequestedBy)
                    .Include(t => t.Technician)
                    .Include(t => t.Equipamento)
                    .Include(t => t.UtilizedEquipments)
                    .FirstOrDefaultAsync(t => t.Id == SelectedTicketId.Value);

                if (SelectedTicket != null)
                {
                    TicketHistory = await _context.TicketHistorico
                        .Where(h => h.TicketId == SelectedTicketId.Value)
                        .OrderByDescending(h => h.Data)
                        .ToListAsync();

                    AssociatedEquipment = SelectedTicket.UtilizedEquipments.ToList();

                    // Load Available Stock
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

        // --- Actions ---

        public async Task<IActionResult> OnPostAdvanceStatusAsync(int ticketId)
        {
            var ticket = await _context.Tickets.Include(t => t.Equipamento).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();
            if (ticket.TechnicianId != null && ticket.Status != "Pendente" && ticket.Status != "Aberto")
            {
                TempData["ErrorMessage"] = "Este ticket já foi aceite por um técnico e não pode ser alterado pelo Administrador.";
                return RedirectToPage(new { SelectedTicketId = ticketId });
            }

            string oldStatus = ticket.Status;
            string newStatus = oldStatus switch
            {
                "Pendente" or "Aberto" or "Pedido" => "Aceite",
                "Aceite" => "Em reparação",
                "Em reparação" or "Em Progresso" or "Em Reparação" or "Em andamento" => "Concluído",
                _ => oldStatus
            };

            if (newStatus != oldStatus)
            {
                ticket.Status = newStatus;
                
                // Assign current technician/admin if accepting
                if (newStatus == "Aceite")
                {
                    var sessionUserId = HttpContext.Session.GetString("UserId");
                    if (int.TryParse(sessionUserId, out int uId)) {
                        // Check if it's a technician (required for id_tecnico FK)
                        var isTech = await _context.Tecnicos.AnyAsync(te => te.UserId == uId);
                        if (isTech) ticket.TechnicianId = uId;
                        else {
                            // Check if it's an admin
                            var isAdmin = await _context.Administradores.AnyAsync(ad => ad.UserId == uId);
                            if (isAdmin) ticket.AdminId = uId;
                        }
                    }
                    ticket.AcceptedAt = DateTime.UtcNow;
                }
                else if (newStatus == "Concluído")
                {
                    ticket.CompletedAt = DateTime.UtcNow;
                    if (ticket.Equipamento != null)
                    {
                        ticket.Equipamento.Status = "A funcionar";
                    }
                }

                await LogHistory(ticketId, $"Status alterado de {oldStatus} para {newStatus}", TipoAcaoHistorico.Status);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { SelectedTicketId = ticketId, FilterStatus, SearchQuery, FilterType, FilterPriority });
        }

        public async Task<IActionResult> OnPostAssignTechnicianAsync(int ticketId, int technicianId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            if (ticket.TechnicianId != null && ticket.Status != "Pendente" && ticket.Status != "Aberto")
            {
                TempData["ErrorMessage"] = "Este ticket já foi aceite por um técnico e não pode ser reatribuído.";
                return RedirectToPage(new { SelectedTicketId = ticketId, FilterStatus, SearchQuery, FilterType, FilterPriority });
            }

            if (ticket.Status == "Aberto" || ticket.Status == "Pedido" || ticket.Status == "Pendente")
            {
                ticket.Status = "Aceite";
                ticket.AcceptedAt = DateTime.UtcNow;
            }
            
            ticket.TechnicianId = technicianId;

            var techUser = await _context.Users.FindAsync(technicianId);
            await LogHistory(ticketId, $"Ticket atribuído ao técnico {techUser?.Username}", TipoAcaoHistorico.Status);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { SelectedTicketId = ticketId, FilterStatus, SearchQuery, FilterType, FilterPriority });
        }

        public async Task<IActionResult> OnPostAssociateEquipmentAsync(int ticketId, int equipmentId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            var equipment = await _context.Equipamentos.FindAsync(equipmentId);

            if (ticket != null && equipment != null && equipment.Status == "Armazenado")
            {
                if (ticket.TechnicianId != null && ticket.Status != "Pendente" && ticket.Status != "Aberto")
                {
                    TempData["ErrorMessage"] = "Não é possível associar equipamentos a um ticket já aceite por um técnico.";
                    return RedirectToPage(new { SelectedTicketId = ticketId });
                }
                equipment.TicketId = ticketId;
                equipment.Status = "Em uso/Alocado";
                
                await LogHistory(ticketId, $"Equipamento associado: {equipment.Name} (S/N: {equipment.SerialNumber})", TipoAcaoHistorico.Equipamento);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { SelectedTicketId = ticketId, FilterStatus, SearchQuery, FilterType, FilterPriority });
        }

        public async Task<IActionResult> OnPostRemoveEquipmentAsync(int ticketId, int equipmentId)
        {
            var equipment = await _context.Equipamentos.FindAsync(equipmentId);

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (equipment != null && equipment.TicketId == ticketId)
            {
                if (ticket != null && ticket.TechnicianId != null && ticket.Status != "Pendente" && ticket.Status != "Aberto")
                {
                    TempData["ErrorMessage"] = "Não é possível remover equipamentos de um ticket já aceite por um técnico.";
                    return RedirectToPage(new { SelectedTicketId = ticketId });
                }
                equipment.TicketId = null;
                equipment.Status = "Armazenado";

                await LogHistory(ticketId, $"Equipamento removido: {equipment.Name} (S/N: {equipment.SerialNumber})", TipoAcaoHistorico.Equipamento);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { SelectedTicketId = ticketId, FilterStatus, SearchQuery, FilterType, FilterPriority });
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int ticketId, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                await LogHistory(ticketId, comment, TipoAcaoHistorico.Comentario);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { SelectedTicketId = ticketId, FilterStatus, SearchQuery, FilterType, FilterPriority });
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
