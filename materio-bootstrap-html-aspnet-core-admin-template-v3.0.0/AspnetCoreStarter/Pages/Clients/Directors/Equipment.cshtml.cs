using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorEquipmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorEquipmentModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Equipamento> Equipments { get; set; }
        public List<Sala> Rooms { get; set; } = new();

        [BindProperty]
        public Ticket NewTicket { get; set; } = new();

        public string SuccessMessage { get; set; }

        public Sala ActiveFilterRoom { get; set; }
        public Bloco ActiveFilterBloco { get; set; }
        public AspnetCoreStarter.Models.School ActiveFilterEscola { get; set; }
        public Agrupamento ActiveFilterAgrupamento { get; set; }

        public List<Agrupamento> AvailableAgrupamentos { get; set; } = new();
        public List<AspnetCoreStarter.Models.School> AvailableEscolas { get; set; } = new();
        public List<Bloco> AvailableBlocos { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(
            int? roomId, 
            int? blocoId, 
            int? escolaId, 
            int? agrupamentoId,
            string name,
            string type,
            string serialNumber,
            string status,
            string success)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            if (!string.IsNullOrEmpty(success)) SuccessMessage = success;

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Find the director's agrupamento
            var director = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == userId);
            if (director == null || director.AgrupamentoId == null)
            {
                Equipments = new List<Equipamento>();
                return Page();
            }

            int myAgrupamentoId = director.AgrupamentoId.Value;

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School).ThenInclude(s => s.Agrupamento)
                .Where(e => e.Room.Block.School.AgrupamentoId == myAgrupamentoId)
                .AsQueryable();

            // Apply Search Filters
            if (!string.IsNullOrEmpty(name)) query = query.Where(e => e.Name.Contains(name));
            if (!string.IsNullOrEmpty(type)) query = query.Where(e => e.Type.Contains(type));
            if (!string.IsNullOrEmpty(serialNumber)) query = query.Where(e => e.SerialNumber.Contains(serialNumber));
            if (!string.IsNullOrEmpty(status)) query = query.Where(e => e.Status == status);

            // Apply Location Filters (limited to their agrupamento)
            if (roomId.HasValue)
            {
                query = query.Where(e => e.RoomId == roomId.Value);
                ActiveFilterRoom = await _context.Salas.FindAsync(roomId.Value);
            }
            else if (blocoId.HasValue)
            {
                query = query.Where(e => e.Room.BlockId == blocoId.Value);
                ActiveFilterBloco = await _context.Blocos.FindAsync(blocoId.Value);
            }
            else if (escolaId.HasValue)
            {
                query = query.Where(e => e.Room.Block.SchoolId == escolaId.Value);
                ActiveFilterEscola = await _context.Schools.FindAsync(escolaId.Value);
            }

            Equipments = await query.ToListAsync();

            // Filter context-sensitive lists
            AvailableAgrupamentos = await _context.Agrupamentos.Where(a => a.Id == myAgrupamentoId).ToListAsync();
            AvailableEscolas = await _context.Schools.Where(s => s.AgrupamentoId == myAgrupamentoId).ToListAsync();
            var schoolIds = AvailableEscolas.Select(s => s.Id).ToList();
            
            AvailableBlocos = await _context.Blocos.Where(b => schoolIds.Contains(b.SchoolId)).ToListAsync();
            var blocoIds = AvailableBlocos.Select(b => b.Id).ToList();
            
            Rooms = await _context.Salas.Where(s => blocoIds.Contains(s.BlockId)).ToListAsync();

            ActiveFilterAgrupamento = AvailableAgrupamentos.FirstOrDefault();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateTicketAsync()
        {
            if (NewTicket.EquipamentoId == null) return Page();

            var equipment = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                .FirstOrDefaultAsync(e => e.Id == NewTicket.EquipamentoId);

            if (equipment == null) return Page();

            NewTicket.SchoolId = equipment.Room.Block.SchoolId;
            NewTicket.Status = "Pedido";
            NewTicket.CreatedAt = DateTime.UtcNow;

            _context.Tickets.Add(NewTicket);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = "Ticket de reparação solicitado com sucesso!" });
        }
    }
}
