using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Admin
{
    public class ContactViewModel
    {
        public User User { get; set; }
        public string Role { get; set; }
    }

    public class AdminChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminChatModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ContactViewModel> Contacts { get; set; } = new();
        public List<Mensagem> Messages { get; set; } = new();
        public int CurrentUserId { get; set; }
        public string CurrentUserPhoto { get; set; }
        public int? SelectedContactId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? contactId)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            CurrentUserId = int.Parse(userIdStr);

            var currentUser = await _context.Users.FindAsync(CurrentUserId);
            CurrentUserPhoto = currentUser?.ProfilePhotoPath ?? "";

            // Fetch all users and identify their roles
            var allUsers = await _context.Users.ToListAsync();
            var adminIds = await _context.Administradores.Select(a => a.UserId).ToListAsync();
            var techIds = await _context.Tecnicos.Select(t => t.UserId).ToListAsync();
            var directorIds = await _context.Diretores.Select(d => d.UserId).ToListAsync();
            var coordIds = await _context.Coordenadores.Select(c => c.UserId).ToListAsync();
            var profIds = await _context.Professores.Select(p => p.UserId).ToListAsync();

            Contacts = allUsers
                .Where(u => u.Id != CurrentUserId)
                .Select(u => new ContactViewModel {
                    User = u,
                    Role = adminIds.Contains(u.Id) ? "Administrador" :
                           techIds.Contains(u.Id) ? "Técnico" :
                           directorIds.Contains(u.Id) ? "Diretor" :
                           coordIds.Contains(u.Id) ? "Coordenador" :
                           profIds.Contains(u.Id) ? "Professor" : "Utilizador"
                })
                .OrderBy(c => c.Role)
                .ThenBy(c => c.User.Username)
                .ToList();

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
