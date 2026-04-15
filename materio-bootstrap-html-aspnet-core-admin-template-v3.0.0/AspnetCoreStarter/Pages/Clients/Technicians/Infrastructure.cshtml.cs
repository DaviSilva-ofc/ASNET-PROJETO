using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class TechnicianInfrastructureModel : PageModel
    {
        private readonly AppDbContext _context;

        public TechnicianInfrastructureModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ClientViewModel> Clients { get; set; } = new();
        public List<EmpresaViewModel> EmpresasInfra { get; set; } = new();
        public List<Sala> Salas { get; set; } = new();

        public class ClientViewModel
        {
            public Agrupamento Agrupamento { get; set; }
            public string DirectorName { get; set; }
            public int DirectorUserId { get; set; }
            public int TicketCount { get; set; }
            public int DamagedEquipCount { get; set; }
            public List<SchoolViewModel> Schools { get; set; } = new();
        }

        public class SchoolViewModel
        {
            public School School { get; set; }
            public int? CoordinatorUserId { get; set; }
            public string CoordinatorName { get; set; }
            public List<Bloco> Blocos { get; set; } = new();
        }

        public class EmpresaViewModel
        {
            public Empresa Empresa { get; set; }
            public int TicketCount { get; set; }
            public int DamagedEquipCount { get; set; }
            public int? IndividualClientId { get; set; }
            public List<Departamento> Departamentos { get; set; } = new();
            public List<Equipamento> Equipments { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && userRole != "Tecnico") return RedirectToPage("/Index");

            var userIdStr = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            // Define authorized IDs
            HashSet<int>? activeSchoolIds = null;
            HashSet<int>? activeAgrupamentoIds = null;
            HashSet<int>? activeEmpresaIds = null;

            if (userRole == "Tecnico")
            {
                var activeTickets = await _context.Tickets
                    .Include(t => t.School)
                    .Include(t => t.Equipamento)
                        .ThenInclude(e => e.Room)
                            .ThenInclude(r => r.Block)
                                .ThenInclude(b => b.School)
                    .Where(t => t.TechnicianId == userId && 
                                t.Status != "Concluído" && 
                                t.Level != "Empréstimo" && 
                                t.Level != "Alteração de Estado" && 
                                (t.Level == null || !t.Level.Contains("ltera")) && 
                                (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && 
                                (t.Level == null || !t.Level.Contains("Estado")))
                    .ToListAsync();

                activeSchoolIds = new HashSet<int>();
                activeAgrupamentoIds = new HashSet<int>();
                activeEmpresaIds = new HashSet<int>();

                foreach (var t in activeTickets)
                {
                    if (t.SchoolId.HasValue)
                    {
                        activeSchoolIds.Add(t.SchoolId.Value);
                        if (t.School?.AgrupamentoId != null) activeAgrupamentoIds.Add(t.School.AgrupamentoId.Value);
                    }
                    else if (t.Equipamento != null && t.Equipamento.Room != null && t.Equipamento.Room.Block != null)
                    {
                        activeSchoolIds.Add(t.Equipamento.Room.Block.SchoolId);
                        if (t.Equipamento.Room.Block.School?.AgrupamentoId != null)
                            activeAgrupamentoIds.Add(t.Equipamento.Room.Block.School.AgrupamentoId.Value);
                    }

                    if (t.Equipamento?.EmpresaId != null) activeEmpresaIds.Add(t.Equipamento.EmpresaId.Value);
                }
            }

            // Fetch data with optional filtering
            var agrupamentosQuery = _context.Agrupamentos.AsQueryable();
            if (activeAgrupamentoIds != null) agrupamentosQuery = agrupamentosQuery.Where(a => activeAgrupamentoIds.Contains(a.Id));
            var agrupamentos = await agrupamentosQuery.ToListAsync();

            var schoolsQuery = _context.Schools.Include(s => s.Agrupamento).AsQueryable();
            if (activeSchoolIds != null) schoolsQuery = schoolsQuery.Where(s => activeSchoolIds.Contains(s.Id) || (s.AgrupamentoId.HasValue && activeAgrupamentoIds!.Contains(s.AgrupamentoId.Value)));
            var allSchools = await schoolsQuery.ToListAsync();

            var validSchoolIdsList = allSchools.Select(s => s.Id).ToList();

            var blocosQuery = _context.Blocos.AsQueryable();
            if (activeSchoolIds != null) blocosQuery = blocosQuery.Where(b => validSchoolIdsList.Contains(b.SchoolId));
            var allBlocos = await blocosQuery.ToListAsync();

            var salasQuery = _context.Salas
                .Include(s => s.Block)
                .Include(s => s.Equipments)
                .Include(s => s.ResponsibleProfessor).ThenInclude(p => p.User)
                .AsQueryable();
            if (activeSchoolIds != null) salasQuery = salasQuery.Where(s => s.BlockId.HasValue && allBlocos.Select(b => b.Id).Contains(s.BlockId.Value));
            Salas = await salasQuery.ToListAsync();

            // Build Client ViewModels (Schools Hierarchy)
            foreach (var agr in agrupamentos)
            {
                var director = await _context.Diretores
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.AgrupamentoId == agr.Id);

                var schoolsInAgr = allSchools.Where(s => s.AgrupamentoId == agr.Id).ToList();
                var schoolIds = schoolsInAgr.Select(s => s.Id).ToList();
                
                var ticketCount = await _context.Tickets.CountAsync(t => t.SchoolId.HasValue && schoolIds.Contains(t.SchoolId.Value) && t.Status != "Concluído" && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && (t.Level == null || !t.Level.Contains("Estado")));
                var damagedEquipCount = await _context.Equipamentos
                    .CountAsync(e => e.RoomId.HasValue && _context.Salas.Any(s => s.Id == e.RoomId.Value && s.Block != null && schoolIds.Contains(s.Block.SchoolId)) && e.Status == "Avariado");

                var clientVm = new ClientViewModel
                {
                    Agrupamento = agr,
                    DirectorName = director?.User?.Username ?? "Sem Diretor",
                    DirectorUserId = director?.UserId ?? 0,
                    TicketCount = ticketCount,
                    DamagedEquipCount = damagedEquipCount
                };

                foreach (var school in schoolsInAgr)
                {
                    var coordinatorRecord = await _context.Coordenadores
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.SchoolId == school.Id);
                        
                    clientVm.Schools.Add(new SchoolViewModel
                    {
                        School = school,
                        CoordinatorUserId = coordinatorRecord?.UserId,
                        CoordinatorName = coordinatorRecord?.User?.Username ?? "Sem Coordenador",
                        Blocos = allBlocos.Where(b => b.SchoolId == school.Id).ToList()
                    });
                }
                
                Clients.Add(clientVm);
            }

            // Build Empresa ViewModels
            var empresasQuery = _context.Empresas.AsQueryable();
            if (activeEmpresaIds != null) empresasQuery = empresasQuery.Where(e => activeEmpresaIds.Contains(e.Id));
            var allEmpresas = await empresasQuery.ToListAsync();

            foreach (var emp in allEmpresas)
            {
                var ticketCount = await _context.Tickets
                    .CountAsync(t => t.Status != "Concluído" && t.EquipamentoId.HasValue && _context.Equipamentos.Any(e => e.Id == t.EquipamentoId.Value && e.EmpresaId == emp.Id) && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")) && (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")));
                
                var damagedEquipCount = await _context.Equipamentos.CountAsync(e => e.EmpresaId == emp.Id && e.Status == "Avariado");
                var indClientUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmpresaId == emp.Id && !_context.Administradores.Any(a => a.UserId == u.Id));

                var departamentos = await _context.Departamentos
                    .Where(d => d.EmpresaId == emp.Id)
                    .Include(d => d.Setores)
                        .ThenInclude(s => s.Salas)
                            .ThenInclude(sala => sala.Equipments)
                    .ToListAsync();

                var salaIds = departamentos
                    .SelectMany(d => d.Setores)
                    .SelectMany(s => s.Salas)
                    .Select(s => s.Id)
                    .ToList();

                var directEquipments = await _context.Equipamentos
                    .Where(e => e.EmpresaId == emp.Id && (!e.RoomId.HasValue || !salaIds.Contains(e.RoomId.Value)))
                    .ToListAsync();

                EmpresasInfra.Add(new EmpresaViewModel
                {
                    Empresa = emp,
                    TicketCount = ticketCount,
                    DamagedEquipCount = damagedEquipCount,
                    IndividualClientId = indClientUser?.Id,
                    Departamentos = departamentos,
                    Equipments = directEquipments
                });
            }

            return Page();
        }
    }
}
