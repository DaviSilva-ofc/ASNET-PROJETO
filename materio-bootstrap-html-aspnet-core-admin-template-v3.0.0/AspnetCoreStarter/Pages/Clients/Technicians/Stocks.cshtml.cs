using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class StocksModel : PageModel
    {
        private readonly AppDbContext _context;

        public StocksModel(AppDbContext context)
        {
            _context = context;
        }

        public List<StockEmpresa> MyStock { get; set; } = new();
        public List<School> AccessibleSchools { get; set; } = new();
        public List<StockEmpresa> SchoolBaseStock { get; set; } = new();
        
        public List<Agrupamento> AccessibleAgrupamentos { get; set; } = new();
        public List<StockEmpresa> AgrupamentoBaseStock { get; set; } = new();

        public List<Ticket> ActiveTickets { get; set; } = new();

        public List<Bloco> AccessibleBlocos { get; set; } = new();
        public List<Sala> AccessibleSalas { get; set; } = new();

        public HashSet<int> ActiveSchoolIds { get; set; } = new();
        public HashSet<int> ActiveAgrupamentoIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var userIdStr = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            // 1. Técnico's directly assigned stock
            MyStock = await _context.StockEmpresa
                .Where(s => s.TechnicianId == userId && s.AgrupamentoId == null && s.SchoolId == null)
                .OrderBy(s => s.EquipmentName)
                .ToListAsync();

            // 2. Determine accessible School & Agrupamento stocks based on active tickets
            var activeTickets = await _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento)
                    .ThenInclude(e => e.Room)
                        .ThenInclude(r => r.Block)
                            .ThenInclude(b => b.School)
                .Where(t => t.TechnicianId == userId && 
                            t.Status != "Concluído" && 
                            t.Level != "Empréstimo" && 
                            t.Level != "Alteração de Estado" && 
                            (t.Level == null || !t.Level.Contains("ltera")) && 
                            (t.Description == null || !t.Description.Contains("PEDIDO DE ALTERA")) && 
                            (t.Level == null || !t.Level.Contains("Estado")))
                .ToListAsync();

            ActiveTickets = activeTickets;

            ActiveSchoolIds = new HashSet<int>();
            ActiveAgrupamentoIds = new HashSet<int>();

            var debugTickets = new List<dynamic>();

            foreach (var t in activeTickets)
            {
                int? directSchoolId = t.SchoolId;
                int? derivedSchoolId = (t.Equipamento != null && t.Equipamento.Room != null && t.Equipamento.Room.Block != null) 
                                       ? t.Equipamento.Room.Block.SchoolId 
                                       : (int?)null;

                debugTickets.Add(new {
                    Id = t.Id,
                    Status = t.Status,
                    DirectSchoolId = directSchoolId,
                    DerivedSchoolId = derivedSchoolId
                });

                if (t.SchoolId.HasValue)
                {
                    ActiveSchoolIds.Add(t.SchoolId.Value);
                    if (t.School?.AgrupamentoId != null) ActiveAgrupamentoIds.Add(t.School.AgrupamentoId.Value);
                }
                else if (t.Equipamento != null && t.Equipamento.Room != null && t.Equipamento.Room.Block != null)
                {
                    ActiveSchoolIds.Add(t.Equipamento.Room.Block.SchoolId);
                    if (t.Equipamento.Room.Block.School?.AgrupamentoId != null)
                    {
                        ActiveAgrupamentoIds.Add(t.Equipamento.Room.Block.School.AgrupamentoId.Value);
                    }
                }
            }

            ViewData["DebugTickets"] = debugTickets;
            ViewData["DebugSchoolIds"] = ActiveSchoolIds.ToList();
            ViewData["DebugAgrupIds"] = ActiveAgrupamentoIds.ToList();

            // Load parallel flat lists because navigation collections like 'School.Blocos' don't exist
            if (ActiveSchoolIds.Any())
            {
                SchoolBaseStock = await _context.StockEmpresa
                    .Include(s => s.School)
                    .Where(s => s.SchoolId.HasValue && ActiveSchoolIds.Contains(s.SchoolId.Value))
                    .ToListAsync();
                
                AccessibleSchools = await _context.Schools
                    .Where(s => ActiveSchoolIds.Contains(s.Id))
                    .ToListAsync();
            }

            if (ActiveAgrupamentoIds.Any())
            {
                AgrupamentoBaseStock = await _context.StockEmpresa
                    .Include(s => s.Agrupamento)
                    .Where(s => s.AgrupamentoId.HasValue && ActiveAgrupamentoIds.Contains(s.AgrupamentoId.Value))
                    .ToListAsync();
                
                AccessibleAgrupamentos = await _context.Agrupamentos
                    .Where(a => ActiveAgrupamentoIds.Contains(a.Id))
                    .ToListAsync();

                // Merge schools from active groupings for the blocks/rooms query
                var schoolsFromGroups = await _context.Schools
                    .Where(s => s.AgrupamentoId.HasValue && ActiveAgrupamentoIds.Contains(s.AgrupamentoId.Value))
                    .ToListAsync();
                AccessibleSchools.AddRange(schoolsFromGroups);
                AccessibleSchools = AccessibleSchools.DistinctBy(s => s.Id).ToList();
            }

            var activeAndAgrupSchoolIds = AccessibleSchools.Select(s => s.Id).ToList();
            if (activeAndAgrupSchoolIds.Any())
            {
                AccessibleBlocos = await _context.Blocos
                    .Where(b => activeAndAgrupSchoolIds.Contains(b.SchoolId))
                    .ToListAsync();

                var blocoIds = AccessibleBlocos.Select(b => b.Id).ToList();
                if (blocoIds.Any())
                {
                    AccessibleSalas = await _context.Salas
                        .Include(s => s.Equipments)
                        .Where(s => s.BlockId.HasValue && blocoIds.Contains(s.BlockId.Value))
                        .ToListAsync();
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAssignStockAsync(int stockId, int ticketId, string itemType = "Stock")
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var userIdStr = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null || ticket.TechnicianId != userId)
            {
                TempData["ErrorMessage"] = "Trabalho não encontrado ou sem permissão.";
                return RedirectToPage();
            }

            if (itemType == "Equipamento")
            {
                var equip = await _context.Equipamentos.FindAsync(stockId);
                if (equip == null)
                {
                    TempData["ErrorMessage"] = "Equipamento não encontrado.";
                    return RedirectToPage();
                }
                equip.TicketId = ticketId;
                equip.Status = "Usado na Intervenção";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"O equipamento '{equip.Name}' foi associado com sucesso ao Trabalho #{ticketId}.";
            }
            else
            {
                var stock = await _context.StockEmpresa.FindAsync(stockId);
                if (stock == null)
                {
                    TempData["ErrorMessage"] = "Item de stock não encontrado.";
                    return RedirectToPage();
                }

                stock.TicketId = ticketId;
                stock.Status = "Usado na Intervenção";
                stock.IsAvailable = false;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"O artigo '{stock.EquipmentName}' foi associado com sucesso ao Trabalho #{ticketId}.";
            }

            return RedirectToPage();
        }
    }

    public class TechnicianStockViewModel
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? LocationName { get; set; }
        public string? Source { get; set; }
        public string? Status { get; set; }
    }
}
