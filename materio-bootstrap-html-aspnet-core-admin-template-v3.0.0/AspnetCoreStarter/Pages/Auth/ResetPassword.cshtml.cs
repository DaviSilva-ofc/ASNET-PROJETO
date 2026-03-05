using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Auth
{
    public class ResetPasswordModel : PageModel
    {
        private readonly AppDbContext _context;

        public ResetPasswordModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Token { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "A nova palavra-passe é obrigatória")]
        [MinLength(6, ErrorMessage = "A palavra-passe deve ter pelo menos 6 caracteres")]
        public string NewPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "A confirmação da palavra-passe é obrigatória")]
        [Compare("NewPassword", ErrorMessage = "As palavras-passe não coincidem")]
        public string ConfirmPassword { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            Token = token;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == Token && u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                ErrorMessage = "O link de recuperação é inválido ou expirou.";
                return Page();
            }

            // Atualizar senha (usando BCrypt como no Register)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            
            // Limpar token
            user.PasswordResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Auth/Login");
        }
    }
}
