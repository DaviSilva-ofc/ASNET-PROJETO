using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

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
        public List<Ticket> MyStockRequests { get; set; } = new();
        public List<RequestStockOption> AvailableRequestItems { get; set; } = new();

        public class RequestStockOption
        {
            public int StockId { get; set; }
            public string Label { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Tecnico")) 
                return RedirectToPage("/Auth/Login");

            var userIdStr = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            // Load items currently with the technician
            MyStock = await _context.StockEmpresa
                .Where(s => s.TechnicianId == userId && s.AgrupamentoId == null && s.SchoolId == null)
                .OrderBy(s => s.EquipmentName)
                .ToListAsync();

            // Load stock requests (tickets of level "Empréstimo" created by this tech)
            // Note: In this system, technicians might not have an AdminId for the ticket, 
            // but we can filter by TechnicianId if we set it upon creation.
            MyStockRequests = await _context.Tickets
                .Where(t => t.TechnicianId == userId && t.Level == "Empréstimo")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            AvailableRequestItems = await _context.StockEmpresa
                .Where(s => s.TechnicianId == null
                            && s.AgrupamentoId == null
                            && s.SchoolId == null
                            && s.IsAvailable
                            && (s.Status == null || s.Status == "Disponível"))
                .GroupBy(s => new { s.EquipmentName, s.Type })
                .Select(g => new RequestStockOption
                {
                    StockId = g.Min(x => x.Id),
                    Label = (g.Key.EquipmentName ?? "Sem nome")
                        + " - "
                        + (g.Key.Type ?? "Sem tipo")
                        + " (Disponíveis: "
                        + g.Count()
                        + ")"
                })
                .OrderBy(x => x.Label)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRequestStockAsync(int stockId, int quantity, string notes)
        {
            var userIdStr = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            if (stockId <= 0 || quantity <= 0)
            {
                TempData["Error"] = "Dados de requisição inválidos.";
                return RedirectToPage();
            }

            var selectedStock = await _context.StockEmpresa
                .Where(s => s.Id == stockId
                            && s.TechnicianId == null
                            && s.AgrupamentoId == null
                            && s.SchoolId == null
                            && s.IsAvailable
                            && (s.Status == null || s.Status == "Disponível"))
                .FirstOrDefaultAsync();

            if (selectedStock == null)
            {
                TempData["Error"] = "O artigo selecionado já não está disponível.";
                return RedirectToPage();
            }

            var itemName = selectedStock.EquipmentName ?? "Sem nome";
            var itemType = selectedStock.Type ?? "Sem tipo";

            var jsonData = JsonSerializer.Serialize(new { 
                ItemName = itemName, 
                ItemType = itemType, 
                Quantity = quantity,
                RequestorId = userId,
                RequestorRole = "Tecnico"
            });

            var ticket = new Ticket
            {
                Description = $"PEDIDO DE STOCK (TÉCNICO):\nArtigo: {itemName}\nTipo: {itemType}\nQtd: {quantity}\nNotas: {notes}\n\n[DATA:{jsonData}]",
                Level = "Empréstimo",
                Status = "Pedido",
                CreatedAt = DateTime.UtcNow,
                TechnicianId = userId
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Requisição de stock enviada para a administração.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReturnToHQAsync(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            var item = await _context.StockEmpresa.FirstOrDefaultAsync(s => s.Id == id && s.TechnicianId == userId);
            if (item != null)
            {
                item.TechnicianId = null;
                item.Status = "Disponível";
                item.IsAvailable = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Item devolvido ao stock central com sucesso.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFinalDisposalAsync(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            var item = await _context.StockEmpresa.FirstOrDefaultAsync(s => s.Id == id && s.TechnicianId == userId);
            if (item != null)
            {
                _context.StockEmpresa.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Baixa definitiva efetuada. O item foi removido do inventário.";
            }
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostCreateStockRequestAsync(string? itemName, string? itemType, int quantity, string? notes)
        {
            var userIdStr = HttpContext.Session.GetString("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                TempData["Error"] = "Por favor selecione um artigo.";
                return RedirectToPage();
            }

            var ticket = new Ticket
            {
                Description = $"PEDIDO DE EQUIPAMENTO (TÉCNICO):\nArtigo: {itemName}\nTipo: {itemType ?? "N/A"}\nQuantidade: {quantity}\nMotivo: {notes}",
                Level = "Empréstimo",
                Status = "Pedido",
                CreatedAt = DateTime.UtcNow,
                TechnicianId = userId
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Pedido de {itemName} enviado com sucesso para a administração.";
            return RedirectToPage();
        }
    }
}
