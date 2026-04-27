using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Coordinators
{
    public class CoordinatorEquipmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public CoordinatorEquipmentModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Equipamento> Equipments { get; set; }
        public List<Sala> Rooms { get; set; } = new();
        public List<StockEmpresa> MyBorrowedItems { get; set; } = new();
        public List<Ticket> MyStockRequests { get; set; } = new();


        [BindProperty]
        public Equipamento NewEquipment { get; set; } = new();

        public string SuccessMessage { get; set; }

        public Sala ActiveFilterRoom { get; set; }
        public Bloco ActiveFilterBloco { get; set; }
        public AspnetCoreStarter.Models.School MySchool { get; set; }

        public List<Bloco> AvailableBlocos { get; set; } = new();
        public List<string> UniqueEquipmentNames { get; set; } = new();
        public List<string> UniqueBrands { get; set; } = new();
        public List<string> UniqueModels { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterBrand { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterModel { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterSerialNumber { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterBlocoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterRoomId { get; set; }

        public async Task<IActionResult> OnGetAsync(string? success)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || User.FindFirst(ClaimTypes.Role)?.Value != "Coordenador")
                return RedirectToPage("/Index");

            int userId = int.Parse(userIdStr);
            SuccessMessage = success;

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (coord?.SchoolId == null) return Page();

            int schoolId = coord.SchoolId.Value;
            MySchool = coord.School;

            AvailableBlocos = await _context.Blocos
                .Where(b => b.SchoolId == schoolId)
                .OrderBy(b => b.Name)
                .ToListAsync();

            var blocoIds = AvailableBlocos.Select(b => b.Id).ToList();

            Rooms = await _context.Salas
                .Where(s => s.BlockId.HasValue && blocoIds.Contains(s.BlockId.Value))
                .OrderBy(s => s.Name)
                .ToListAsync();

            var query = _context.Equipamentos
                .Include(e => e.Room)
                .ThenInclude(r => r.Block)
                .ThenInclude(b => b.School)
                .Where(e => e.RoomId.HasValue && Rooms.Select(r => r.Id).Contains(e.RoomId.Value))
                .AsQueryable();

            // Apply Filters
            if (!string.IsNullOrEmpty(FilterName))
                query = query.Where(e => e.Name == FilterName);
            if (!string.IsNullOrEmpty(FilterType))
                query = query.Where(e => e.Type == FilterType);
            if (!string.IsNullOrEmpty(FilterSerialNumber))
                query = query.Where(e => e.SerialNumber.Contains(FilterSerialNumber));
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
            if (FilterBlocoId.HasValue)
                query = query.Where(e => e.Room.BlockId == FilterBlocoId);
            if (FilterRoomId.HasValue)
                query = query.Where(e => e.RoomId == FilterRoomId);

            Equipments = await query.ToListAsync();

            if (FilterRoomId.HasValue) ActiveFilterRoom = Rooms.FirstOrDefault(r => r.Id == FilterRoomId);
            if (FilterBlocoId.HasValue) ActiveFilterBloco = AvailableBlocos.FirstOrDefault(b => b.Id == FilterBlocoId);

            UniqueEquipmentNames = await _context.Equipamentos
                .Where(e => e.RoomId.HasValue && Rooms.Select(r => r.Id).Contains(e.RoomId.Value))
                .Select(e => e.Name)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            // Load school-wide requests
            MyStockRequests = await _context.Tickets
                .Include(t => t.RequestedBy)
                .Where(t => t.SchoolId == schoolId && t.Level == "Empréstimo")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Load items currently assigned to the school from central stock
            MyBorrowedItems = await _context.StockEmpresa
                .Where(s => s.SchoolId == schoolId && s.Status == "Emprestado")
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateEquipmentAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");

            if (!ModelState.IsValid) return RedirectToPage(new { success = "Erro nos dados do formulário" });

            _context.Equipamentos.Add(NewEquipment);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = "Equipamento registado com sucesso!" });
        }


        public async Task<IActionResult> OnPostDeleteEquipmentAsync(int id)
        {
            var equip = await _context.Equipamentos.FindAsync(id);
            if (equip != null)
            {
                _context.Equipamentos.Remove(equip);
                await _context.SaveChangesAsync();
                return RedirectToPage(new { success = "Equipamento eliminado com sucesso!" });
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditEquipmentAsync()
        {
            if (!ModelState.IsValid) return RedirectToPage(new { success = "Erro ao validar dados do equipamento" });

            var equip = await _context.Equipamentos.FindAsync(NewEquipment.Id);
            if (equip == null) return RedirectToPage(new { success = "Equipamento não encontrado" });

            // Update fields
            equip.Name = NewEquipment.Name;
            equip.Type = NewEquipment.Type;
            equip.Brand = NewEquipment.Brand;
            equip.Model = NewEquipment.Model;
            equip.SerialNumber = NewEquipment.SerialNumber;
            equip.RoomId = NewEquipment.RoomId;

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Equipamento atualizado com sucesso!" });
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var equip = await _context.Equipamentos.FindAsync(id);
            if (equip == null) return RedirectToPage();

            if (equip.Status == "Avariado")
                equip.Status = "A funcionar";
            else
                equip.Status = "Avariado";

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = $"Estado do equipamento alterado para {equip.Status}" });
        }

        public async Task<IActionResult> OnPostCreateStockRequestAsync(string? itemName, string? itemType, int quantity, string? notes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            if (string.IsNullOrWhiteSpace(itemName))
                return RedirectToPage(new { success = "Por favor selecione um artigo." });

            var coord = await _context.Coordenadores.FirstOrDefaultAsync(c => c.UserId == userId);
            
            var dataObj = new {
                ItemName = itemName,
                ItemType = itemType,
                Quantity = quantity,
                AgrupamentoId = 0, // Fallback if no agrupamento exists
                RequestorId = userId,
                RequestorRole = "Coordenador"
            };
            var dataJson = System.Text.Json.JsonSerializer.Serialize(dataObj);

            var ticket = new Ticket
            {
                Description = $"PEDIDO DE STOCK (COORDENADOR):\nArtigo: {itemName}\nTipo: {itemType ?? "N/A"}\nQuantidade: {quantity}\nMotivo: {notes}\n\n[DATA:{dataJson}]",
                Level = "Empréstimo",
                Status = "Pendente",
                CreatedAt = DateTime.UtcNow,
                RequestedByUserId = userId,
                SchoolId = coord?.SchoolId
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = $"Pedido de {itemName} enviado com sucesso para a administração." });
        }

        public async Task<IActionResult> OnPostReturnItemAsync(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            var item = await _context.StockEmpresa.FindAsync(id);
            if (item != null)
            {
                // Return item logic: Clear school/personal assignment and set as available in central stock
                item.SchoolId = null;
                item.ProfessorId = null;
                item.DirectorId = null;
                item.TechnicianId = null;
                item.Status = "Disponível";
                item.IsAvailable = true;
                
                await _context.SaveChangesAsync();
                return RedirectToPage(new { success = "Item devolvido ao stock central com sucesso." });
            }
            return RedirectToPage();
        }
    }
}
