using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using System;

namespace AspnetCoreStarter.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty]
        public bool RememberMe { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnGetExternalLogin(string provider)
        {
            try
            {
                var configKey = provider == "Google" ? "Authentication:Google:ClientId" : "Authentication:Microsoft:ClientId";
                var configValue = ((IConfiguration)HttpContext.RequestServices.GetService(typeof(IConfiguration)))[configKey];

                if (string.IsNullOrEmpty(configValue) || configValue.Contains("SEU_") || configValue.Contains("YOUR_"))
                {
                    ErrorMessage = $"Configuração necessária: Por favor, insira o Client ID do {provider} no arquivo appsettings.json para ativar esta funcionalidade.";
                    return Page();
                }

                var redirectUrl = Url.Page("./Login", pageHandler: "ExternalLoginCallback");
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
                return Challenge(properties, provider);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erro ao iniciar login externo ({provider}): {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnGetExternalLoginCallbackAsync()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync("External");
            if (!authenticateResult.Succeeded)
            {
                return RedirectToPage("./Login");
            }

            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                ErrorMessage = "Não foi possível obter o email da conta externa.";
                return Page();
            }

            // Ensure database is up to date
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE utilizadores ADD COLUMN cover_photo_path VARCHAR(255) NULL;"); } catch { }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ErrorMessage = $"O email '{email}' não está registado no sistema. Por favor, registe-se primeiro ou contacte o administrador.";
                await HttpContext.SignOutAsync("External");
                return Page();
            }

            // Reuse the login logic to identify role
            var isAdmin = await _context.Administradores.AnyAsync(a => a.UserId == user.Id);
            string role = "Ativo";
            if (isAdmin) role = "Admin";
            else if (await _context.Diretores.AnyAsync(d => d.UserId == user.Id)) role = "Diretor";
            else if (await _context.Tecnicos.AnyAsync(t => t.UserId == user.Id)) role = "Tecnico";
            else if (await _context.Coordenadores.AnyAsync(c => c.UserId == user.Id)) role = "Coordenador";
            else if (await _context.Professores.AnyAsync(p => p.UserId == user.Id)) role = "Professor";
            else if (user.EmpresaId.HasValue) role = "ClientePrivado";

            if (role != "Admin" && !string.Equals(user.AccountStatus, "Ativo", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = $"A sua conta está com o estado '{user.AccountStatus}'. Contacte o administrador.";
                await HttpContext.SignOutAsync("External");
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, role),
                new Claim("ProfilePhoto", user.ProfilePhotoPath ?? "/img/avatars/1.png")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in to the main scheme and clean up external scheme
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            await HttpContext.SignOutAsync("External");

            // Session compatibility
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);

            // Redirect logic
            if (role == "Admin") return RedirectToPage("/Admin/Dashboard");
            if (role == "Diretor") return RedirectToPage("/Clients/Directors/Dashboard");
            if (role == "Tecnico") return RedirectToPage("/Clients/Technicians/Dashboard");
            if (role == "Coordenador") return RedirectToPage("/Clients/Coordinators/Dashboard");
            if (role == "Professor") return RedirectToPage("/Clients/Professors/Dashboard");
            if (role == "ClientePrivado") return RedirectToPage("/Clients/Private/Dashboard");

            return RedirectToPage("/frontpages/LandingPage");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "O email/utilizador e a palavra-passe são obrigatórios.";
                return Page();
            }

            try
            {
                // Temporary fix for missing tables and columns in MySQL
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS contratos (
                            id_contrato INT AUTO_INCREMENT PRIMARY KEY,
                            periodo VARCHAR(50),
                            tipo_contrato VARCHAR(50),
                            status_contrato VARCHAR(50),
                            descricao TEXT,
                            id_agrupamento INT,
                            id_admin INT,
                            FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento),
                            FOREIGN KEY (id_admin) REFERENCES utilizadores(id_utilizador)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS diretores (
                            id_diretores INT AUTO_INCREMENT PRIMARY KEY,
                            id_utilizador INT,
                            id_agrupamento INT,
                            FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador),
                            FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS coordenadores (
                            id_coordenadores INT AUTO_INCREMENT PRIMARY KEY,
                            id_utilizador INT,
                            id_escola INT,
                            FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador),
                            FOREIGN KEY (id_escola) REFERENCES escolas(id_escola)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS professores (
                            id_professores INT AUTO_INCREMENT PRIMARY KEY,
                            id_utilizador INT,
                            id_bloco INT,
                            FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador),
                            FOREIGN KEY (id_bloco) REFERENCES blocos(id_bloco)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS tecnicos (
                            id_tecnico INT AUTO_INCREMENT PRIMARY KEY,
                            id_utilizador INT,
                            especialidade VARCHAR(100),
                            FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS administradores (
                            id_utilizador INT PRIMARY KEY,
                            id_agrupamento INT,
                            FOREIGN KEY (id_utilizador) REFERENCES utilizadores(id_utilizador),
                            FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS stock_empresa (
                            id_stock INT AUTO_INCREMENT PRIMARY KEY,
                            nome_equipamento VARCHAR(100),
                            tipo VARCHAR(100),
                            descricao TEXT,
                            disponivel BOOLEAN DEFAULT TRUE,
                            id_tecnico INT,
                            id_admin INT,
                            FOREIGN KEY (id_admin) REFERENCES utilizadores(id_utilizador)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS stock_tecnico (
                            id_stock_tecnico INT AUTO_INCREMENT PRIMARY KEY,
                            id_tecnico INT,
                            nome_equipamento VARCHAR(100),
                            descricao TEXT,
                            disponivel BOOLEAN DEFAULT TRUE,
                            FOREIGN KEY (id_tecnico) REFERENCES tecnicos(id_utilizador)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS mensagens (
                            id_mensagem INT AUTO_INCREMENT PRIMARY KEY,
                            id_remetente INT,
                            id_destinatario INT,
                            conteudo TEXT,
                            data_envio DATETIME DEFAULT CURRENT_TIMESTAMP,
                            lida BOOLEAN DEFAULT FALSE,
                            FOREIGN KEY (id_remetente) REFERENCES utilizadores(id_utilizador),
                            FOREIGN KEY (id_destinatario) REFERENCES utilizadores(id_utilizador)
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE utilizadores ADD COLUMN password_hash VARCHAR(255) NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE salas ADD COLUMN id_professor_responsavel INT NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN id_equipamento INT NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN status VARCHAR(50) DEFAULT 'Pedido';"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE equipamentos ADD COLUMN status VARCHAR(50) DEFAULT 'Funcionando';"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("UPDATE utilizadores SET status_conta = 'Pendente' WHERE status_conta IS NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN id_agrupamento INT NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN id_escola INT NULL;"); } catch { }
                try { 
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS empresas (
                            id_empresa INT AUTO_INCREMENT PRIMARY KEY,
                            nome_empresa VARCHAR(100) NOT NULL,
                            localizacao VARCHAR(255) NULL
                        ) ENGINE=InnoDB;"); 
                } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE utilizadores ADD COLUMN id_empresa INT NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contratos ADD COLUMN nivel_urgencia VARCHAR(20) NULL;"); } catch { }
                try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE utilizadores ADD COLUMN cover_photo_path VARCHAR(255) NULL;"); } catch { }
                try {
                    await _context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS pedidos_stock (
                            id_pedido INT AUTO_INCREMENT PRIMARY KEY,
                            nome_artigo VARCHAR(100) NOT NULL,
                            tipo_artigo VARCHAR(100),
                            quantidade INT DEFAULT 1,
                            notas TEXT,
                            id_coordenador INT,
                            id_escola INT,
                            id_agrupamento INT,
                            status VARCHAR(50) DEFAULT 'Pendente_Diretor',
                            notas_diretor TEXT,
                            data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP,
                            data_atualizacao DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                            FOREIGN KEY (id_coordenador) REFERENCES utilizadores(id_utilizador),
                            FOREIGN KEY (id_escola) REFERENCES escolas(id_escola),
                            FOREIGN KEY (id_agrupamento) REFERENCES agrupamentos(id_agrupamento)
                        ) ENGINE=InnoDB;");
                } catch { }

                // Procurar por email ou username
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email || u.Username == Email);

                if (user == null)
                {
                    ErrorMessage = "Utilizador não encontrado na base de dados.";
                    return Page();
                }

                // Verify against PasswordHash or the direct Password/palavra_passe field
                var passwordToVerify = string.IsNullOrEmpty(user.PasswordHash) ? user.Password : user.PasswordHash;
                if (!BCrypt.Net.BCrypt.Verify(Password, passwordToVerify))
                {
                    ErrorMessage = "Palavra-passe incorreta.";
                    return Page();
                }

                // Identify the user's role by checking specific tables
                var isAdmin = await _context.Administradores.AnyAsync(a => a.UserId == user.Id);
                string role = "Ativo"; // Default fallback
                if (isAdmin)
                {
                    role = "Admin";
                }
                else if (await _context.Diretores.AnyAsync(d => d.UserId == user.Id))
                {
                    role = "Diretor";
                }
                else if (await _context.Tecnicos.AnyAsync(t => t.UserId == user.Id))
                {
                    role = "Tecnico";
                }
                else if (await _context.Coordenadores.AnyAsync(c => c.UserId == user.Id))
                {
                    role = "Coordenador";
                }
                else if (await _context.Professores.AnyAsync(p => p.UserId == user.Id))
                {
                    role = "Professor";
                }
                else if (user.EmpresaId.HasValue)
                {
                    role = "ClientePrivado";
                }

                Console.WriteLine($"[LOGIN] Utilizador: {user.Username}, Role: {role}, AccountStatus: '{user.AccountStatus}'");

                // Verificar se a conta está ativa (admins podem entrar sempre)
                if (role != "Admin" && !string.Equals(user.AccountStatus, "Ativo", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = $"A sua conta está com o estado '{user.AccountStatus}'. Contacte o administrador.";
                    return Page();
                }

                // Criar claims para autenticação por cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("ProfilePhoto", user.ProfilePhotoPath ?? "/img/avatars/1.png")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(RememberMe ? 30 : 1)
                    });

                // Sessão (para compatibilidade)
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);

                // Redirecionar com base no cargo
                if (role == "Admin")
                {
                    return RedirectToPage("/Admin/Dashboard");
                }
                if (role == "Diretor")
                {
                    return RedirectToPage("/Clients/Directors/Dashboard");
                }
                if (role == "Tecnico")
                {
                    return RedirectToPage("/Clients/Technicians/Dashboard");
                }
                if (role == "Coordenador")
                {
                    return RedirectToPage("/Clients/Coordinators/Dashboard");
                }

                if (role == "Professor")
                {
                    return RedirectToPage("/Clients/Professors/Dashboard");
                }

                if (role == "ClientePrivado")
                {
                    return RedirectToPage("/Clients/Private/Dashboard");
                }

                return RedirectToPage("/frontpages/LandingPage");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erro ao tentar iniciar sessão: {ex.Message}";
                return Page();
            }
        }
    }
}
