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
    public class DirectorInfrastructureModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorInfrastructureModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ClientViewModel> Clients { get; set; } = new();
        public List<Agrupamento> Agrupamentos { get; set; } = new();
        public List<AspnetCoreStarter.Models.School> Schools { get; set; } = new();
        public List<Bloco> Blocos { get; set; } = new();
        public List<Sala> Salas { get; set; } = new();
        public List<User> AvailableCoordenadores { get; set; } = new();
        public List<User> AvailableProfessores { get; set; } = new();


        // Edit Properties
        [BindProperty]
        public int? EditId { get; set; }
        [BindProperty]
        public string? EditName { get; set; }
        [BindProperty]
        public int? EditParentId { get; set; }
        [BindProperty]
        public int? EditResponsibleProfessorId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Find the director's agrupamento
            var director = await _context.Diretores
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (director == null || director.AgrupamentoId == null)
            {
                // If they are a director but have no agrupamento assigned, they see nothing or an error
                return Page();
            }

            int agrupamentoId = director.AgrupamentoId.Value;
 
            // Temporary fix for missing columns in MySQL
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE salas ADD COLUMN id_professor_responsavel INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN id_equipamento INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE equipamentos ADD COLUMN status VARCHAR(50) DEFAULT 'A funcionar';"); } catch { }

            // Fetch only data related to this agrupamento
            Agrupamentos = await _context.Agrupamentos
                .Where(a => a.Id == agrupamentoId)
                .ToListAsync();

            Schools = await _context.Schools
                .Include(s => s.Agrupamento)
                .Where(s => s.AgrupamentoId == agrupamentoId)
                .ToListAsync();

            var schoolIds = Schools.Select(s => s.Id).ToList();

            Blocos = await _context.Blocos
                .Include(b => b.School)
                .Where(b => schoolIds.Contains(b.SchoolId))
                .ToListAsync();

            var blocoIds = Blocos.Select(b => b.Id).ToList();

            Salas = await _context.Salas
                .Include(s => s.Block)
                .Include(s => s.Equipments)
                .Include(s => s.ResponsibleProfessor)
                    .ThenInclude(p => p.User)
                .Where(s => s.BlockId.HasValue && blocoIds.Contains(s.BlockId.Value))
                .ToListAsync();

            // Build Client ViewModels (only one for the director's agrupamento)
            foreach (var agr in Agrupamentos)
            {
                var schoolsInAgr = Schools.Where(s => s.AgrupamentoId == agr.Id).ToList();
                
                var ticketCount = await _context.Tickets.CountAsync(t => t.SchoolId.HasValue && schoolIds.Contains(t.SchoolId.Value));
                var contractCount = await _context.Contratos.CountAsync(c => c.AgrupamentoId == agr.Id);

                var clientVm = new ClientViewModel
                {
                    Agrupamento = agr,
                    DirectorName = director.User?.Username ?? "Eu",
                    DirectorUserId = director.UserId,
                    TicketCount = ticketCount,
                    ContractCount = contractCount
                };

                foreach (var school in schoolsInAgr)
                {
                    var coordinatorRecord = await _context.Coordenadores
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.SchoolId == school.Id);
                        
                    var schoolBlocos = Blocos.Where(b => b.SchoolId == school.Id).ToList();
                    
                    clientVm.Schools.Add(new SchoolViewModel
                    {
                        School = school,
                        CoordinatorName = coordinatorRecord?.User?.Username ?? "Sem Coordenador",
                        CoordinatorUserId = coordinatorRecord?.UserId,
                        Blocos = schoolBlocos
                    });
                }
                
                Clients.Add(clientVm);
            }

            // Fetch available coordinators and professors for dropdowns (scoped or all)
            AvailableCoordenadores = await _context.Coordenadores
                .Include(c => c.User)
                .Select(c => c.User)
                .Where(u => u != null)
                .Distinct()
                .ToListAsync();

            AvailableProfessores = await _context.Professores
                .Include(p => p.User)
                .Select(p => p.User)
                .Where(u => u != null)
                .Distinct()
                .ToListAsync();

            return Page();
        }

        // --- CRUD Handlers for Blocos and Salas ---


        public async Task<IActionResult> OnPostEditBlocoAsync()
        {
            var item = await _context.Blocos.FindAsync(EditId);
            if (item != null && !string.IsNullOrEmpty(EditName) && EditParentId.HasValue)
            {
                var director = await GetDirectorAsync();
                if (director != null)
                {
                    item.Name = EditName;
                    item.SchoolId = EditParentId.Value;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Bloco atualizado com sucesso.";
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditSalaAsync()
        {
            var item = await _context.Salas.FindAsync(EditId);
            if (item != null && !string.IsNullOrEmpty(EditName) && EditParentId.HasValue)
            {
                var director = await GetDirectorAsync();
                if (director != null)
                {
                    item.Name = EditName;
                    item.BlockId = EditParentId.Value;
                    item.ResponsibleProfessorId = (EditResponsibleProfessorId.HasValue && EditResponsibleProfessorId.Value > 0) 
                        ? EditResponsibleProfessorId.Value 
                        : null;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sala atualizada com sucesso.";
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteBlocoAsync(int id)
        {
            try
            {
                var item = await _context.Blocos.FindAsync(id);
                if (item != null)
                {
                    _context.Blocos.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Bloco removido com sucesso.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Não é possível remover este bloco pois existem salas vinculadas a ele.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSalaAsync(int id)
        {
            try
            {
                var item = await _context.Salas.FindAsync(id);
                if (item != null)
                {
                    _context.Salas.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sala removida com sucesso.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Não é possível remover esta sala pois existem equipamentos vinculados a ela.";
            }
            return RedirectToPage();
        }

        private async Task<Diretor?> GetDirectorAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return null;
            return await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == userId);
        }
    }
}
