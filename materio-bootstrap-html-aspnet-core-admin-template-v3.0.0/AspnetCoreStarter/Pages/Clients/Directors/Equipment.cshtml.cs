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

        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterSerialNumber { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterEscolaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterBlocoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterRoomId { get; set; }

        public async Task<IActionResult> OnGetAsync(string? success)
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
                .Where(e => e.Room != null && e.Room.Block.School.AgrupamentoId == myAgrupamentoId) // Stay within director's agrupamento
                .AsQueryable();

            // Apply Search Filters
            if (!string.IsNullOrEmpty(FilterName)) query = query.Where(e => e.Name.Contains(FilterName));
            if (!string.IsNullOrEmpty(FilterType)) query = query.Where(e => e.Type.Contains(FilterType));
            if (!string.IsNullOrEmpty(FilterSerialNumber)) query = query.Where(e => e.SerialNumber.Contains(FilterSerialNumber));
            if (!string.IsNullOrEmpty(FilterStatus)) query = query.Where(e => e.Status == FilterStatus);

            // Apply Location Filters (limited to their agrupamento)
            if (FilterRoomId.HasValue)
            {
                query = query.Where(e => e.RoomId == FilterRoomId.Value);
                ActiveFilterRoom = await _context.Salas.FindAsync(FilterRoomId.Value);
            }
            else if (FilterBlocoId.HasValue)
            {
                query = query.Where(e => e.Room != null && e.Room.BlockId == FilterBlocoId.Value);
                ActiveFilterBloco = await _context.Blocos.FindAsync(FilterBlocoId.Value);
            }
            else if (FilterEscolaId.HasValue)
            {
                query = query.Where(e => e.Room != null && e.Room.Block.SchoolId == FilterEscolaId.Value);
                ActiveFilterEscola = await _context.Schools.FindAsync(FilterEscolaId.Value);
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
