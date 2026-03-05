using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Auth
{
    public class RegisterSchoolModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterSchoolModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "O nome da escola é obrigatório")]
            [MaxLength(200)]
            public string Name { get; set; }

            [Required(ErrorMessage = "A morada é obrigatória")]
            [MaxLength(500)]
            public string Address { get; set; }

            [Required(ErrorMessage = "O email de contacto é obrigatório")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string ContactEmail { get; set; }

            [Required(ErrorMessage = "O telefone é obrigatório")]
            [MaxLength(50)]
            public string Phone { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Verificar se a escola já existe (opcional, por nome ou email)
            // Para simplificar, vamos apenas criar

            var school = new School
            {
                Name = Input.Name,
                Address = Input.Address,
                ContactEmail = Input.ContactEmail,
                Phone = Input.Phone
            };

            try
            {
                _context.Schools.Add(school);
                await _context.SaveChangesAsync();

                SuccessMessage = "Escola registada com sucesso! Entraremos em contacto brevemente.";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = "Erro ao registar a escola: " + ex.Message;
            }
            return Page();
        }
    }
}
