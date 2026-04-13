using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class TechnicianChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public TechnicianChatModel(AppDbContext context)
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
            if (!User.IsInRole("Tecnico")) return RedirectToPage("/Index");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            CurrentUserId = int.Parse(userIdStr);

            // For now chat is only Técnico <-> Admin
            Contacts = await _context.Users
                .Join(_context.Administradores, u => u.Id, a => a.UserId, (u, a) => u)
                .Where(u => u.Id != CurrentUserId)
                .Distinct()
                .ToListAsync();

            if (contactId.HasValue)
            {
                var allowed = Contacts.Any(c => c.Id == contactId.Value);
                if (!allowed) return RedirectToPage();

                SelectedContactId = contactId;
                Messages = await _context.Mensagens
                    .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == contactId) ||
                                (m.SenderId == contactId && m.ReceiverId == CurrentUserId))
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

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

            var receiverIsAdmin = await _context.Administradores.AnyAsync(a => a.UserId == request.ReceiverId);
            if (!receiverIsAdmin) return new BadRequestObjectResult("Destinatário inválido.");

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
                .Select(m => new
                {
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
        public string Content { get; set; } = "";
    }
}
