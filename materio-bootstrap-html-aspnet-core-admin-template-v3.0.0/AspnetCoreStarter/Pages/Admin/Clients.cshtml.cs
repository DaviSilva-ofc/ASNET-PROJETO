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

        public List<Agrupamento> Agrupamentos { get; set; } = new();
        public List<AspnetCoreStarter.Models.School> Schools { get; set; } = new();
        public List<Bloco> Blocos { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? FilterId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterEmail { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? FilterAgrupamento { get; set; }

        [BindProperty]
        public string NewUserName { get; set; }
        [BindProperty]
        public string NewUserEmail { get; set; }
        [BindProperty]
        public string NewUserPassword { get; set; }
        [BindProperty]
        public int? SelectedParentId { get; set; }

        [BindProperty]
        public int EditUserId { get; set; }
        [BindProperty]
        public string? EditUserName { get; set; }
        [BindProperty]
        public string? EditUserEmail { get; set; }
        [BindProperty]
        public string? EditUserPassword { get; set; }
        [BindProperty]
        public string? EditUserRole { get; set; }

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

            DirectorsList = await directorsQuery.ToListAsync();
            CoordinatorsList = await coordinatorsQuery.ToListAsync();
            ProfessorsList = await professorsQuery.ToListAsync();

            Agrupamentos = await _context.Agrupamentos.ToListAsync();
            Schools = await _context.Schools.ToListAsync();
            Blocos = await _context.Blocos.ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddDiretorAsync()
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

            var diretor = new Diretor
            {
                UserId = user.Id,
                AgrupamentoId = SelectedParentId
            };

            _context.Diretores.Add(diretor);
            await _context.SaveChangesAsync();

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
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(EditUserPassword);

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
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Utilizador atualizado com sucesso.";
            return RedirectToPage();
        }
    }
}
