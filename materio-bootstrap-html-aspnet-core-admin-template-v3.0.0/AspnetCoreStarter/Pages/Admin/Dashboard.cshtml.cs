using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public List<AspnetCoreStarter.Models.User> PendingUsers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSchools { get; set; }
        public int TotalEquipments { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verificação de segurança via Cookie Authentication
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || userRole != "Admin") 
                return RedirectToPage("/Index");

            PendingUsers = await _context.Users
                .Where(u => u.AccountStatus == "Pendente")
                .ToListAsync();

            TotalUsers = await _context.Users.CountAsync();
            TotalSchools = await _context.Schools.CountAsync();
            TotalEquipments = await _context.Equipamentos.CountAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var userFound = await _context.Users.FindAsync(id);
            if (userFound != null)
            {
                userFound.AccountStatus = "Ativo";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var userFound = await _context.Users.FindAsync(id);
            if (userFound != null)
            {
                userFound.AccountStatus = "Rejeitado";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
