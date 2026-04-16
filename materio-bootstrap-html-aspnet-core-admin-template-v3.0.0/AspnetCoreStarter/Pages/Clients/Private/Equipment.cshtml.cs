using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Private
{
    public class PrivateEquipmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public PrivateEquipmentModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Equipamento> Equipments { get; set; } = new();
        public List<PrivateStockItemViewModel> BorrowedItems { get; set; } = new();
        public List<Ticket> MyStockRequests { get; set; } = new();
        public HashSet<int> EquipmentWithActiveTickets { get; set; } = new();

        [BindProperty]
        public Ticket NewTicket { get; set; } = new();

        [BindProperty]
        public Equipamento NewEquipment { get; set; } = new();


        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public Empresa Empresa { get; set; }

        public List<Departamento> Departamentos { get; set; } = new();
        public List<Setor> Setores { get; set; } = new();
        public List<Sala> Salas { get; set; } = new();

        public List<string> UniqueEquipmentNames { get; set; } = new();
        public List<string> UniqueBrands { get; set; } = new();
        public List<string> UniqueTypes { get; set; } = new();
        public List<string> UniqueSerialNumbers { get; set; } = new();
        public List<Sala> UniqueRooms { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterBrand { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterSerialNumber { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? FilterRoomId { get; set; }

        public async Task<IActionResult> OnGetAsync(string? success, string? error)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            if (!string.IsNullOrEmpty(success)) SuccessMessage = success;
            if (!string.IsNullOrEmpty(error)) ErrorMessage = error;

            // Database Sync (ensure tables exist)
            await SyncDatabaseSchema();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var user = await _context.Users.Include(u => u.Empresa).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.EmpresaId == null) return RedirectToPage("/Auth/Login");

            Empresa = user.Empresa;
            int empresaId = user.EmpresaId.Value;

            // Load Infrastructure
            Departamentos = await _context.Departamentos
                .Include(d => d.Setores)
                    .ThenInclude(s => s.Salas)
                        .ThenInclude(r => r.Equipments)
                .Where(d => d.EmpresaId == empresaId)
                .ToListAsync();

            Setores = Departamentos.SelectMany(d => d.Setores).ToList();
            Salas = Setores.SelectMany(s => s.Salas).ToList();

            // Load Equipments
            var queryFull = _context.Equipamentos
                .Include(e => e.Empresa)
                .Include(e => e.Room).ThenInclude(r => r.Setor).ThenInclude(s => s.Departamento)
                .Where(e => e.EmpresaId == empresaId)
                .AsQueryable();

            var query = queryFull;

            // Filters
            if (!string.IsNullOrEmpty(FilterName)) query = query.Where(e => e.Name == FilterName);
            if (!string.IsNullOrEmpty(FilterBrand)) query = query.Where(e => e.Brand == FilterBrand);
            if (!string.IsNullOrEmpty(FilterType)) query = query.Where(e => e.Type == FilterType);
            if (!string.IsNullOrEmpty(FilterSerialNumber)) query = query.Where(e => e.SerialNumber == FilterSerialNumber);
            if (FilterRoomId.HasValue) query = query.Where(e => e.RoomId == FilterRoomId.Value);
            if (!string.IsNullOrEmpty(FilterStatus))
            {
                if (FilterStatus == "Associado")
                {
                    query = query.Where(e => e.TicketId != null);
                }
                else
                {
                    query = query.Where(e => e.Status == FilterStatus);
                }
            }

            Equipments = await query.ToListAsync();

            var activeTicketEqIds = await _context.Tickets
                .Where(t => t.EquipamentoId != null && t.Status != "Concluído")
                .Select(t => t.EquipamentoId.Value).Distinct().ToListAsync();
            EquipmentWithActiveTickets = new HashSet<int>(activeTicketEqIds);
            
            // Load Borrowed Items from ASNET Store (stock_empresa)
            var borrowedQuery = _context.StockEmpresa
                .Where(s => s.EmpresaId == empresaId && s.Status == "Emprestado")
                .AsQueryable();

            var borrowedRaw = await borrowedQuery.ToListAsync();
            BorrowedItems = borrowedRaw.GroupBy(s => new { s.EquipmentName, s.Type })
                .Select(g => new PrivateStockItemViewModel
                {
                    Id = g.First().Id,
                    Name = g.Key.EquipmentName ?? "Equipamento",
                    Category = g.Key.Type ?? "N/A",
                    Quantity = g.Count(),
                    Status = "Emprestado"
                }).ToList();

            // Populate Filter Lists from full inventory
            UniqueBrands = (await queryFull.Where(e => e.Brand != null).Select(e => e.Brand).Distinct().ToListAsync())!;
            UniqueEquipmentNames = await queryFull.Select(e => e.Name).Distinct().ToListAsync();
            UniqueTypes = (await queryFull.Where(e => e.Type != null).Select(e => e.Type).Distinct().ToListAsync())!;
            UniqueSerialNumbers = (await queryFull.Where(e => e.SerialNumber != null).Select(e => e.SerialNumber).Distinct().ToListAsync())!;
            UniqueRooms = await _context.Salas
                .Include(r => r.Setor)
                .Where(r => r.Setor != null && r.Setor.Departamento != null && r.Setor.Departamento.EmpresaId == empresaId)
                .OrderBy(r => r.Name)
                .ToListAsync();

            // Load My Stock Requests (Level Empréstimo created by this user)
            MyStockRequests = await _context.Tickets
                .Include(t => t.RequestedBy)
                .Include(t => t.Equipamento)
                .Where(t => t.RequestedByUserId == userId && t.Level == "Empréstimo")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Page();
        }

        private async Task SyncDatabaseSchema()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS departamentos (
                        id_departamento INT AUTO_INCREMENT PRIMARY KEY,
                        nome_departamento VARCHAR(100) NOT NULL,
                        id_empresa INT NOT NULL,
                        FOREIGN KEY (id_empresa) REFERENCES empresas(id_empresa) ON DELETE CASCADE
                    ) ENGINE=InnoDB;");

                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS setores (
                        id_setor INT AUTO_INCREMENT PRIMARY KEY,
                        nome_setor VARCHAR(100) NOT NULL,
                        id_departamento INT NOT NULL,
                        FOREIGN KEY (id_departamento) REFERENCES departamentos(id_departamento) ON DELETE CASCADE
                    ) ENGINE=InnoDB;");

                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE salas ADD COLUMN id_setor INT NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE salas ADD FOREIGN KEY (id_setor) REFERENCES setores(id_setor) ON DELETE SET NULL;"); } catch { }
            }
            catch { }
        }

        public async Task<IActionResult> OnPostAddEquipmentAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(int.Parse(userIdStr!));
            
            NewEquipment.EmpresaId = user!.EmpresaId;
            NewEquipment.Status = NewEquipment.Status ?? "A funcionar";
            
            _context.Equipamentos.Add(NewEquipment);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Equipamento registado com sucesso!" });
        }

        public async Task<IActionResult> OnPostEditEquipmentAsync()
        {
            var item = await _context.Equipamentos.FindAsync(NewEquipment.Id);
            if (item == null) return RedirectToPage(new { error = "Equipamento não encontrado" });

            item.Name = NewEquipment.Name;
            item.Type = NewEquipment.Type;
            item.Brand = NewEquipment.Brand;
            item.Model = NewEquipment.Model;
            item.SerialNumber = NewEquipment.SerialNumber;
            item.RoomId = NewEquipment.RoomId;
            item.Status = NewEquipment.Status;

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Equipamento atualizado com sucesso!" });
        }

        public async Task<IActionResult> OnPostDeleteEquipmentAsync(int id)
        {
            var item = await _context.Equipamentos.FindAsync(id);
            if (item != null)
            {
                _context.Equipamentos.Remove(item);
                await _context.SaveChangesAsync();
                return RedirectToPage(new { success = "Equipamento eliminado com sucesso!" });
            }
            return RedirectToPage(new { error = "Erro ao eliminar equipamento" });
        }


        public async Task<IActionResult> OnPostCreateTicketAsync()
        {
            if (NewTicket.EquipamentoId == null) return Page();
            var equip = await _context.Equipamentos.Include(e => e.Empresa).FirstOrDefaultAsync(e => e.Id == NewTicket.EquipamentoId);
            if (equip == null) return Page();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            NewTicket.RequestedByUserId = int.Parse(userIdStr!);
            NewTicket.Status = "Pedido";
            NewTicket.CreatedAt = DateTime.UtcNow;
            string userNotes = NewTicket.Description ?? "";
            NewTicket.Description = $"[EMPRESA: {equip.Empresa?.Name ?? "N/A"}] {equip.Name} ({equip.SerialNumber}) - {userNotes}";

            _context.Tickets.Add(NewTicket);
            if (equip.Status != "Avariado") equip.Status = "Avariado";
            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Ticket solicitado!" });
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var item = await _context.Equipamentos.FindAsync(id);
            if (item != null && item.Status != "Em reparo")
            {
                item.Status = (item.Status == "Avariado") ? "A funcionar" : "Avariado";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReturnLoanAsync(int sampleId, int quantity)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(int.Parse(userIdStr!));
            if (user == null || user.EmpresaId == null) return RedirectToPage("/Auth/Login");

            if (quantity <= 0) return RedirectToPage();

            var sample = await _context.StockEmpresa.FindAsync(sampleId);
            if (sample == null || sample.EmpresaId != user.EmpresaId) return RedirectToPage();

            var lentItems = await _context.StockEmpresa
                .Where(s => s.EquipmentName == sample.EquipmentName && s.Type == sample.Type && s.EmpresaId == user.EmpresaId && s.Status == "Emprestado")
                .Take(quantity)
                .ToListAsync();

            if (lentItems.Count == 0) return RedirectToPage();

            foreach (var item in lentItems)
            {
                item.Status = "Disponível";
                item.IsAvailable = true;
                item.EmpresaId = null; 
                item.SchoolId = null;
                item.AgrupamentoId = null;
            }

            var relatedPedido = await _context.PedidosStock
                .Where(p => p.RequestedByUserId == user.Id && p.Status == "Atendido" && 
                            (p.ItemName == sample.EquipmentName || p.ItemType == sample.Type))
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();

            if (relatedPedido != null)
            {
                relatedPedido.Status = "Devolvido";
                relatedPedido.UpdatedAt = System.DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = $"Devolveu {lentItems.Count} unidade(s) de {sample.EquipmentName} à Administração ASNET." });
        }

        public async Task<IActionResult> OnPostCreateStockRequestAsync(string? itemName, string? itemType, int quantity, string? notes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            var user = await _context.Users.Include(u => u.Empresa).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return RedirectToPage("/Auth/Login");

            if (string.IsNullOrWhiteSpace(itemName))
                return RedirectToPage(new { success = "" });

            var ticket = new Ticket
            {
                Description = $"PEDIDO DE STOCK (CLIENTE PRIVADO):\nArtigo: {itemName}\nTipo: {itemType ?? "N/A"}\nQuantidade: {quantity}\nMotivo: {notes}\nEmpresa: {user.Empresa?.Name ?? "N/A"}",
                Level = "Empréstimo",
                Status = "Pedido",
                CreatedAt = DateTime.UtcNow,
                RequestedByUserId = userId
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = $"Pedido de {itemName} submetido com sucesso." });
        }
    }
}
