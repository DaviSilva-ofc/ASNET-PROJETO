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
                .Where(s => blocoIds.Contains(s.BlockId))
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

            return Page();
        }
    }
}
