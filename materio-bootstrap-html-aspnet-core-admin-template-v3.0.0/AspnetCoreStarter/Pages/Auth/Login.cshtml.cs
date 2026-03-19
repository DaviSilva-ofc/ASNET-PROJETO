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
                // Procurar por email ou username
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email || u.Username == Email);

                if (user == null)
                {
                    ErrorMessage = "Utilizador não encontrado na base de dados.";
                    return Page();
                }

                if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
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
