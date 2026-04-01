using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Coordinators
{
    public class CoordinatorChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public CoordinatorChatModel(AppDbContext context)
        {
            _context = context;
        }

        public List<User> AvailableContacts { get; set; } = new();
        public User SelectedUser { get; set; }
        public List<Mensagem> ChatHistory { get; set; } = new();
        public int CurrentUserId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? userId)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            CurrentUserId = int.Parse(userIdStr);

            var coord = await _context.Coordenadores
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.UserId == CurrentUserId);

            if (coord?.SchoolId == null) return Page();

            int schoolId = coord.SchoolId.Value;
            int? agrupamentoId = coord.School.AgrupamentoId;

            // Restricted Contacts: 
            // 1. Professores from the same school
            var profes = await _context.Professores
                .Include(p => p.User)
                .Where(p => p.User != null && p.Bloco != null && p.Bloco.SchoolId == schoolId)
                .Select(p => p.User)
                .ToListAsync();

            // 2. Administrators
            var admins = await _context.Users
                .Join(_context.Administradores, u => u.Id, a => a.UserId, (u, a) => u)
                .ToListAsync();

            // 3. Diretor of the Agrupamento
            var diretores = new List<User>();
            if (agrupamentoId.HasValue)
            {
                diretores = await _context.Diretores
                    .Include(d => d.User)
                    .Where(d => d.User != null && d.AgrupamentoId == agrupamentoId)
                    .Select(d => d.User!)
                    .ToListAsync();
            }

            AvailableContacts = profes.Concat(admins).Concat(diretores)
                .Where(u => u != null && u.Id != CurrentUserId)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .ToList();

            if (userId.HasValue)
            {
                SelectedUser = AvailableContacts.FirstOrDefault(u => u.Id == userId);
                if (SelectedUser != null)
                {
                    ChatHistory = await _context.Mensagens
                        .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == userId) ||
                                    (m.SenderId == userId && m.ReceiverId == CurrentUserId))
                        .OrderBy(m => m.CreatedAt)
                        .ToListAsync();

                    // Mark as read
                    var unread = ChatHistory.Where(m => m.ReceiverId == CurrentUserId && !m.IsRead).ToList();
                    if (unread.Any())
                    {
                        unread.ForEach(m => m.IsRead = true);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSendMessageAsync(int receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return Content("Content empty");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int senderId = int.Parse(userIdStr);

            var msg = new Mensagem
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = System.DateTime.Now,
                IsRead = false
            };

            _context.Mensagens.Add(msg);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { userId = receiverId });
        }
    }
}
