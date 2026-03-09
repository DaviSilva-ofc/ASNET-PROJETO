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

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Auth/Login");

            Equipments = await _context.Equipamentos.Include(e => e.Room).ThenInclude(r => r.Block).ToListAsync();
            Rooms = await _context.Salas.ToListAsync();

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
