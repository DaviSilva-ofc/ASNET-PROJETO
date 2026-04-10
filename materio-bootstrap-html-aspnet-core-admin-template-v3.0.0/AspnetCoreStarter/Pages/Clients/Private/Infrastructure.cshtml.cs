using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Private
{
    public class InfrastructureModel : PageModel
    {
        private readonly AppDbContext _context;

        public InfrastructureModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Departamento> Departamentos { get; set; } = new();
        public List<Setor> Setores { get; set; } = new();
        public List<Sala> Salas { get; set; } = new();

        [BindProperty]
        public string? NewDeptName { get; set; }
        [BindProperty]
        public string? NewSetorName { get; set; }
        [BindProperty]
        public int? SelectedDeptId { get; set; }
        [BindProperty]
        public string? NewSalaName { get; set; }
        [BindProperty]
        public int? SelectedSetorId { get; set; }

        [BindProperty]
        public int? EditId { get; set; }
        [BindProperty]
        public string? EditName { get; set; }
        [BindProperty]
        public int? EditParentId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            int userId = int.Parse(userIdStr);

            var user = await _context.Users.FindAsync(userId);
            if (user?.EmpresaId == null) return RedirectToPage("/Index");

            Departamentos = await _context.Departamentos
                .Where(d => d.EmpresaId == user.EmpresaId)
                .Include(d => d.Setores)
                    .ThenInclude(s => s.Salas)
                        .ThenInclude(sala => sala.Equipments)
                .ToListAsync();

            Setores = await _context.Setores
                .Include(s => s.Departamento)
                .Where(s => s.Departamento.EmpresaId == user.EmpresaId)
                .ToListAsync();

            Salas = await _context.Salas
                .Include(s => s.Setor)
                .Where(s => s.Setor!.Departamento!.EmpresaId == user.EmpresaId)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddDeptAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDeptName)) return RedirectToPage();
            
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            
            _context.Departamentos.Add(new Departamento { Name = NewDeptName, EmpresaId = user!.EmpresaId!.Value });
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddSetorAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSetorName) || !SelectedDeptId.HasValue) return RedirectToPage();
            _context.Setores.Add(new Setor { Name = NewSetorName, DepartamentoId = SelectedDeptId.Value });
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddSalaAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSalaName) || !SelectedSetorId.HasValue) return RedirectToPage();
            _context.Salas.Add(new Sala { Name = NewSalaName, SetorId = SelectedSetorId.Value });
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditDeptAsync()
        {
            var item = await _context.Departamentos.FindAsync(EditId);
            if (item != null && !string.IsNullOrWhiteSpace(EditName))
            {
                item.Name = EditName;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditSetorAsync()
        {
            var item = await _context.Setores.FindAsync(EditId);
            if (item != null && !string.IsNullOrWhiteSpace(EditName) && EditParentId.HasValue)
            {
                item.Name = EditName;
                item.DepartamentoId = EditParentId.Value;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditSalaAsync()
        {
            var item = await _context.Salas.FindAsync(EditId);
            if (item != null && !string.IsNullOrWhiteSpace(EditName) && EditParentId.HasValue)
            {
                item.Name = EditName;
                item.SetorId = EditParentId.Value;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteDeptAsync(int id)
        {
            var item = await _context.Departamentos.FindAsync(id);
            if (item != null)
            {
                _context.Departamentos.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSetorAsync(int id)
        {
            var item = await _context.Setores.FindAsync(id);
            if (item != null)
            {
                _context.Setores.Remove(item);
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
    }
}
