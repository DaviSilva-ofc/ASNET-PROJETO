using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Private
{
    public class PrivateChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public PrivateChatModel(AppDbContext context)
        {
            _context = context;
        }

        public List<User> Contacts { get; set; } = new();
        public List<Mensagem> Messages { get; set; } = new();
        public int CurrentUserId { get; set; }
        public int? SelectedContactId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? contactId)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");
            CurrentUserId = userId;

            // For Private Clients, contacts are all Administrators (Support)
            // Plus any users they have previously messaged
            var administrators = await _context.Administradores
                .Include(a => a.User)
                .Where(a => a.User != null)
                .Select(a => a.User!)
                .ToListAsync();

            var messagedUserIds = await _context.Mensagens
                .Where(m => m.ReceiverId == CurrentUserId || m.SenderId == CurrentUserId)
                .Select(m => m.SenderId == CurrentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var messagedUsers = await _context.Users
                .Where(u => messagedUserIds.Contains(u.Id))
                .ToListAsync();

            Contacts = administrators.Concat(messagedUsers)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .Where(u => u.Id != CurrentUserId)
                .ToList();

            if (contactId.HasValue)
            {
                SelectedContactId = contactId;
                Messages = await _context.Mensagens
                    .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == contactId) || 
                                (m.SenderId == contactId && m.ReceiverId == CurrentUserId))
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                // Mark messages from the contact to me as read
                var unread = Messages.Where(m => m.ReceiverId == CurrentUserId && !m.IsRead).ToList();
                if (unread.Any())
                {
                    unread.ForEach(m => m.IsRead = true);
                    await _context.SaveChangesAsync();
                }
            }
            else if (Contacts.Any())
            {
                // Optionally select the first admin by default or stay on welcome screen
                // SelectedContactId = Contacts.First().Id;
                // return RedirectToPage("./Chat", new { contactId = SelectedContactId });
            }

            return Page();
        }

        public async Task<IActionResult> OnGetFetchMessagesAsync(int contactId, int lastMessageId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return new UnauthorizedResult();
            var currentUserId = int.Parse(userIdStr);

            var newMessages = await _context.Mensagens
                .Where(m => m.Id > lastMessageId && 
                            ((m.SenderId == contactId && m.ReceiverId == currentUserId) || 
                             (m.SenderId == currentUserId && m.ReceiverId == contactId)))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    m.Id,
                    m.Content,
                    m.SenderId,
                    CreatedAt = m.CreatedAt.ToString("HH:mm"),
                    IsMe = m.SenderId == currentUserId
                })
                .ToListAsync();

            return new JsonResult(newMessages);
        }
    }
}
