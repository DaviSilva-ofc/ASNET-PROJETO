using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorChatModel(AppDbContext context)
        {
            _context = context;
        }

        public List<User> Technicians { get; set; } = new();
        public List<Mensagem> Messages { get; set; } = new();
        public int CurrentUserId { get; set; }
        public int? SelectedTechId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? techId)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            CurrentUserId = int.Parse(userIdStr);

            // Load all technicians
            Technicians = await _context.Tecnicos
                .Include(t => t.User)
                .Where(t => t.User != null)
                .Select(t => t.User!)
                .ToListAsync();

            if (techId.HasValue)
            {
                SelectedTechId = techId;
                Messages = await _context.Mensagens
                    .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == techId) || 
                                (m.SenderId == techId && m.ReceiverId == CurrentUserId))
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                // Mark received messages as read
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

        public async Task<IActionResult> OnGetFetchMessagesAsync(int techId, int lastMessageId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return new UnauthorizedResult();
            var currentUserId = int.Parse(userIdStr);

            var newMessages = await _context.Mensagens
                .Where(m => m.Id > lastMessageId && 
                            ((m.SenderId == techId && m.ReceiverId == currentUserId) || 
                             (m.SenderId == currentUserId && m.ReceiverId == techId)))
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
