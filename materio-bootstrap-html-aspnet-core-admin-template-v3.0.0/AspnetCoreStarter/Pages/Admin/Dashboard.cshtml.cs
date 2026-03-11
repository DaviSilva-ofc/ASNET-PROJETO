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
        public int TotalSalas { get; set; }
        public int TotalBlocos { get; set; }
        public int TotalAgrupamentos { get; set; }

        // Infrastructure tree data
        public List<Agrupamento> Agrupamentos { get; set; }
        public List<AspnetCoreStarter.Models.School> AllSchools { get; set; }
        public List<Bloco> AllBlocos { get; set; }
        public List<Sala> AllSalas { get; set; }

        // Chart data — per-school equipment counts
        public List<string> SchoolNames { get; set; }
        public List<int> SchoolEquipmentCounts { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
                return RedirectToPage("/Index");

            // Pending users
            PendingUsers = await _context.Users
                .Where(u => u.AccountStatus == "Pendente")
                .ToListAsync();

            // Totals
            TotalUsers = await _context.Users.CountAsync();
            TotalSchools = await _context.Schools.CountAsync();
            TotalEquipments = await _context.Equipamentos.CountAsync();
            TotalSalas = await _context.Salas.CountAsync();
            TotalBlocos = await _context.Blocos.CountAsync();
            TotalAgrupamentos = await _context.Agrupamentos.CountAsync();

            // Infrastructure tree data
            Agrupamentos = await _context.Agrupamentos.ToListAsync();
            AllSchools = await _context.Schools.Include(s => s.Agrupamento).ToListAsync();
            AllBlocos = await _context.Blocos.Include(b => b.School).ToListAsync();
            AllSalas = await _context.Salas.Include(s => s.Block).ToListAsync();

            // Per-school equipment counts for bar chart
            SchoolNames = new List<string>();
            SchoolEquipmentCounts = new List<int>();

            var schools = await _context.Schools.ToListAsync();
            foreach (var school in schools)
            {
                SchoolNames.Add(school.Name ?? "Sem Nome");
                var blocoIds = await _context.Blocos
                    .Where(b => b.SchoolId == school.Id)
                    .Select(b => b.Id)
                    .ToListAsync();
                var salaIds = await _context.Salas
                    .Where(s => blocoIds.Contains(s.BlockId))
                    .Select(s => s.Id)
                    .ToListAsync();
                var equipCount = await _context.Equipamentos
                    .Where(e => salaIds.Contains(e.RoomId))
                    .CountAsync();
                SchoolEquipmentCounts.Add(equipCount);
            }

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
