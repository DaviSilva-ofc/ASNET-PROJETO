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

        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterSerialNumber { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterAgrupamentoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterEscolaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterBlocoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterRoomId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Auth/Login");

            // One-time data migration: normalize legacy equipment Artigo/Tipo values
            try {
                // Fix Monitors: Name="Monitor" + Type="Computadores" → Name="Monitores", Type="Monitor"
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE equipamentos SET nome_equipamento='Monitores', tipo='Monitor' WHERE LOWER(nome_equipamento)='monitor' AND LOWER(tipo)='computadores'");
                // Fix Computadores: Name="Computador" + Type="Computadores" → Name="Computadores", Type="Desktop"
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE equipamentos SET nome_equipamento='Computadores', tipo='Desktop' WHERE LOWER(nome_equipamento)='computador' AND LOWER(tipo)='computadores'");
                // Fix Switch: Name="Switch" + Type="Rede" → Name="Networking", Type="Switch"
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE equipamentos SET nome_equipamento='Networking', tipo='Switch' WHERE LOWER(nome_equipamento)='switch' AND LOWER(tipo)='rede'");
                // Fix any remaining "Rede" type → "Switch"
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE equipamentos SET tipo='Switch' WHERE LOWER(tipo)='rede'");
                // Fix any remaining singular "Computador" → "Computadores"
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE equipamentos SET nome_equipamento='Computadores' WHERE LOWER(nome_equipamento)='computador'");
                // Fix any remaining singular "Monitor" → "Monitores"
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE equipamentos SET nome_equipamento='Monitores' WHERE LOWER(nome_equipamento)='monitor'");
            } catch { }

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School).ThenInclude(s => s.Agrupamento)
                .AsQueryable();

            // Apply Filters (Case-insensitive and robust)
            if (!string.IsNullOrEmpty(FilterName)) 
            {
                var nameLower = FilterName.ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(nameLower) || 
                                         (e.Brand != null && e.Brand.ToLower().Contains(nameLower)) || 
                                         (e.Model != null && e.Model.ToLower().Contains(nameLower)));
            }

            if (!string.IsNullOrEmpty(FilterType)) 
            {
                var typeMatch = FilterType;
                var typeNoAccent = FilterType.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").ToLower();
                
                // Search in BOTH Type and Name to be highly inclusive
                // Allow matches in Name even if Type is NULL
                query = query.Where(e => (e.Type != null && (e.Type.Contains(typeMatch) || e.Type.ToLower().Contains(typeNoAccent))) || 
                                         (e.Name != null && e.Name.ToLower().Contains(typeNoAccent)));
            }

            if (!string.IsNullOrEmpty(FilterSerialNumber)) 
            {
                var snLower = FilterSerialNumber.ToLower();
                query = query.Where(e => e.SerialNumber != null && e.SerialNumber.ToLower().Contains(snLower));
            }

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
            else if (FilterAgrupamentoId.HasValue)
            {
                query = query.Where(e => e.Room != null && e.Room.Block.School.AgrupamentoId == FilterAgrupamentoId.Value);
                ActiveFilterAgrupamento = await _context.Agrupamentos.FindAsync(FilterAgrupamentoId.Value);
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
