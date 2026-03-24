using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System;
using System.Linq;

namespace AspnetCoreStarter.Pages
{
    public class SeedTechModel : PageModel
    {
        private readonly AppDbContext _context;

        public SeedTechModel(AppDbContext context)
        {
            _context = context;
        }

        public string Result { get; set; }

        public void OnGet()
        {
            if (!_context.Users.Any(u => u.Email == "tecnico@asnet.pt"))
            {
                var techUser = new User
                {
                    Username = "Técnico Silva",
                    Email = "tecnico@asnet.pt",
                    Password = "password123",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    AccountStatus = "Ativo",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(techUser);
                _context.SaveChanges();

                var tech = new Tecnico
                {
                    UserId = techUser.Id,
                    AreaTecnica = "Informática",
                    Nivel = "Senior"
                };
                _context.Tecnicos.Add(tech);
                _context.SaveChanges();

                Result = "Técnico criado com sucesso! Email: tecnico@asnet.pt | Senha: password123";
            }
            else
            {
                Result = "Técnico de teste já existe. Pode usar o email: tecnico@asnet.pt e senha: password123";
            }
        }
    }
}
