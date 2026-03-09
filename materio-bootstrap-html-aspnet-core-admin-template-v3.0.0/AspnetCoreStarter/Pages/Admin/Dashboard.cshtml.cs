using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public List<User> PendingUsers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSchools { get; set; }
        public int TotalEquipments { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verificação básica de segurança
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Auth/Login");

            var admin = await _context.Users.FindAsync(int.Parse(userId));
            if (admin == null || admin.Role != "Admin") return RedirectToPage("/Index");

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
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.AccountStatus = "Ativo";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.AccountStatus = "Rejeitado";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
