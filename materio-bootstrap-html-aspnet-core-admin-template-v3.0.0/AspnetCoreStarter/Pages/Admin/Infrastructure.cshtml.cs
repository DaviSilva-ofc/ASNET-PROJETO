using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Admin
{
    public class InfrastructureModel : PageModel
    {
        private readonly AppDbContext _context;

        public InfrastructureModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Agrupamento> Agrupamentos { get; set; }
        public List<AspnetCoreStarter.Models.School> Schools { get; set; }
        public List<Bloco> Blocos { get; set; }
        public List<Sala> Salas { get; set; }

        [BindProperty]
        public string NewAgrupamentoName { get; set; }

        [BindProperty]
        public string NewSchoolName { get; set; }
        [BindProperty]
        public string NewSchoolAddress { get; set; }
        [BindProperty]
        public int? SelectedAgrupamentoId { get; set; }

        [BindProperty]
        public string NewBlocoName { get; set; }
        [BindProperty]
        public int SelectedSchoolId { get; set; }

        [BindProperty]
        public string NewSalaName { get; set; }
        [BindProperty]
        public int SelectedBlocoId { get; set; }

        // Edit Properties
        [BindProperty]
        public int EditId { get; set; }
        [BindProperty]
        public string EditName { get; set; }
        [BindProperty]
        public string EditAddress { get; set; }
        [BindProperty]
        public int? EditParentId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin") return RedirectToPage("/Index");

            Agrupamentos = await _context.Agrupamentos.ToListAsync();
            Schools = await _context.Schools.Include(s => s.Agrupamento).ToListAsync();
            Blocos = await _context.Blocos.Include(b => b.School).ToListAsync();
            Salas = await _context.Salas.Include(s => s.Block).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddAgrupamentoAsync()
        {
            if (!string.IsNullOrEmpty(NewAgrupamentoName))
            {
                _context.Agrupamentos.Add(new Agrupamento { Name = NewAgrupamentoName });
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddSchoolAsync()
        {
            if (!string.IsNullOrEmpty(NewSchoolName))
            {
                _context.Schools.Add(new AspnetCoreStarter.Models.School 
                { 
                    Name = NewSchoolName, 
                    Address = NewSchoolAddress ?? "N/A",
                    AgrupamentoId = SelectedAgrupamentoId
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddBlocoAsync()
        {
            if (!string.IsNullOrEmpty(NewBlocoName))
            {
                _context.Blocos.Add(new Bloco { Name = NewBlocoName, SchoolId = SelectedSchoolId });
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddSalaAsync()
        {
            if (!string.IsNullOrEmpty(NewSalaName))
            {
                _context.Salas.Add(new Sala { Name = NewSalaName, BlockId = SelectedBlocoId });
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAgrupamentoAsync(int id)
        {
            var item = await _context.Agrupamentos.FindAsync(id);
            if (item != null)
            {
                _context.Agrupamentos.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSchoolAsync(int id)
        {
            var item = await _context.Schools.FindAsync(id);
            if (item != null)
            {
                _context.Schools.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteBlocoAsync(int id)
        {
            var item = await _context.Blocos.FindAsync(id);
            if (item != null)
            {
                _context.Blocos.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSalaAsync(int id)
        {
            var item = await _context.Salas.FindAsync(id);
            if (item != null)
            {
                _context.Salas.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        // --- Edit Handlers ---
        public async Task<IActionResult> OnPostEditAgrupamentoAsync()
        {
            var item = await _context.Agrupamentos.FindAsync(EditId);
            if (item != null && !string.IsNullOrEmpty(EditName))
            {
                item.Name = EditName;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditSchoolAsync()
        {
            var item = await _context.Schools.FindAsync(EditId);
            if (item != null && !string.IsNullOrEmpty(EditName))
            {
                item.Name = EditName;
                item.Address = EditAddress ?? "N/A";
                item.AgrupamentoId = EditParentId;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditBlocoAsync()
        {
            var item = await _context.Blocos.FindAsync(EditId);
            if (item != null && !string.IsNullOrEmpty(EditName) && EditParentId.HasValue)
            {
                item.Name = EditName;
                item.SchoolId = EditParentId.Value;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditSalaAsync()
        {
            var item = await _context.Salas.FindAsync(EditId);
            if (item != null && !string.IsNullOrEmpty(EditName) && EditParentId.HasValue)
            {
                item.Name = EditName;
                item.BlockId = EditParentId.Value;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
