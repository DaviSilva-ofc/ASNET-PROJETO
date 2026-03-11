using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.IO;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RegisterModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public bool Terms { get; set; }
            public IFormFile ProfilePhoto { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Input.Terms)
            {
                ErrorMessage = "Deve aceitar os termos e condições.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.Username) || string.IsNullOrWhiteSpace(Input.Email) || string.IsNullOrWhiteSpace(Input.Password))
            {
                ErrorMessage = "Preencha todos os campos obrigatórios.";
                return Page();
            }

            // Guardar foto de perfil se existir
            string photoPath = null;
            if (Input.ProfilePhoto != null && Input.ProfilePhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.ProfilePhoto.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfilePhoto.CopyToAsync(stream);
                }
                photoPath = "/uploads/profiles/" + uniqueFileName;
            }

            // Criar utilizador
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(Input.Password);
            var user = new User
            {
                Username = Input.Username,
                Email = Input.Email,
                Password = hashedPassword, // Map to 'palavra_passe'
                PasswordHash = hashedPassword, // Map to 'PasswordHash'
                ProfilePhotoPath = photoPath
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToPage("/Auth/Login");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                ErrorMessage = $"Erro ao gravar na base de dados: {dbEx.InnerException?.Message ?? dbEx.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ocorreu um erro inesperado: {ex.Message}";
                return Page();
            }
        }
    }
}
