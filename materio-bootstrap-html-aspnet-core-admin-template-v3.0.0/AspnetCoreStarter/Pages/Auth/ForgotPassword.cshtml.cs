using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Auth
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public ForgotPasswordModel(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

                // Enviar email real
                var resetLink = $"{Request.Scheme}://{Request.Host}/Auth/ResetPassword?token={user.PasswordResetToken}";
                var emailBody = $@"
                    <h2>Recuperação de Palavra-passe</h2>
                    <p>Olá,</p>
                    <p>Recebemos um pedido para alterar a senha da sua conta ASNET.</p>
                    <p>Clique no link abaixo para definir uma nova senha:</p>
                    <p><a href='{resetLink}'>{resetLink}</a></p>
                    <br>
                    <p>Se não solicitou esta alteração, por favor ignore este email.</p>";

                try
                {
                    await _emailService.SendEmailAsync(user.Email, "Recuperação de Palavra-passe - ASNET", emailBody);
                    Message = "Enviámos um link de recuperação para o seu email. Por favor, verifique a sua caixa de entrada.";
                    ErrorMessage = null;
                }
                catch (System.Exception)
                {
                    ErrorMessage = "Houve um erro ao tentar enviar o email. Por favor, tente novamente mais tarde ou contacte o suporte.";
                    Message = null;
                }
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
