using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AspnetCoreStarter.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public User UserProfile { get; set; }

        [BindProperty]
        public string? NewPassword { get; set; }

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        [BindProperty]
        public IFormFile? ProfilePhoto { get; set; }

        [BindProperty]
        public IFormFile? CoverPhoto { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsOwnProfile { get; set; } = true;
        public string ProfileRole { get; set; } = "Membro";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // Ensure database is up to date
            try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE utilizadores ADD COLUMN cover_photo_path VARCHAR(255) NULL;"); } catch { }

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return RedirectToPage("/Auth/Login");
            }

            int targetUserId = id ?? currentUserId;
            IsOwnProfile = (targetUserId == currentUserId);

            UserProfile = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (UserProfile == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Identify role
            if (await _context.Administradores.AnyAsync(a => a.UserId == targetUserId)) ProfileRole = "Admin";
            else if (await _context.Diretores.AnyAsync(d => d.UserId == targetUserId)) ProfileRole = "Diretor";
            else if (await _context.Tecnicos.AnyAsync(t => t.UserId == targetUserId)) ProfileRole = "Tecnico";
            else if (await _context.Coordenadores.AnyAsync(c => c.UserId == targetUserId)) ProfileRole = "Coordenador";
            else if (await _context.Professores.AnyAsync(p => p.UserId == targetUserId)) ProfileRole = "Professor";
            else if (UserProfile.EmpresaId.HasValue) ProfileRole = "Cliente Privado";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Update basic info
            user.Username = UserProfile.Username;
            user.Email = UserProfile.Email;

            // Handle Profile Photo
            if (ProfilePhoto != null && ProfilePhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + ProfilePhoto.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePhoto.CopyToAsync(stream);
                }
                user.ProfilePhotoPath = "/uploads/profiles/" + uniqueFileName;
            }

            // Handle Cover Photo
            if (CoverPhoto != null && CoverPhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "covers");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + CoverPhoto.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await CoverPhoto.CopyToAsync(stream);
                }
                user.CoverPhotoPath = "/uploads/covers/" + uniqueFileName;
            }

            // Handle Password Change
            if (!string.IsNullOrEmpty(NewPassword))
            {
                if (NewPassword != ConfirmPassword)
                {
                    ErrorMessage = "As palavras-passe não coincidem.";
                    UserProfile = user;
                    return Page();
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                user.Password = hashedPassword;
                user.PasswordHash = hashedPassword;
            }

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Refresh claims to update navbar/session info immediately
                var role = User.FindFirstValue(ClaimTypes.Role) ?? "Membro";
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
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                SuccessMessage = "Perfil atualizado com sucesso!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erro ao atualizar perfil: {ex.Message}";
            }

            UserProfile = user;
            return Page();
        }
    }
}
