using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public List<AspnetCoreStarter.Models.User> PendingUsers { get; set; }
        public int TotalUsers { get; set; }
        public int ExpiringContractsCount { get; set; }
        public int PendingTicketsCount { get; set; }
        public int LowStockAlertsCount { get; set; }
        public int TotalUnreadMessages { get; set; }
        public List<AspnetCoreStarter.Models.User> RecentMessageSenders { get; set; }

        public int TicketsPedidoCount { get; set; }
        public int TicketsConcluidoCount { get; set; }
        public int TicketsPendenteCount { get; set; }

        public List<int> LineChartPedidosData { get; set; }
        public List<int> LineChartPendentesData { get; set; }
        public List<int> LineChartConcluidosData { get; set; }
        public List<string> LineChartLabels { get; set; }

        public string ClientLocationsJson { get; set; }

        // Infrastructure tree data
        public List<Agrupamento> Agrupamentos { get; set; }
        public List<AspnetCoreStarter.Models.School> AllSchools { get; set; }
        public List<Bloco> AllBlocos { get; set; }
        public List<Sala> AllSalas { get; set; }

        // Chart data — per-school equipment counts
        public List<string> SchoolNames { get; set; }
        public List<int> SchoolEquipmentCounts { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
                return RedirectToPage("/Index");

            // Pending users
            PendingUsers = await _context.Users
                .Where(u => u.AccountStatus == "Pendente")
                .ToListAsync();

            // Totals
            TotalUsers = await _context.Users.CountAsync();
            
            // New Metrics Logic
            // 1. Contratos a expirar (using Periodo string - temporary logic: count all for now)
            // Note: In a real scenario, we'd parse the 'Periodo' string to check expiration.
            ExpiringContractsCount = await _context.Contratos.CountAsync(); 

            // 2. Tickets pendentes (Tickets with status Pendente or no technician assigned)
            PendingTicketsCount = await _context.Tickets
                .Where(t => t.Status == "Pendente" || t.TechnicianId == null)
                .CountAsync();

            LowStockAlertsCount = await _context.StockEmpresa
                .Where(s => !s.IsAvailable)
                .CountAsync();

            // 4. Chat Notifications (Unread messages for the current admin)
            int currentUserId = int.Parse(userId);
            var unreadMessages = await _context.Mensagens
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            TotalUnreadMessages = unreadMessages.Count;
            RecentMessageSenders = unreadMessages
                .Select(m => m.Sender)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .Take(5)
                .ToList();

            // Infrastructure tree data
            Agrupamentos = await _context.Agrupamentos.ToListAsync();
            AllSchools = await _context.Schools.Include(s => s.Agrupamento).ToListAsync();
            AllBlocos = await _context.Blocos.Include(b => b.School).ToListAsync();
            AllSalas = await _context.Salas.Include(s => s.Block).ToListAsync();

            // Per-school equipment counts for bar chart (keeping logic intact if needed elsewhere, though chart is removed)
            SchoolNames = new List<string>();
            SchoolEquipmentCounts = new List<int>();

            var schools = await _context.Schools.ToListAsync();
            foreach (var school in schools)
            {
                SchoolNames.Add(school.Name ?? "Sem Nome");
                var blocoIds = await _context.Blocos
                    .Where(b => b.SchoolId == school.Id)
                    .Select(b => b.Id)
                    .ToListAsync();
                var salaIds = await _context.Salas
                    .Where(s => blocoIds.Contains(s.BlockId))
                    .Select(s => s.Id)
                    .ToListAsync();
                var equipCount = await _context.Equipamentos
                    .Where(e => salaIds.Contains(e.RoomId))
                    .CountAsync();
                SchoolEquipmentCounts.Add(equipCount);
            }

            // Ticket Status Chart Data
            TicketsPedidoCount = await _context.Tickets.CountAsync(t => t.Status == "Pedido");
            TicketsConcluidoCount = await _context.Tickets.CountAsync(t => t.Status == "Concluido");
            TicketsPendenteCount = await _context.Tickets.CountAsync(t => t.Status == "Pendente");

            // Line Chart Data grouped by Level
            var allTickets = await _context.Tickets.ToListAsync();
            LineChartLabels = new List<string> { "Baixo", "Medio", "Alto" };
            LineChartPedidosData = new List<int>();
            LineChartPendentesData = new List<int>();
            LineChartConcluidosData = new List<int>();

            foreach (var label in LineChartLabels)
            {
                LineChartPedidosData.Add(allTickets.Count(t => t.Level == label && t.Status == "Pedido"));
                LineChartPendentesData.Add(allTickets.Count(t => t.Level == label && t.Status == "Pendente"));
                LineChartConcluidosData.Add(allTickets.Count(t => t.Level == label && t.Status == "Concluido"));
            }

            // Client Locations for Map
            var locations = await _context.Schools
                .Where(s => !string.IsNullOrEmpty(s.Address))
                .Select(s => new { name = s.Name, address = s.Address })
                .ToListAsync();
            ClientLocationsJson = System.Text.Json.JsonSerializer.Serialize(locations);

            return Page();
        }

        public async Task<IActionResult> OnPostProcessApprovalAsync(int id, string role, int? agrupamentoId, string[]? areaTecnica, string? areaTecnicaOutros, string? nivel, int? escolaId, int? blocoId)
        {
            var userFound = await _context.Users.FindAsync(id);
            if (userFound != null)
            {
                userFound.AccountStatus = "Ativo";

                // Create role-specific record based on the selected role
                switch (role)
                {
                    case "Diretor":
                        var diretor = new Diretor { UserId = id, AgrupamentoId = agrupamentoId };
                        _context.Diretores.Add(diretor);
                        break;
                    case "Tecnico":
                        // Consolidate multiple technical areas
                        var areas = new List<string>();
                        if (areaTecnica != null && areaTecnica.Length > 0)
                        {
                            areas.AddRange(areaTecnica);
                        }
                        if (!string.IsNullOrWhiteSpace(areaTecnicaOutros))
                        {
                            areas.Add(areaTecnicaOutros.Trim());
                        }
                        
                        string finalArea = string.Join(", ", areas);
                        // Limit to schema VARCHAR length if necessary (optional safeguard)
                        if (finalArea.Length > 100) finalArea = finalArea.Substring(0, 97) + "...";

                        var tecnico = new Tecnico { UserId = id, AreaTecnica = finalArea, Nivel = nivel };
                        _context.Tecnicos.Add(tecnico);
                        break;
                    case "Coordenador":
                        var coordenador = new Coordenador { UserId = id, SchoolId = escolaId };
                        _context.Coordenadores.Add(coordenador);
                        break;
                    case "Professor":
                        var professor = new Professor { UserId = id, BlocoId = blocoId };
                        _context.Professores.Add(professor);
                        break;
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var userFound = await _context.Users.FindAsync(id);
            if (userFound != null)
            {
                userFound.AccountStatus = "Rejeitado";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
