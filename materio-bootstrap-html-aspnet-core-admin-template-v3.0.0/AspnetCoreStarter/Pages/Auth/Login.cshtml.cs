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
        [Required(ErrorMessage = "O email/utilizador é obrigatório")]
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
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
                    new Claim(ClaimTypes.Role, role)
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
