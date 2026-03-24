using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Admin
{
    public class ChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public ChatModel(AppDbContext context)
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
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            CurrentUserId = int.Parse(userIdStr);

            // Load contacts (Users who have chatted with us, or all Directors for start)
            // For now, let's load all Directors as potential contacts for the tech support
            var directors = await _context.Diretores
                .Include(d => d.User)
                .Where(d => d.User != null)
                .Select(d => d.User!)
                .ToListAsync();
            
            // Also include anyone who sent us a message
            var messagedUsers = await _context.Mensagens
                .Where(m => m.ReceiverId == CurrentUserId || m.SenderId == CurrentUserId)
                .Select(m => m.SenderId == CurrentUserId ? m.Receiver : m.Sender)
                .Distinct()
                .ToListAsync();

            Contacts = directors.Union(messagedUsers).Where(u => u.Id != CurrentUserId).ToList();

            if (contactId.HasValue)
            {
                SelectedContactId = contactId;
                Messages = await _context.Mensagens
                    .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == contactId) || 
                                (m.SenderId == contactId && m.ReceiverId == CurrentUserId))
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                // Mark as read
                var unread = Messages.Where(m => m.ReceiverId == CurrentUserId && !m.IsRead).ToList();
                if (unread.Any())
                {
                    unread.ForEach(m => m.IsRead = true);
                    await _context.SaveChangesAsync();
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSendMessageAsync([FromBody] MessageRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || request == null || string.IsNullOrWhiteSpace(request.Content))
                return new BadRequestResult();

            var senderId = int.Parse(userIdStr);

            var message = new Mensagem
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Mensagens.Add(message);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, messageId = message.Id, createdAt = message.CreatedAt.ToString("HH:mm") });
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

    public class MessageRequest
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; }
    }
}
