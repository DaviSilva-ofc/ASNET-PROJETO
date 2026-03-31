using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Admin
{
    public class ClientsModel : PageModel
    {
        private readonly AppDbContext _context;

        public ClientsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Diretor> DirectorsList { get; set; } = new();
        public List<Coordenador> CoordinatorsList { get; set; } = new();
        public List<Professor> ProfessorsList { get; set; } = new();
        public List<User> IndependentClientsList { get; set; } = new();

        public List<Agrupamento> Agrupamentos { get; set; } = new();
        public List<AspnetCoreStarter.Models.School> Schools { get; set; } = new();
        public List<Bloco> Blocos { get; set; } = new();
        public List<Sala> Salas { get; set; } = new();
        public List<Empresa> Empresas { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? FilterId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterEmail { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? FilterAgrupamento { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? FilterEmpresa { get; set; }

        [BindProperty]
        public string NewUserName { get; set; }
        [BindProperty]
        public string NewUserEmail { get; set; }
        [BindProperty]
        public string NewUserPassword { get; set; }
        [BindProperty]
        public string NewUserRole { get; set; }
        [BindProperty]
        public int? SelectedParentId { get; set; }
        [BindProperty]
        public int? NewUserEmpresaId { get; set; }

        [BindProperty]
        public string? EditUserRole { get; set; }
        [BindProperty]
        public int EditUserId { get; set; }
        [BindProperty]
        public string? EditUserName { get; set; }
        [BindProperty]
        public string? EditUserEmail { get; set; }
        [BindProperty]
        public string? EditUserPassword { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin") return RedirectToPage("/Index");

            // Base queries
            var directorsQuery = _context.Diretores
                .Include(d => d.User)
                .Include(d => d.Agrupamento)
                .AsQueryable();

            var coordinatorsQuery = _context.Coordenadores
                .Include(c => c.User)
                .Include(c => c.School)
                    .ThenInclude(s => s.Agrupamento)
                .AsQueryable();

            var professorsQuery = _context.Professores
                .Include(p => p.User)
                .Include(p => p.Bloco)
                    .ThenInclude(b => b.School)
                        .ThenInclude(s => s.Agrupamento)
                .AsQueryable();

            // Apply Filters
            if (FilterId.HasValue)
            {
                directorsQuery = directorsQuery.Where(d => d.UserId == FilterId.Value);
                coordinatorsQuery = coordinatorsQuery.Where(c => c.UserId == FilterId.Value);
                professorsQuery = professorsQuery.Where(p => p.UserId == FilterId.Value);
            }

            if (!string.IsNullOrEmpty(FilterName))
            {
                directorsQuery = directorsQuery.Where(d => d.User.Username.Contains(FilterName));
                coordinatorsQuery = coordinatorsQuery.Where(c => c.User.Username.Contains(FilterName));
                professorsQuery = professorsQuery.Where(p => p.User.Username.Contains(FilterName));
            }

            if (!string.IsNullOrEmpty(FilterEmail))
            {
                directorsQuery = directorsQuery.Where(d => d.User.Email.Contains(FilterEmail));
                coordinatorsQuery = coordinatorsQuery.Where(c => c.User.Email.Contains(FilterEmail));
                professorsQuery = professorsQuery.Where(p => p.User.Email.Contains(FilterEmail));
            }

            if (FilterAgrupamento.HasValue && FilterAgrupamento.Value > 0)
            {
                directorsQuery = directorsQuery.Where(d => d.AgrupamentoId == FilterAgrupamento.Value);
                coordinatorsQuery = coordinatorsQuery.Where(c => c.School.AgrupamentoId == FilterAgrupamento.Value);
                professorsQuery = professorsQuery.Where(p => p.Bloco.School.AgrupamentoId == FilterAgrupamento.Value);
            }

            // Mutual exclusion: if an Empresa consists of independent clients, hide other types
            if (FilterEmpresa.HasValue && FilterEmpresa.Value > 0)
            {
                directorsQuery = directorsQuery.Where(d => false);
                coordinatorsQuery = coordinatorsQuery.Where(c => false);
                professorsQuery = professorsQuery.Where(p => false);
            }

            // Apply type filter — only load relevant sections
            bool showDirectors     = string.IsNullOrEmpty(FilterType) || FilterType == "Diretor";
            bool showCoordinators  = string.IsNullOrEmpty(FilterType) || FilterType == "Coordenador";
            bool showProfessors    = string.IsNullOrEmpty(FilterType) || FilterType == "Professor";
            bool showIndependent   = string.IsNullOrEmpty(FilterType) || FilterType == "Cliente Individual";

            DirectorsList    = showDirectors    ? await directorsQuery.ToListAsync()    : new();
            CoordinatorsList = showCoordinators ? await coordinatorsQuery.ToListAsync() : new();
            ProfessorsList   = showProfessors   ? await professorsQuery.ToListAsync()   : new();

            // Independent Clients: Users who are NOT directors, coordinators, professors or admins
            var directorUserIds    = await _context.Diretores.Select(d => d.UserId).ToListAsync();
            var coordinatorUserIds = await _context.Coordenadores.Select(c => c.UserId).ToListAsync();
            var professorUserIds   = await _context.Professores.Select(p => p.UserId).ToListAsync();
            var adminUserIds       = await _context.Administradores.Select(a => a.UserId).ToListAsync();
            
            var excludeIds = directorUserIds.Union(coordinatorUserIds).Union(professorUserIds).Union(adminUserIds).ToList();

            if (showIndependent)
            {
                var indQuery = _context.Users
                    .Include(u => u.Empresa)
                    .Where(u => !excludeIds.Contains(u.Id));

                // Apply common text filters to independent clients too
                if (FilterId.HasValue)
                    indQuery = indQuery.Where(u => u.Id == FilterId.Value);
                if (!string.IsNullOrEmpty(FilterName))
                    indQuery = indQuery.Where(u => u.Username.Contains(FilterName));
                if (!string.IsNullOrEmpty(FilterEmail))
                    indQuery = indQuery.Where(u => u.Email.Contains(FilterEmail));
                if (FilterEmpresa.HasValue && FilterEmpresa.Value > 0)
                    indQuery = indQuery.Where(u => u.EmpresaId == FilterEmpresa.Value);

                // Mutual exclusion: if an Agrupamento is selected, hide independent clients
                if (FilterAgrupamento.HasValue && FilterAgrupamento.Value > 0)
                    indQuery = indQuery.Where(u => false);

                IndependentClientsList = await indQuery.ToListAsync();
            }

            Agrupamentos = await _context.Agrupamentos.ToListAsync();
            Schools = await _context.Schools.ToListAsync();
            Blocos = await _context.Blocos.ToListAsync();
            Salas = await _context.Salas.ToListAsync();
            Empresas = await _context.Empresas.ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddDiretorAsync()
        {
            NewUserRole = "Diretor";
            return await OnPostAddUserAsync();
        }

        public async Task<IActionResult> OnPostAddUserAsync()
        {
            if (string.IsNullOrEmpty(NewUserName) || string.IsNullOrEmpty(NewUserEmail) || string.IsNullOrEmpty(NewUserPassword))
            {
                TempData["ErrorMessage"] = "Todos os campos são obrigatórios.";
                return RedirectToPage();
            }

            // check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == NewUserEmail))
            {
                TempData["ErrorMessage"] = "Este email já está registado.";
                return RedirectToPage();
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewUserPassword);
            var user = new User
            {
                Username = NewUserName,
                Email = NewUserEmail,
                Password = hashedPassword,
                PasswordHash = hashedPassword,
                AccountStatus = "Ativo"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (NewUserRole == "Diretor")
            {
                var record = new Diretor { UserId = user.Id, AgrupamentoId = SelectedParentId };
                _context.Diretores.Add(record);
            }
            else if (NewUserRole == "Coordenador")
            {
                var record = new Coordenador { UserId = user.Id, SchoolId = SelectedParentId };
                _context.Coordenadores.Add(record);
            }
            else if (NewUserRole == "Professor")
            {
                var record = new Professor { UserId = user.Id, BlocoId = SelectedParentId };
                _context.Professores.Add(record);
            }
            else if (NewUserRole == "Cliente Independente")
            {
                user.EmpresaId = NewUserEmpresaId;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Utilizador registado com sucesso.";
            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostAddCoordenadorAsync()
        {
            if (string.IsNullOrEmpty(NewUserName) || string.IsNullOrEmpty(NewUserEmail) || string.IsNullOrEmpty(NewUserPassword))
                return RedirectToPage();

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewUserPassword);
            var user = new User
            {
                Username = NewUserName,
                Email = NewUserEmail,
                Password = hashedPassword,
                PasswordHash = hashedPassword,
                AccountStatus = "Ativo"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var coord = new Coordenador
            {
                UserId = user.Id,
                SchoolId = SelectedParentId
            };

            _context.Coordenadores.Add(coord);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddProfessorAsync()
        {
            if (string.IsNullOrEmpty(NewUserName) || string.IsNullOrEmpty(NewUserEmail) || string.IsNullOrEmpty(NewUserPassword))
                return RedirectToPage();

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewUserPassword);
            var user = new User
            {
                Username = NewUserName,
                Email = NewUserEmail,
                Password = hashedPassword,
                PasswordHash = hashedPassword,
                AccountStatus = "Ativo"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var professor = new Professor
            {
                UserId = user.Id,
                BlocoId = SelectedParentId
            };

            _context.Professores.Add(professor);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(int id, string role)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (role == "Diretor") {
                    var r = await _context.Diretores.FirstOrDefaultAsync(x => x.UserId == id);
                    if (r != null) _context.Diretores.Remove(r);
                } else if (role == "Coordenador") {
                    var r = await _context.Coordenadores.FirstOrDefaultAsync(x => x.UserId == id);
                    if (r != null) _context.Coordenadores.Remove(r);
                } else if (role == "Professor") {
                    var r = await _context.Professores.FirstOrDefaultAsync(x => x.UserId == id);
                    if (r != null) _context.Professores.Remove(r);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Utilizador removido com sucesso.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAgrupamentoAsync(int id)
        {
            try {
                var item = await _context.Agrupamentos.FindAsync(id);
                if (item != null) {
                    _context.Agrupamentos.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Agrupamento removido com sucesso.";
                }
            } catch {
                TempData["ErrorMessage"] = "Não é possível remover este agrupamento pois existem escolas ou diretores vinculados.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSchoolAsync(int id)
        {
            try {
                var item = await _context.Schools.FindAsync(id);
                if (item != null) {
                    _context.Schools.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Escola removida com sucesso.";
                }
            } catch {
                TempData["ErrorMessage"] = "Não é possível remover esta escola pois existem blocos ou coordenadores vinculados.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteBlocoAsync(int id)
        {
            try {
                var item = await _context.Blocos.FindAsync(id);
                if (item != null) {
                    _context.Blocos.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Bloco removido com sucesso.";
                }
            } catch {
                TempData["ErrorMessage"] = "Não é possível remover este bloco pois existem salas vinculadas.";
            }
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostDeleteSalaAsync(int id)
        {
            try {
                var item = await _context.Salas.FindAsync(id);
                if (item != null) {
                    _context.Salas.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sala removida com sucesso.";
                }
            } catch {
                TempData["ErrorMessage"] = "Não é possível remover esta sala pois existem equipamentos vinculados.";
            }
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostEditUserAsync()
        {
            var user = await _context.Users.FindAsync(EditUserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizador não encontrado.";
                return RedirectToPage();
            }

            if (!string.IsNullOrWhiteSpace(EditUserName))
                user.Username = EditUserName.Trim();

            if (!string.IsNullOrWhiteSpace(EditUserEmail))
                user.Email = EditUserEmail.Trim();

            if (!string.IsNullOrWhiteSpace(EditUserPassword))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(EditUserPassword);
                user.PasswordHash = hash;
                user.Password = hash; // Ensure compatibility with systems using the old field
            }

            // Role-specific reassignment (Agrupamento/School/Bloco)
            if (SelectedParentId.HasValue && SelectedParentId.Value > 0)
            {
                if (EditUserRole == "Diretor")
                {
                    var record = await _context.Diretores.FirstOrDefaultAsync(x => x.UserId == EditUserId);
                    if (record != null) record.AgrupamentoId = SelectedParentId.Value;
                }
                else if (EditUserRole == "Coordenador")
                {
                    var record = await _context.Coordenadores.FirstOrDefaultAsync(x => x.UserId == EditUserId);
                    if (record != null) record.SchoolId = SelectedParentId.Value;
                }
                else if (EditUserRole == "Professor")
                {
                    var record = await _context.Professores.FirstOrDefaultAsync(x => x.UserId == EditUserId);
                    if (record != null) record.BlocoId = SelectedParentId.Value;
                }
                else if (EditUserRole == "Cliente Independente")
                {
                    user.EmpresaId = SelectedParentId;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Utilizador atualizado com sucesso.";
            return RedirectToPage();
        }
    }
}
