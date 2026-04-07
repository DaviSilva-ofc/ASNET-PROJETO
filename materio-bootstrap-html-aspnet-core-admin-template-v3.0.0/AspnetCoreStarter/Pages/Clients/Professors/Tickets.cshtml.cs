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
    public class ProfessorTicketsModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProfessorTicketsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Ticket> MyTickets { get; set; } = new();
        public List<Sala> MyRooms { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SalaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? eqId { get; set; }

        [BindProperty]
        public Ticket NewTicket { get; set; } = new();

        public List<Equipamento> AvailableEquipment { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");

            int userId = int.Parse(userIdStr);

            // Fetch teacher's rooms
            MyRooms = await _context.Salas
                .Where(s => s.ResponsibleProfessorId == userId)
                .ToListAsync();

            var roomIds = MyRooms.Select(r => r.Id).ToList();

            // Fetch tickets for those rooms
            var query = _context.Tickets
                .Include(t => t.Equipamento)
                    .ThenInclude(e => e.Room)
                .Where(t => t.Equipamento != null && t.Equipamento.RoomId.HasValue && roomIds.Contains(t.Equipamento.RoomId.Value))
                .AsQueryable();

            if (SalaId.HasValue)
            {
                query = query.Where(t => t.Equipamento.RoomId == SalaId.Value);
            }

            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Todos os Estados")
            {
                query = query.Where(t => (t.Status == FilterStatus) || (FilterStatus == "Pedido" && t.Status == "Pendente") || (FilterStatus == "Em andamento" && t.Status == "Em Resolução"));
            }

            MyTickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            // Handle eqId for Novo Ticket modal auto-selection
            if (eqId.HasValue)
            {
                var eq = await _context.Equipamentos
                    .Include(e => e.Room)
                        .ThenInclude(r => r.Block)
                            .ThenInclude(b => b.School)
                    .FirstOrDefaultAsync(e => e.Id == eqId.Value);

                if (eq != null && eq.Room?.Block?.School != null)
                {
                    // Ensure the eq belongs to teacher's rooms
                    if (roomIds.Contains(eq.RoomId.Value))
                    {
                        NewTicket.EquipamentoId = eq.Id;
                        NewTicket.SchoolId = eq.Room.Block.School.Id;
                    }
                }
            }

            // Get IDs of equipment that already have active tickets (not Concluído)
            var activeTicketEqIds = await _context.Tickets
                .Where(t => t.EquipamentoId.HasValue && t.Status != "Concluído" && t.Status != "Recusado")
                .Select(t => t.EquipamentoId.Value)
                .ToListAsync();

            // Available equipment for the Novo Ticket modal (ONLY AVARIADO equipment in their rooms WITHOUT active tickets)
            AvailableEquipment = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                        .ThenInclude(b => b.School)
                .Where(e => e.RoomId.HasValue && roomIds.Contains(e.RoomId.Value))
                .Where(e => e.Status == "Avariado" || e.Status == "Indisponível" || e.Status == "Danificado")
                .Where(e => !activeTicketEqIds.Contains(e.Id))
                .OrderBy(e => e.Room.Name)
                .ThenBy(e => e.Type)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateTicketAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            
            if (NewTicket.EquipamentoId != null)
            {
                // Double check if there's already an active ticket
                var existingTicket = await _context.Tickets
                    .AnyAsync(t => t.EquipamentoId == NewTicket.EquipamentoId && t.Status != "Concluído" && t.Status != "Recusado");
                
                if (existingTicket)
                {
                    TempData["ErrorMessage"] = "Já existe um ticket ativo para este equipamento.";
                    return RedirectToPage();
                }

                var eq = await _context.Equipamentos
                    .Include(e => e.Room)
                        .ThenInclude(r => r.Block)
                    .FirstOrDefaultAsync(e => e.Id == NewTicket.EquipamentoId);
                    
                if (eq != null && eq.Room?.Block != null)
                {
                    NewTicket.SchoolId = eq.Room.Block.SchoolId;
                }
            }

            // Standardizing ticket status
            NewTicket.Status = "Pedido";
            NewTicket.CreatedAt = System.DateTime.UtcNow;

            _context.Tickets.Add(NewTicket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket criado com sucesso!";
            return RedirectToPage();
        }
    }
}
