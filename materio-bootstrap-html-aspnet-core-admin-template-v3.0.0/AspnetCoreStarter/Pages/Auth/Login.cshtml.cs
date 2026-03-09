using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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
        [Required(ErrorMessage = "A palavra-passe é obrigatória")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

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

            // Procurar por email ou username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email || u.Username == Email);

            if (user != null && BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                // Login com sucesso
                // Por agora vamos usar Sessão como configurado no Program.cs
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
                {
                    HttpContext.Session.SetString("UserProfilePhoto", user.ProfilePhotoPath);
                }

                if (user.Role == "Admin")
                {
                    return RedirectToPage("/Admin/Dashboard");
                }

                return RedirectToPage("/frontpages/LandingPage");
            }

            ErrorMessage = "Email ou palavra-passe incorretos.";
            return Page();
        }
    }
}
