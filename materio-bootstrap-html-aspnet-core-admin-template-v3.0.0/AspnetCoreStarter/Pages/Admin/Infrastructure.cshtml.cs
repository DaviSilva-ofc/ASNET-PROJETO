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

        public List<ClientViewModel> Clients { get; set; } = new();
        public List<User> AvailableDirectors { get; set; } = new();
        public List<User> AvailableCoordenadores { get; set; } = new();
        public List<User> AvailableProfessores { get; set; } = new();
        public List<User> AvailableIndividualClients { get; set; } = new();
        public List<EmpresaViewModel> EmpresasInfra { get; set; } = new();

        public class EmpresaViewModel
        {
            public Empresa Empresa { get; set; }
            public List<User> Responsibles { get; set; } = new();
            public int TicketCount { get; set; }
            public int ContractCount { get; set; }
            public List<Equipamento> Equipments { get; set; } = new();
            public string Abbreviation { get; set; } = "";
            public int? IndividualClientId { get; set; }
        }

        // Existing properties for modals (kept for compatibility)
        public List<Agrupamento> Agrupamentos { get; set; } = new();
        public List<AspnetCoreStarter.Models.School> Schools { get; set; } = new();
        public List<Bloco> Blocos { get; set; } = new();
        public List<Sala> Salas { get; set; } = new();

        [BindProperty]
        public string? NewAgrupamentoName { get; set; }
        [BindProperty]
        public string? NewSchoolName { get; set; }
        [BindProperty]
        public string? NewSchoolAddress { get; set; }
        [BindProperty]
        public int? SelectedAgrupamentoId { get; set; }
        [BindProperty]
        public int? NewSchoolCoordinatorId { get; set; }
        [BindProperty]
        public string? NewBlocoName { get; set; }
        [BindProperty]
        public int? SelectedSchoolId { get; set; }
        [BindProperty]
        public string? NewSalaName { get; set; }
        [BindProperty]
        public int? SelectedBlocoId { get; set; }

        // Establishment Creation Properties (Migrated from Clients)
        [BindProperty]
        public string? NewEstabName { get; set; }
        [BindProperty]
        public string? NewEstabType { get; set; }
        [BindProperty]
        public int? NewEstabParentId { get; set; }
        [BindProperty]
        public string? NewEstabAddress { get; set; }
        [BindProperty]
        public int? NewEstabCoordinatorId { get; set; }
        [BindProperty]
        public int? NewEstabProfessorId { get; set; }
        [BindProperty]
        public int? NewEstabIndividualClientId { get; set; }

        // Edit Properties
        [BindProperty]
        public int? EditId { get; set; }
        [BindProperty]
        public string? EditName { get; set; }
        [BindProperty]
        public string? EditAddress { get; set; }
        [BindProperty]
        public int? EditParentId { get; set; }
        [BindProperty]
        public int? EditCoordinatorId { get; set; }
        [BindProperty]
        public int? EditSchoolId { get; set; }
        [BindProperty]
        public string? EditSchoolName { get; set; }
        [BindProperty]
        public int? EditBlocoId { get; set; }
        [BindProperty]
        public string? EditBlocoName { get; set; }
        [BindProperty]
        public int? EditSalaId { get; set; }
        [BindProperty]
        public string? EditSalaName { get; set; }
        [BindProperty]
        public int? EditResponsibleProfessorId { get; set; }
        [BindProperty]
        public int? EditIndividualClientId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin") return RedirectToPage("/Index");

            // Temporary fix for missing columns
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE utilizadores ADD COLUMN password_hash VARCHAR(255) NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE salas ADD COLUMN id_professor_responsavel INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN id_equipamento INT NULL;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN status VARCHAR(50) DEFAULT 'Pedido';"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP;"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE equipamentos ADD COLUMN status VARCHAR(50) DEFAULT 'Funcionando';"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE equipamentos ADD COLUMN id_empresa INT NULL;"); } catch { }

            Agrupamentos = await _context.Agrupamentos.ToListAsync();
            Schools = await _context.Schools.Include(s => s.Agrupamento).ToListAsync();
            Blocos = await _context.Blocos.Include(b => b.School).ToListAsync();
            Salas = await _context.Salas
                .Include(s => s.Block)
                .Include(s => s.Equipments)
                .Include(s => s.ResponsibleProfessor)
                    .ThenInclude(p => p.User)
                .ToListAsync();

            // Build Client ViewModels
            foreach (var agr in Agrupamentos)
            {
                var director = await _context.Diretores
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.AgrupamentoId == agr.Id);

                var schoolsInAgr = Schools.Where(s => s.AgrupamentoId == agr.Id).ToList();
                var schoolIds = schoolsInAgr.Select(s => s.Id).ToList();
                
                var ticketCount = await _context.Tickets.CountAsync(t => t.SchoolId.HasValue && schoolIds.Contains(t.SchoolId.Value));
                var contractCount = await _context.Contratos.CountAsync(c => c.AgrupamentoId == agr.Id);

                var clientVm = new ClientViewModel
                {
                    Agrupamento = agr,
                    Abbreviation = GetAbbreviation(agr.Name),
                    DirectorName = director?.User?.Username ?? "Sem Diretor",
                    DirectorUserId = director?.UserId ?? 0,
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

            // Fetch all users that belong to the 'Diretor' role/table
            AvailableDirectors = await _context.Diretores
                .Include(d => d.User)
                .Select(d => d.User)
                .Where(u => u != null)
                .Distinct()
                .ToListAsync();

            // Fetch all users that belong to the 'Coordenador' role/table
            AvailableCoordenadores = await _context.Coordenadores
                .Include(c => c.User)
                .Select(c => c.User)
                .Where(u => u != null)
                .Distinct()
                .ToListAsync();

            // Fetch all users that belong to the 'Professor' role/table
            AvailableProfessores = await _context.Professores
                .Include(p => p.User)
                .Select(p => p.User)
                .Where(u => u != null)
                .Distinct()
                .ToListAsync();

            // Fetch Individual Clients (Users not in admin/director/coord/prof)
            var exclDir = await _context.Diretores.Select(d => d.UserId).ToListAsync();
            var exclCoo = await _context.Coordenadores.Select(c => c.UserId).ToListAsync();
            var exclPro = await _context.Professores.Select(p => p.UserId).ToListAsync();
            var exclAdm = await _context.Administradores.Select(a => a.UserId).ToListAsync();
            var excludedIds = exclDir.Union(exclCoo).Union(exclPro).Union(exclAdm).ToList();
            
            AvailableIndividualClients = await _context.Users
                .Where(u => !excludedIds.Contains(u.Id))
                .OrderBy(u => u.Username)
                .ToListAsync();

            // Fetch Empresas and their details
            var allEmpresas = await _context.Empresas.ToListAsync();
            foreach (var emp in allEmpresas)
            {
                var responsibles = await _context.Users
                    .Where(u => u.EmpresaId == emp.Id)
                    .Where(u => _context.Administradores.Any(a => a.UserId == u.Id))
                    .ToListAsync();

                var ticketCount = await _context.Tickets
                    .CountAsync(t => (t.EquipamentoId.HasValue && _context.Equipamentos.Any(e => e.Id == t.EquipamentoId.Value && e.EmpresaId == emp.Id)));

                var contractCount = await _context.Contratos.CountAsync(c => c.EmpresaId == emp.Id);
                var equipments = await _context.Equipamentos.Where(e => e.EmpresaId == emp.Id).ToListAsync();

                var indClientUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmpresaId == emp.Id && !_context.Administradores.Any(a => a.UserId == u.Id));

                if (indClientUser != null)
                {
                    responsibles.Add(indClientUser);
                }

                EmpresasInfra.Add(new EmpresaViewModel
                {
                    Empresa = emp,
                    Responsibles = responsibles,
                    TicketCount = ticketCount,
                    ContractCount = contractCount,
                    Equipments = equipments,
                    Abbreviation = GetAbbreviation(emp.Name),
                    IndividualClientId = indClientUser?.Id
                });
            }

            return Page();
        }

        private string GetAbbreviation(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "N/A";
            var words = name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return name.Substring(0, Math.Min(3, name.Length)).ToUpper();
            
            var abbr = "";
            foreach (var word in words)
            {
                if (word.Length > 2) abbr += word[0];
            }
            return abbr.ToUpper();
        }

        public async Task<IActionResult> OnPostAddEstabelecimentoAsync()
        {
            if (string.IsNullOrEmpty(NewEstabName) || string.IsNullOrEmpty(NewEstabType))
            {
                TempData["ErrorMessage"] = "Nome e tipo são obrigatórios.";
                return RedirectToPage();
            }

            try
            {
                switch (NewEstabType)
                {
                    case "Agrupamento":
                        _context.Agrupamentos.Add(new Agrupamento { Name = NewEstabName });
                        break;
                    case "Escola":
                        var newSchool = new AspnetCoreStarter.Models.School 
                        { 
                            Name = NewEstabName, 
                            AgrupamentoId = NewEstabParentId, 
                            Address = string.IsNullOrWhiteSpace(NewEstabAddress) ? "N/A" : NewEstabAddress 
                        };
                        _context.Schools.Add(newSchool);
                        await _context.SaveChangesAsync();

                        if (NewEstabCoordinatorId.HasValue && NewEstabCoordinatorId.Value > 0)
                        {
                            var coordinatorRecord = await _context.Coordenadores.FirstOrDefaultAsync(c => c.UserId == NewEstabCoordinatorId.Value);
                            if (coordinatorRecord != null)
                            {
                                coordinatorRecord.SchoolId = newSchool.Id;
                                _context.Coordenadores.Update(coordinatorRecord);
                            }
                        }
                        break;
                    case "Bloco":
                        _context.Blocos.Add(new Bloco { Name = NewEstabName, SchoolId = NewEstabParentId ?? 0 });
                        break;
                    case "Sala":
                        var newSala = new Sala { Name = NewEstabName, BlockId = NewEstabParentId ?? 0 };
                        if (NewEstabProfessorId.HasValue && NewEstabProfessorId.Value > 0)
                        {
                            newSala.ResponsibleProfessorId = NewEstabProfessorId.Value;
                        }
                        _context.Salas.Add(newSala);
                        break;
                    case "Empresa":
                        var newEmpresa = new Empresa { 
                            Name = NewEstabName,
                            Location = string.IsNullOrWhiteSpace(NewEstabAddress) ? null : NewEstabAddress
                        };
                        _context.Empresas.Add(newEmpresa);
                        await _context.SaveChangesAsync();

                        if (NewEstabIndividualClientId.HasValue && NewEstabIndividualClientId.Value > 0)
                        {
                            var clientUser = await _context.Users.FindAsync(NewEstabIndividualClientId.Value);
                            if (clientUser != null)
                            {
                                clientUser.EmpresaId = newEmpresa.Id;
                            }
                        }
                        break;
                    default:
                        TempData["ErrorMessage"] = "Tipo de estabelecimento inválido.";
                        return RedirectToPage();
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Estabelecimento criado com sucesso.";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Erro ao criar estabelecimento: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteEmpresaAsync(int id)
        {
            try
            {
                var item = await _context.Empresas.FindAsync(id);
                if (item != null)
                {
                    _context.Empresas.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Empresa removida com sucesso.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Não é possível remover esta empresa pois existem utilizadores ou dados vinculados a ela.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAgrupamentoAsync(int id)
        {
            try
            {
                var item = await _context.Agrupamentos.FindAsync(id);
                if (item != null)
                {
                    _context.Agrupamentos.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Agrupamento removido com sucesso.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Não é possível remover este agrupamento pois existem escolas ou diretores vinculados a ele.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSchoolAsync(int id)
        {
            try
            {
                var item = await _context.Schools.FindAsync(id);
                if (item != null)
                {
                    _context.Schools.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Escola removida com sucesso.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Não é possível remover esta escola pois existem blocos ou coordenadores vinculados a ela.";
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

        // --- Edit Handlers ---
        public async Task<IActionResult> OnPostEditAgrupamentoAsync()
        {
            if (!ModelState.IsValid) 
            {
                TempData["ErrorMessage"] = "Dados inválidos no formulário.";
                return RedirectToPage();
            }

            var agrupamento = await _context.Agrupamentos.FindAsync(EditId);
            if (agrupamento != null && !string.IsNullOrEmpty(EditName))
            {
                agrupamento.Name = EditName.Trim();
                
                try 
                {
                    // 1. Remove previous director link for this agrupamento
                    var currentLinks = await _context.Diretores.Where(d => d.AgrupamentoId == EditId).ToListAsync();
                    foreach (var link in currentLinks)
                    {
                        link.AgrupamentoId = null;
                    }

                    // 2. Assign new director if selected
                    if (EditParentId.HasValue && EditParentId.Value > 0)
                    {
                        var newDirector = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == EditParentId.Value);
                        if (newDirector != null)
                        {
                            newDirector.AgrupamentoId = EditId;
                        }
                    }

                    // 3. Optionally update escola name and coordinator
                    if (EditSchoolId.HasValue && EditSchoolId.Value > 0)
                    {
                        var school = await _context.Schools.FindAsync(EditSchoolId.Value);
                        if (school != null)
                        {
                            if (!string.IsNullOrWhiteSpace(EditSchoolName))
                                school.Name = EditSchoolName.Trim();

                            // Remove previous coordinator link for this school
                            var schoolCoordLinks = await _context.Coordenadores
                                .Where(c => c.SchoolId == EditSchoolId.Value).ToListAsync();
                            foreach (var lnk in schoolCoordLinks) lnk.SchoolId = null;

                            // Assign new coordinator if selected
                            if (EditCoordinatorId.HasValue && EditCoordinatorId.Value > 0)
                            {
                                var newCoord = await _context.Coordenadores
                                    .FirstOrDefaultAsync(c => c.UserId == EditCoordinatorId.Value);
                                if (newCoord != null) newCoord.SchoolId = EditSchoolId.Value;
                            }

                            // 4. Optionally update Bloco
                            if (EditBlocoId.HasValue && EditBlocoId.Value > 0)
                            {
                                var bloco = await _context.Blocos.FindAsync(EditBlocoId.Value);
                                if (bloco != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(EditBlocoName))
                                        bloco.Name = EditBlocoName.Trim();

                                    // 5. Optionally update Sala
                                    if (EditSalaId.HasValue && EditSalaId.Value > 0)
                                    {
                                        var sala = await _context.Salas.FindAsync(EditSalaId.Value);
                                        if (sala != null)
                                        {
                                            if (!string.IsNullOrWhiteSpace(EditSalaName))
                                                sala.Name = EditSalaName.Trim();
                                            
                                            sala.ResponsibleProfessorId = (EditResponsibleProfessorId.HasValue && EditResponsibleProfessorId.Value > 0) 
                                                ? EditResponsibleProfessorId.Value 
                                                : null;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"O Agrupamento '{agrupamento.Name}' foi atualizado com sucesso.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao atualizar agrupamento: {ex.Message}";
                }
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
                try 
                {
                    // 1. Remove previous coordinator link for this school
                    var currentLinks = await _context.Coordenadores.Where(c => c.SchoolId == EditId).ToListAsync();
                    foreach (var link in currentLinks)
                    {
                        link.SchoolId = null;
                    }

                    // 2. Assign new coordinator if selected
                    if (EditCoordinatorId.HasValue && EditCoordinatorId.Value > 0)
                    {
                        var newCoordinator = await _context.Coordenadores.FirstOrDefaultAsync(c => c.UserId == EditCoordinatorId.Value);
                        if (newCoordinator != null)
                        {
                            newCoordinator.SchoolId = EditId;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"A Escola '{item.Name}' foi atualizada com sucesso.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao atualizar escola: {ex.Message}";
                }
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
                try 
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"O Bloco '{item.Name}' foi atualizado com sucesso.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao atualizar bloco: {ex.Message}";
                }
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
                try 
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"A Sala '{item.Name}' foi atualizada com sucesso.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao atualizar sala: {ex.Message}";
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditDirectorAsync()
        {
            var user = await _context.Users.FindAsync(EditId);
            if (user != null && !string.IsNullOrEmpty(EditName))
            {
                user.Username = EditName;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditEmpresaAsync()
        {
            var item = await _context.Empresas.FindAsync(EditId);
            if (item != null && !string.IsNullOrEmpty(EditName))
            {
                item.Name = EditName;
                item.Location = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress;
                try 
                {
                    // Unlink previous individual clients if we are explicitly assigning a new one
                    if (EditIndividualClientId.HasValue)
                    {
                        var currentClients = await _context.Users.Where(u => u.EmpresaId == item.Id).ToListAsync();
                        foreach(var c in currentClients) 
                        {
                            // Avoid unlinking Admins, only unlink generic Users
                            bool isAdmin = await _context.Administradores.AnyAsync(a => a.UserId == c.Id);
                            if(!isAdmin) c.EmpresaId = null;
                        }

                        if(EditIndividualClientId.Value > 0)
                        {
                            var newClient = await _context.Users.FindAsync(EditIndividualClientId.Value);
                            if(newClient != null) newClient.EmpresaId = item.Id;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"A Empresa '{item.Name}' foi atualizada com sucesso.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao atualizar empresa: {ex.Message}";
                }
            }
            return RedirectToPage();
        }
    }
}
