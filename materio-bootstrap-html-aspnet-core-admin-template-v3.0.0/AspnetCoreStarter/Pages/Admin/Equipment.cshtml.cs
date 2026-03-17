using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Admin
{
    public class EquipmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public EquipmentModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Equipamento> Equipments { get; set; }
        public List<Sala> Rooms { get; set; }

        [BindProperty]
        public Equipamento NewEquipment { get; set; }

        public Sala ActiveFilterRoom { get; set; }
        public Bloco ActiveFilterBloco { get; set; }
        public School ActiveFilterEscola { get; set; }
        public Agrupamento ActiveFilterAgrupamento { get; set; }

        public List<Agrupamento> AvailableAgrupamentos { get; set; }
        public List<School> AvailableEscolas { get; set; }
        public List<Bloco> AvailableBlocos { get; set; }

        public async Task<IActionResult> OnGetAsync(
            int? roomId, 
            int? blocoId, 
            int? escolaId, 
            int? agrupamentoId,
            string name,
            string type,
            string serialNumber)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Auth/Login");

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School).ThenInclude(s => s.Agrupamento)
                .AsQueryable();

            // Apply Filters
            if (!string.IsNullOrEmpty(name)) query = query.Where(e => e.Name.Contains(name));
            if (!string.IsNullOrEmpty(type)) query = query.Where(e => e.Type.Contains(type));
            if (!string.IsNullOrEmpty(serialNumber)) query = query.Where(e => e.SerialNumber.Contains(serialNumber));

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
            else if (agrupamentoId.HasValue)
            {
                query = query.Where(e => e.Room.Block.School.AgrupamentoId == agrupamentoId.Value);
                ActiveFilterAgrupamento = await _context.Agrupamentos.FindAsync(agrupamentoId.Value);
            }

            Equipments = await query.ToListAsync();
            Rooms = await _context.Salas.ToListAsync();
            AvailableAgrupamentos = await _context.Agrupamentos.ToListAsync();
            AvailableEscolas = await _context.Schools.ToListAsync();
            AvailableBlocos = await _context.Blocos.ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (ModelState.IsValid)
            {
                _context.Equipamentos.Add(NewEquipment);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var item = await _context.Equipamentos.FindAsync(id);
            if (item != null)
            {
                _context.Equipamentos.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
