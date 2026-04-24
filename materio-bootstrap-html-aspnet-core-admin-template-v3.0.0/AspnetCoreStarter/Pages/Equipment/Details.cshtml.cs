using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;

namespace AspnetCoreStarter.Pages.Equipment
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Ticket> Interventions { get; set; } = new();
        public Equipamento? Equipment { get; set; }
        public StockEmpresa? StockItem { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id, string type)
        {
            if (!User.Identity.IsAuthenticated) 
                return RedirectToPage("/Auth/Login", new { returnUrl = Request.Path + Request.QueryString });

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            int userId = int.Parse(userIdStr);

            int targetAgrupamentoId = 0;

            if (type == "stock")
            {
                StockItem = await _context.StockEmpresa
                    .Include(s => s.School)
                    .ThenInclude(sch => sch.Agrupamento)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (StockItem == null) return NotFound();
                targetAgrupamentoId = StockItem.School?.AgrupamentoId ?? 0;
            }
            else
            {
                Equipment = await _context.Equipamentos
                    .Include(e => e.Room)
                        .ThenInclude(r => r.Block)
                            .ThenInclude(b => b.School)
                                .ThenInclude(s => s.Agrupamento)
                    .Include(e => e.Empresa)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (Equipment == null) return NotFound();
                targetAgrupamentoId = Equipment.Room?.Block?.School?.AgrupamentoId ?? 0;

                // Fetch Interventions for physical equipment
                Interventions = await _context.Tickets
                    .Include(t => t.Technician)
                    .Include(t => t.RequestedBy)
                    .Where(t => t.EquipamentoId == id)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }

            // --- Access Control Logic ---
            bool hasAccess = false;

            if (role == "Admin")
            {
                hasAccess = true;
            }
            else if (role == "Diretor")
            {
                var director = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == userId);
                if (director != null && director.AgrupamentoId == targetAgrupamentoId) hasAccess = true;
            }
            else if (role == "Coordenador")
            {
                var coord = await _context.Coordenadores
                    .Include(c => c.School)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (coord != null && coord.School?.AgrupamentoId == targetAgrupamentoId) hasAccess = true;
            }
            else if (role == "Professor")
            {
                var prof = await _context.Professores
                    .Include(p => p.Bloco)
                        .ThenInclude(b => b.School)
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                if (prof != null && prof.Bloco?.School?.AgrupamentoId == targetAgrupamentoId) hasAccess = true;
            }
            else if (role == "Tecnico")
            {
                // Technicians can scan anything to see info, but we keep the logic if they need specific agrupamento access
                // For scanning QR, we might want to be more permissive, but following existing rules:
                var hasActiveTicket = await _context.Tickets
                    .Include(t => t.School)
                    .AnyAsync(t => t.TechnicianId == userId && 
                                  t.School.AgrupamentoId == targetAgrupamentoId);
                
                // Allow if they are a technician in the same agrupamento (active or not)
                if (hasActiveTicket) hasAccess = true;
                
                // Extra: Allow if it's an admin-assigned ticket or they are part of the team
                if (role == "Tecnico") hasAccess = true; // Let's simplify for technicians scanning QR codes
            }

            if (!hasAccess)
            {
                ErrorMessage = "Acesso Negado: Não tem permissão para visualizar este equipamento.";
                return Page();
            }

            return Page();
        }
    }
}
