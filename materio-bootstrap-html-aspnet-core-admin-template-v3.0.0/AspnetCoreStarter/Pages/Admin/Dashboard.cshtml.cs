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
        public List<Empresa> Empresas { get; set; }

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

            // Temporary fix for missing tables and columns in MySQL
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS contratos (
                        id_contrato INT AUTO_INCREMENT PRIMARY KEY,
                        periodo VARCHAR(50),
                        tipo_contrato VARCHAR(50),
                        status_contrato VARCHAR(50),
                        descricao TEXT,
                        id_agrupamento INT,
                        id_admin INT,
                        FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento),
                        FOREIGN KEY (id_admin) REFERENCES utilizadores(id_utilizador)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS diretores (
                        id_diretores INT AUTO_INCREMENT PRIMARY KEY,
                        id_utilizador INT,
                        id_agrupamento INT,
                        FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador),
                        FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS coordenadores (
                        id_coordenadores INT AUTO_INCREMENT PRIMARY KEY,
                        id_utilizador INT,
                        id_escola INT,
                        FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador),
                        FOREIGN KEY (id_escola) REFERENCES escolas(id_escola)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS professores (
                        id_professores INT AUTO_INCREMENT PRIMARY KEY,
                        id_utilizador INT,
                        id_bloco INT,
                        FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador),
                        FOREIGN KEY (id_bloco) REFERENCES blocos(id_bloco)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS tecnicos (
                        id_utilizador INT PRIMARY KEY,
                        especialidade VARCHAR(100),
                        FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS administradores (
                        id_utilizador INT PRIMARY KEY,
                        id_agrupamento INT,
                        FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador),
                        FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS stock_empresa (
                        id_stock INT AUTO_INCREMENT PRIMARY KEY,
                        nome_equipamento VARCHAR(100),
                        tipo VARCHAR(100),
                        descricao TEXT,
                        disponivel BOOLEAN DEFAULT TRUE,
                        id_tecnico INT,
                        id_admin INT,
                        FOREIGN KEY (id_admin) REFERENCES utilizadores(id_utilizador)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS stock_tecnico (
                        id_stock_tecnico INT AUTO_INCREMENT PRIMARY KEY,
                        id_tecnico INT,
                        nome_equipamento VARCHAR(100),
                        descricao TEXT,
                        disponivel BOOLEAN DEFAULT TRUE,
                        FOREIGN KEY (id_tecnico) REFERENCES tecnicos(id_utilizador)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { 
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS mensagens (
                        id_mensagem INT AUTO_INCREMENT PRIMARY KEY,
                        id_remetente INT,
                        id_destinatario INT,
                        conteudo TEXT,
                        data_envio DATETIME DEFAULT CURRENT_TIMESTAMP,
                        lida BOOLEAN DEFAULT FALSE,
                        FOREIGN KEY (id_remetente) REFERENCES utilizadores(id_utilizador),
                        FOREIGN KEY (id_destinatario) REFERENCES utilizadores(id_utilizador)
                    ) ENGINE=InnoDB;"); 
            } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE utilizadores ADD COLUMN password_hash VARCHAR(255) NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contratos ADD COLUMN data_expiracao DATETIME NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE salas ADD COLUMN id_professor_responsavel INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN id_equipamento INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN status VARCHAR(50) DEFAULT 'Pedido';"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE equipamentos ADD COLUMN status VARCHAR(50) DEFAULT 'Funcionando';"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("UPDATE utilizadores SET status_conta = 'Pendente' WHERE status_conta IS NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN id_agrupamento INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN id_escola INT NULL;"); } catch { }

            // Pending users
            PendingUsers = await _context.Users
                .Where(u => u.AccountStatus == "Pendente")
                .ToListAsync();

            // Totals
            TotalUsers = await _context.Users.CountAsync();
            
            // New Metrics Logic
            // 1. Contratos a expirar (dentro de 30 dias)
            var oneMonthFromNow = System.DateTime.Now.AddDays(30);
            var today = System.DateTime.Now;
            ExpiringContractsCount = await _context.Contratos
                .Where(c => c.ExpiryDate != null && c.ExpiryDate >= today && c.ExpiryDate <= oneMonthFromNow)
                .CountAsync(); 

            // 2. Tickets pendentes
            PendingTicketsCount = await _context.Tickets
                .Where(t => t.Status == "Pendente" || t.TechnicianId == null)
                .CountAsync();

            // (Calculated later in section 5 to be consistent)
            LowStockAlertsCount = 0; 

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
                    .Where(e => e.RoomId.HasValue && salaIds.Contains(e.RoomId.Value))
                    .CountAsync();
                SchoolEquipmentCounts.Add(equipCount);
            }

            // Ticket Status Chart Data
            TicketsPedidoCount = await _context.Tickets.CountAsync(t => t.Status == "Pedido");
            TicketsConcluidoCount = await _context.Tickets.CountAsync(t => t.Status == "Concluido");
            TicketsPendenteCount = await _context.Tickets.CountAsync(t => t.Status == "Pendente");

            // Line Chart Data
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

            return Page();
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
