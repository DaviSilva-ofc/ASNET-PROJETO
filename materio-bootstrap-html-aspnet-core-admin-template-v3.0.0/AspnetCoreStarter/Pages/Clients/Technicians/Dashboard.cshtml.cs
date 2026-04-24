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
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public int MyTicketsCount { get; set; }
        public int PendingTicketsCount { get; set; }
        public int AvailableTicketsCount { get; set; }
        public int MyStockAlertsCount { get; set; }
        public int UnreadMessagesCount { get; set; }
        public int GlobalAvailableStockCount { get; set; }

        public List<Ticket> RecentTickets { get; set; } = new();
        public List<StockEmpresa> MyStock { get; set; } = new();
        public List<Ticket> ActiveTickets { get; set; } = new();

        public List<School> LocaisAtendimento { get; set; } = new();
        public List<AspnetCoreStarter.Models.User> RecentMessageSenders { get; set; } = new();

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
        
        public List<string> LineChartLabels { get; set; } = new();
        public (List<int> Pendente, List<int> EmResolucao, List<int> Concluido) LineChartMonthlyData { get; set; } 
            = (new List<int>(), new List<int>(), new List<int>());

        public List<string> BarChartLabels { get; set; } = new() { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };
        public List<int> BarChartData { get; set; } = new();

        public string? ClientLocationsJson { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico"))
            {
                return RedirectToPage("/Auth/Login");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Counts
            // Refined filtering to include ONLY breakdown/repair tickets
            MyTicketsCount = await _context.Tickets.CountAsync(t => t.TechnicianId == userId && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")));
            PendingTicketsCount = await _context.Tickets.CountAsync(t => t.TechnicianId == userId && (t.Status == "Pendente" || t.Status == "Em Resolução") && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")));
            AvailableTicketsCount = await _context.Tickets.CountAsync(t => t.TechnicianId == null && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")));
            
            MyStockAlertsCount = await _context.StockTecnico
                .Where(s => s.TechnicianId == userId && !s.IsAvailable)
                .CountAsync();

            UnreadMessagesCount = await _context.Mensagens
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            GlobalAvailableStockCount = await _context.StockEmpresa
                .CountAsync(s => s.Status == "Armazenado" || s.Status == "Disponível");

            // Fetch Recent Tickets (Assigned Repairs)
            RecentTickets = await _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento)
                .Where(t => t.TechnicianId == userId && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")))
                .OrderByDescending(t => t.CreatedAt)
                .Take(3)
                .ToListAsync();

            // Fetch My Stock (Directly assigned in StockEmpresa)
            MyStock = await _context.StockEmpresa
                .Where(s => s.TechnicianId == userId && s.AgrupamentoId == null && s.SchoolId == null)
                .OrderByDescending(s => s.IsAvailable)
                .ToListAsync();

            // Fetch Active Tickets for association modal
            ActiveTickets = await _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento)
                .Where(t => t.TechnicianId == userId && t.Status != "Concluído" && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")))
                .ToListAsync();

            // Fetch all schools with addresses for the map
            LocaisAtendimento = await _context.Schools
                .Where(s => !string.IsNullOrEmpty(s.Address))
                .ToListAsync();

            // Chat Senders
            var unreadMessages = await _context.Mensagens
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();
            RecentMessageSenders = unreadMessages.Select(m => m.Sender).GroupBy(s => s.Id).Select(g => g.First()).Take(5).ToList();

            // Line Chart Analytics (Últimos 6 meses contínuos)
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                LineChartLabels.Add(date.ToString("MMM"));

                var ticketsThisMonth = await _context.Tickets
                    .Where(t => t.TechnicianId == userId && t.CreatedAt.Month == date.Month && t.CreatedAt.Year == date.Year && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")))
                    .ToListAsync();

                LineChartMonthlyData.Pendente.Add(ticketsThisMonth.Count(t => t.Status == "Pendente"));
                LineChartMonthlyData.EmResolucao.Add(ticketsThisMonth.Count(t => t.Status == "Em Resolução"));
                LineChartMonthlyData.Concluido.Add(ticketsThisMonth.Count(t => t.Status == "Concluído"));
            }

            // Bar Chart Analytics (Visão Anual - Janeiro a Dezembro do ano atual)
            int currentYear = DateTime.Now.Year;
            var concludedTickets = await _context.Tickets
                .Where(t => t.TechnicianId == userId && t.Status == "Concluído" && t.CreatedAt.Year == currentYear && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")))
                .ToListAsync();

            for (int month = 1; month <= 12; month++)
            {
                BarChartData.Add(concludedTickets.Count(t => t.CreatedAt.Month == month));
            }

            // Map Locations with Ticket Status
            var schoolsWithTickets = await _context.Schools
                .Where(s => !s.IsDeleted)
                .ToListAsync();
            var mapLocations = schoolsWithTickets.Select(s => {
                var schoolTickets = _context.Tickets.Where(t => t.SchoolId == s.Id).ToList();
                return new { 
                    name = s.Name, 
                    address = string.IsNullOrEmpty(s.Address) ? s.Name : s.Address,
                    hasInProgress = schoolTickets.Any(t => {
                        var st = t.Status.ToLower();
                        return st.Contains("reparação") || st.Contains("resolução") || st.Contains("andamento") || st.Contains("aceite") || st.Contains("progresso");
                    }),
                    hasCompleted = schoolTickets.Any(t => t.Status.ToLower().Contains("concluído")),
                    hasPending = schoolTickets.Any(t => {
                        var st = t.Status.ToLower();
                        return st.Contains("pendente") || st.Contains("pedido") || st.Contains("aberto");
                    })
                };
            }).ToList();
            ClientLocationsJson = System.Text.Json.JsonSerializer.Serialize(mapLocations);

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

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string status)
        {
            var equipment = await _context.Equipamentos.FindAsync(id);
            if (equipment == null) return NotFound();

            equipment.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Estado do equipamento atualizado com sucesso.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignStockAsync(int stockId, int ticketId, string itemType = "Stock")
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null || ticket.TechnicianId != userId)
            {
                TempData["ErrorMessage"] = "Trabalho não encontrado ou sem permissão.";
                return RedirectToPage();
            }

            var stock = await _context.StockEmpresa.FindAsync(stockId);
            if (stock == null)
            {
                TempData["ErrorMessage"] = "Item de stock não encontrado.";
                return RedirectToPage();
            }

            stock.TicketId = ticketId;
            stock.Status = "Usado na Intervenção";
            stock.IsAvailable = false;
            
            await LogHistory(ticketId, $"Artigo de Stock pessoal associado: {stock.EquipmentName}", TipoAcaoHistorico.Equipamento);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"O artigo '{stock.EquipmentName}' foi associado com sucesso ao Trabalho #{ticketId}.";

            return RedirectToPage();
        }

        // --- Panel Actions ---

        public async Task<IActionResult> OnPostAdvanceStatusAsync(int ticketId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.Include(t => t.Equipamento).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null || (ticket.TechnicianId != userId && ticket.TechnicianId != null)) return NotFound();

            string oldStatus = ticket.Status;
            string newStatus = oldStatus switch
            {
                "Aberto" or "Pedido" or "Pendente" => "Aceite",
                "Aceite" => "Em Reparação",
                "Em Reparação" or "Em andamento" => "Concluído",
                _ => oldStatus
            };

            if (newStatus != oldStatus)
            {
                ticket.Status = newStatus;
                if (newStatus == "Aceite") {
                    ticket.TechnicianId = userId;
                    ticket.AcceptedAt = DateTime.UtcNow;
                }
                else if (newStatus == "Concluído") {
                    ticket.CompletedAt = DateTime.UtcNow;
                    if (ticket.Equipamento != null) {
                        ticket.Equipamento.Status = "A funcionar";
                    }
                }

                await LogHistory(ticketId, $"Status alterado para {newStatus}", TipoAcaoHistorico.Status);
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
