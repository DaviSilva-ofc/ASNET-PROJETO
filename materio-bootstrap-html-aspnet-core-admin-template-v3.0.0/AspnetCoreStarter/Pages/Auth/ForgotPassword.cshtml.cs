using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Auth
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly AppDbContext _context;

        public ForgotPasswordModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }

        public string Message { get; set; }
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);

            if (user != null)
            {
                // Gerar token de recuperação
                user.PasswordResetToken = Guid.NewGuid().ToString();
                user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                
                await _context.SaveChangesAsync();

                // Simular envio de email mostrando o link
                var resetLink = $"{Request.Scheme}://{Request.Host}/Auth/ResetPassword?token={user.PasswordResetToken}";
                Message = $"Link de recuperação gerado (Simulação de Email): <a href='{resetLink}'>Clique aqui para definir nova senha</a>";
                ErrorMessage = null; // Limpar erro anterior se houver sucesso
            }
            else
            {
                ErrorMessage = "Este email não está registado na aplicação.";
                Message = null; // Limpar mensagem de sucesso anterior
            }

            return Page();
        }
    }
}
