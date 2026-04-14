using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using AspnetCoreStarter.Services;
using System;

namespace AspnetCoreStarter.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IStockService _stockService;

        public DashboardModel(AppDbContext context, IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public List<AspnetCoreStarter.Models.User> PendingUsers { get; set; }
        public int TotalUsers { get; set; }
        public int ExpiringContractsCount { get; set; }
        public int PendingTicketsCount { get; set; }
        public int LowStockAlertsCount { get; set; }
        public int TotalUnreadMessages { get; set; }
        public List<AspnetCoreStarter.Models.User> RecentMessageSenders { get; set; }
        public List<LowStockItemViewModel> LowStockItems { get; set; } = new();
        public List<PedidoStock> EscalatedRequests { get; set; } = new();
        public List<Ticket> TechnicianStockRequests { get; set; } = new();
        public List<Ticket> RecentMaintenanceTickets { get; set; } = new();
        public List<StockEmpresa> GlobalStock { get; set; } = new();

        public int TicketsPendenteCount { get; set; }
        public int TicketsEmResolucaoCount { get; set; }
        public int TicketsConcluidoCount { get; set; }

        public List<string> MonthlyLabels { get; set; } = new();
        public List<int> MonthlyPendentesData { get; set; } = new();
        public List<int> MonthlyEmResolucaoData { get; set; } = new();
        public List<int> MonthlyConcluidosData { get; set; } = new();

        public string ClientLocationsJson { get; set; }

        // Infrastructure tree data
        public List<Agrupamento> Agrupamentos { get; set; }
        public List<AspnetCoreStarter.Models.School> AllSchools { get; set; }
        public List<Bloco> AllBlocos { get; set; }
        public List<Sala> AllSalas { get; set; }
        public List<Empresa> Empresas { get; set; }

        // Chart data — per-school equipment counts
        public List<string> SchoolNames { get; set; } = new();
        public List<int> SchoolEquipmentCounts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
                return RedirectToPage("/Index");

            // Totals
            TotalUsers = await _context.Users.CountAsync();
            
            // 1. Contratos a expirar (dentro de 30 dias)
            var oneMonthFromNow = DateTime.Now.AddDays(30);
            var today = DateTime.Now;
            ExpiringContractsCount = await _context.Contratos
                .Where(c => c.ExpiryDate != null && c.ExpiryDate >= today && c.ExpiryDate <= oneMonthFromNow)
                .CountAsync(); 

            // 2. Tickets pendentes
            PendingTicketsCount = await _context.Tickets
                .Where(t => t.Status == "Pendente" || (t.Level == "Empréstimo" && t.Status == "Pedido"))
                .CountAsync();

            // 4. Chat Notifications
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
            Empresas = await _context.Empresas.ToListAsync();

            // Per-school equipment counts
            SchoolNames = new List<string>();
            SchoolEquipmentCounts = new List<int>();

            foreach (var school in AllSchools)
            {
                SchoolNames.Add(school.Name ?? "Sem Nome");
                var blocoIds = AllBlocos
                    .Where(b => b.SchoolId == school.Id)
                    .Select(b => b.Id)
                    .ToList();
                var salaIds = AllSalas
                    .Where(s => s.BlockId.HasValue && blocoIds.Contains(s.BlockId.Value))
                    .Select(s => s.Id)
                    .ToList();
                var equipCount = await _context.Equipamentos
                    .Where(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value))
                    .CountAsync();
                SchoolEquipmentCounts.Add(equipCount);
            }

            // Ticket Status Chart Data
            TicketsPendenteCount = await _context.Tickets.CountAsync(t => t.Status == "Pendente");
            TicketsEmResolucaoCount = await _context.Tickets.CountAsync(t => t.Status == "Em Resolução");
            TicketsConcluidoCount = await _context.Tickets.CountAsync(t => t.Status == "Concluído");

            // Monthly Trend Data (Last 6 Months)
            MonthlyLabels = new List<string>();
            MonthlyPendentesData = new List<int>();
            MonthlyEmResolucaoData = new List<int>();
            MonthlyConcluidosData = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.Now.AddMonths(-i);
                var monthLabel = monthDate.ToString("MMM", new System.Globalization.CultureInfo("pt-PT"));
                MonthlyLabels.Add(monthLabel);

                var startOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                MonthlyPendentesData.Add(await _context.Tickets.CountAsync(t => t.CreatedAt >= startOfMonth && t.CreatedAt <= endOfMonth && t.Status == "Pendente"));
                MonthlyEmResolucaoData.Add(await _context.Tickets.CountAsync(t => t.CreatedAt >= startOfMonth && t.CreatedAt <= endOfMonth && t.Status == "Em Resolução"));
                MonthlyConcluidosData.Add(await _context.Tickets.CountAsync(t => t.CreatedAt >= startOfMonth && t.CreatedAt <= endOfMonth && t.Status == "Concluído"));
            }

            // Client Locations for Map
            var locations = await _context.Schools
                .Where(s => !string.IsNullOrEmpty(s.Address))
                .Select(s => new { name = s.Name, address = s.Address })
                .ToListAsync();
            ClientLocationsJson = System.Text.Json.JsonSerializer.Serialize(locations);

            // 5. Low Stock Alerts Logic
            var allAvailableStock = await _context.StockEmpresa
                .Where(s => s.IsAvailable || s.Status == "Disponível")
                .ToListAsync();

            var groupedStock = allAvailableStock
                .GroupBy(s => new { Name = s.EquipmentName ?? "Sem Nome", Type = s.Type })
                .Select(g => new LowStockItemViewModel
                {
                    Name = g.Key.Name,
                    AvailableCount = g.Count(),
                    Type = g.Key.Type
                })
                .ToList();

            LowStockItems = groupedStock
                .Where(x => _stockService.IsLowStock(x.Name, x.Type, x.AvailableCount))
                .ToList();

            LowStockAlertsCount = LowStockItems.Count;

            // Pending users
            PendingUsers = await _context.Users
                .Where(u => u.AccountStatus == "Pendente")
                .ToListAsync();

            // Load escalated requests (only those pending admin action — fulfilled ones are removed)
            EscalatedRequests = await _context.PedidosStock
                .Include(p => p.School)
                .Include(p => p.Agrupamento)
                .Include(p => p.RequestedBy)
                .Where(p => p.Status == "Pendente_Admin")
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();

            // Load technician stock requests sent as Tickets (Level = "Empréstimo")
            TechnicianStockRequests = await _context.Tickets
                .Include(t => t.Technician)
                .Where(t => t.Level == "Empréstimo" && t.Status == "Pedido")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Recent General Maintenance Tickets (excluding loans)
            RecentMaintenanceTickets = await _context.Tickets
                .Include(t => t.Technician)
                .Include(t => t.Equipamento)
                .Include(t => t.School)
                .Where(t => t.Level != "Empréstimo")
                .OrderByDescending(t => t.CreatedAt)
                .Take(7)
                .ToListAsync();

            // Load global stock (available items with no school assigned)
            GlobalStock = await _context.StockEmpresa
                .Where(s => s.SchoolId == null && (s.IsAvailable || s.Status == "Armazenado" || s.Status == "Disponível"))
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostFulfillEscalatedRequestAsync(int requestId, int[] selectedStockIds, string? adminNotes)
        {
            var request = await _context.PedidosStock.FindAsync(requestId);
            if (request == null) return RedirectToPage();

            if (selectedStockIds != null && selectedStockIds.Length > 0)
            {
                var items = await _context.StockEmpresa
                    .Where(s => selectedStockIds.Contains(s.Id))
                    .ToListAsync();

                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int? currentAdminId = int.TryParse(userIdStr, out int id) ? id : null;

                foreach (var item in items)
                {
                    item.AgrupamentoId = request.AgrupamentoId;
                    item.SchoolId = request.SchoolId;
                    item.Status = "Emprestado";
                    item.IsAvailable = false;
                    item.AdminId = currentAdminId; // Mark as Admin loan
                }
            }

            // Remove the request entirely — when returned, it was already deleted from the list
            _context.PedidosStock.Remove(request);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Emprestado com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectEscalatedRequestAsync(int requestId, string? adminNotes)
        {
            var request = await _context.PedidosStock.FindAsync(requestId);
            if (request == null) return RedirectToPage();

            request.Status = "Recusado";
            request.DirectorNotes = adminNotes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pedido recusado pelo Administrador.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostProcessApprovalAsync(int id, string role, int? agrupamentoId, string[]? areaTecnica, string? areaTecnicaOutros, string? nivel, int? escolaId, int? blocoId, int? empresaId)
        {
            if (string.IsNullOrEmpty(role))
            {
                ModelState.AddModelError("", "O cargo é obrigatório para aprovação.");
                return RedirectToPage();
            }

            var userFound = await _context.Users.FindAsync(id);
            if (userFound != null)
            {
                bool roleCreated = false;
                switch (role)
                {
                    case "Diretor":
                        if (!agrupamentoId.HasValue) break;
                        var diretor = new Diretor { UserId = id, AgrupamentoId = agrupamentoId };
                        _context.Diretores.Add(diretor);
                        roleCreated = true;
                        break;
                    case "Tecnico":
                        if (string.IsNullOrEmpty(nivel)) break;
                        var areas = new List<string>();
                        if (areaTecnica != null && areaTecnica.Length > 0)
                            areas.AddRange(areaTecnica);
                        if (!string.IsNullOrWhiteSpace(areaTecnicaOutros))
                            areas.Add(areaTecnicaOutros.Trim());
                        
                        string finalArea = string.Join(", ", areas);
                        if (finalArea.Length > 100) finalArea = finalArea.Substring(0, 97) + "...";

                        var tecnico = new Tecnico { UserId = id, AreaTecnica = finalArea, Nivel = nivel };
                        _context.Tecnicos.Add(tecnico);
                        roleCreated = true;
                        break;
                    case "Coordenador":
                        if (!escolaId.HasValue) break;
                        var coordenador = new Coordenador { UserId = id, SchoolId = escolaId };
                        _context.Coordenadores.Add(coordenador);
                        roleCreated = true;
                        break;
                    case "Professor":
                        if (!blocoId.HasValue) break;
                        var professor = new Professor { UserId = id, BlocoId = blocoId };
                        _context.Professores.Add(professor);
                        roleCreated = true;
                        break;
                    case "Cliente Individual":
                        if (!empresaId.HasValue) break;
                        userFound.EmpresaId = empresaId;
                        _context.Users.Update(userFound);
                        roleCreated = true;
                        break;
                }

                if (roleCreated)
                {
                    userFound.AccountStatus = "Ativo";
                    await _context.SaveChangesAsync();
                }
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

    public class LowStockItemViewModel
    {
        public string Name { get; set; }
        public int AvailableCount { get; set; }
        public string? Type { get; set; }
    }
}
