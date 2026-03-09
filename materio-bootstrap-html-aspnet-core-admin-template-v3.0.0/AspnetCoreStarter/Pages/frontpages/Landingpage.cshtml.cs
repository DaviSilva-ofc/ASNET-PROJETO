using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.FrontPages
{
    public class LandingPageModel : PageModel
    {
        private readonly IEmailService _emailService;

        public LandingPageModel(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [BindProperty]
        [Required(ErrorMessage = "O nome é obrigatório")]
        public string FullName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string ContactEmail { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "A mensagem é obrigatória")]
        public string MessageBody { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Por favor, preencha todos os campos corretamente.";
                return Page();
            }

            try
            {
                var subject = $"Novo contacto do site - {FullName}";
                var emailContent = $@"
                    <h2>Novo Contacto Recebido</h2>
                    <p><strong>Nome:</strong> {FullName}</p>
                    <p><strong>Email:</strong> {ContactEmail}</p>
                    <p><strong>Mensagem:</strong></p>
                    <p>{MessageBody}</p>";

                // Enviar para o email solicitado pelo USER
                await _emailService.SendEmailAsync("a41710@esas.pt", subject, emailContent);

                SuccessMessage = "A sua mensagem foi enviada com sucesso! Entraremos em contacto brevemente.";
                FullName = string.Empty;
                ContactEmail = string.Empty;
                MessageBody = string.Empty;
                ModelState.Clear();
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Erro ao enviar mensagem: {ex.Message}";
            }

            return Page();
        }
    }
}
