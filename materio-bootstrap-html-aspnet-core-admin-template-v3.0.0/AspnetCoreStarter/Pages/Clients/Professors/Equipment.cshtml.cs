using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using System;

namespace AspnetCoreStarter.Pages.Clients.Professors
{
    public class EquipmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public EquipmentModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Equipamento> Equipments { get; set; } = new();
        public List<Sala> MyRooms { get; set; } = new();
        public List<StockEmpresa> MyBorrowedItems { get; set; } = new();
        public List<Ticket> MyStockRequests { get; set; } = new();
        public HashSet<int> EquipmentWithActiveTickets { get; set; } = new();

        [BindProperty]
        public Ticket NewTicket { get; set; } = new();

        public string SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SalaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }

        public async Task<IActionResult> OnGetAsync(string? success)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            if (!string.IsNullOrEmpty(success)) SuccessMessage = success;

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");

            int userId = int.Parse(userIdStr);

            // Fetch teacher's rooms
            MyRooms = await _context.Salas
                .Where(s => s.ResponsibleProfessorId == userId)
                .ToListAsync();

            var roomIds = MyRooms.Select(r => r.Id).ToList();

            // Fetch equipment in those rooms
            var query = _context.Equipamentos
                .Include(e => e.Room)
                .Where(e => e.RoomId.HasValue && roomIds.Contains(e.RoomId.Value))
                .AsQueryable();

            if (SalaId.HasValue)
            {
                query = query.Where(e => e.RoomId == SalaId.Value);
            }

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                if (FilterStatus == "Associado")
                    query = query.Where(e => e.TicketId != null);
                else if (FilterStatus == "Avariado")
                    query = query.Where(e => e.Status == "Avariado" || e.Status == "Indisponível");
                else if (FilterStatus == "Funcionando")
                    query = query.Where(e => e.Status == "A funcionar" || e.Status == "Funcionando" || e.Status == "Disponível" || string.IsNullOrEmpty(e.Status));
            }

            if (!string.IsNullOrEmpty(FilterName))
            {
                query = query.Where(e => e.Name.Contains(FilterName));
            }

            Equipments = await query.ToListAsync();

            // Fetch IDs of equipment with active tickets
            var eqIds = Equipments.Select(e => e.Id).ToList();
            var activeTickets = await _context.Tickets
                .Where(t => t.EquipamentoId.HasValue && eqIds.Contains(t.EquipamentoId.Value) && t.Status != "Concluído" && t.Status != "Recusado")
                .Select(t => t.EquipamentoId.Value)
                .ToListAsync();
            
            EquipmentWithActiveTickets = new HashSet<int>(activeTickets);

            // Fetch items borrowed by the professor
            MyBorrowedItems = await _context.StockEmpresa
                .Where(s => s.ProfessorId == userId)
                .ToListAsync();

            // Fetch pending requests from this user
            MyStockRequests = await _context.Tickets
                .Where(t => t.RequestedByUserId == userId && t.Level == "Empréstimo" && t.Status == "Pedido")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Page();
        }



        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");

            int userId = int.Parse(userIdStr);

            var equipment = await _context.Equipamentos
                .Include(e => e.Room)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (equipment == null || !equipment.RoomId.HasValue) return RedirectToPage();

            // Ensure professor is responsible for this room
            if (equipment.Room.ResponsibleProfessorId != userId)
                return Forbid();

            bool isDamaged = equipment.Status == "Avariado" || equipment.Status == "Indisponível";

            if (isDamaged)
            {
                equipment.Status = "A funcionar";
                SuccessMessage = "Equipamento marcado como funcional.";
            }
            else
            {
                equipment.Status = "Avariado";
                SuccessMessage = "Equipamento marcado como avariado.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = SuccessMessage });
        }
 
        public async Task<IActionResult> OnPostCreateStockRequestAsync(string? itemName, string? itemType, int quantity, string? notes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                return RedirectToPage(new { success = "Erro: Por favor selecione um artigo." });
            }

            // Get professor's school to help administration route the request
            var room = await _context.Salas.Include(r => r.Block).ThenInclude(b => b.School).FirstOrDefaultAsync(r => r.ResponsibleProfessorId == userId);

            var dataObj = new {
                ItemName = itemName,
                ItemType = itemType,
                Quantity = quantity,
                AgrupamentoId = 0,
                RequestorId = userId,
                RequestorRole = "Professor"
            };
            var dataJson = System.Text.Json.JsonSerializer.Serialize(dataObj);

            var ticket = new Ticket
            {
                Description = $"PEDIDO DE EQUIPAMENTO (PROFESSOR):\nArtigo: {itemName}\nTipo: {itemType ?? "N/A"}\nQuantidade: {quantity}\nMotivo: {notes}\nLocalização Sugerida: {room?.Name} ({room?.Block?.School?.Name})\n\n[DATA:{dataJson}]",
                Level = "Empréstimo",
                Status = "Pedido",
                CreatedAt = DateTime.UtcNow,
                RequestedByUserId = userId,
                SchoolId = room?.Block?.SchoolId
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

            var item = await _context.StockEmpresa.FirstOrDefaultAsync(s => s.Id == id && s.ProfessorId == userId);
            if (item != null)
            {
                item.ProfessorId = null;
                item.Status = "Disponível";
                item.IsAvailable = true;
                
                await _context.SaveChangesAsync();
                return RedirectToPage(new { success = "Equipamento devolvido com sucesso." });
            }
            
            return RedirectToPage();
        }
    }
}
