using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class ContactViewModel
    {
        public User User { get; set; }
        public string Role { get; set; }
    }

    public class DirectorChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorChatModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ContactViewModel> Contacts { get; set; } = new();
        public List<Mensagem> Messages { get; set; } = new();
        public int CurrentUserId { get; set; }
        public int? SelectedTechId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? techId)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            CurrentUserId = int.Parse(userIdStr);

            // 1. Get current Director's AgrupamentoId
            var agrupamentoId = await _context.Diretores
                .Where(d => d.UserId == CurrentUserId)
                .Select(d => d.AgrupamentoId)
                .FirstOrDefaultAsync();

            // 2. Load all Administrators
            var administrators = await _context.Administradores
                .Include(a => a.User)
                .Select(a => new ContactViewModel { User = a.User!, Role = "Administrador" })
                .ToListAsync();

            // 3. Load Coordinators from the same Grouping
            var coordinators = await _context.Coordenadores
                .Include(c => c.User)
                .Include(c => c.School)
                .Where(c => c.School != null && c.School.AgrupamentoId == agrupamentoId)
                .Select(c => new ContactViewModel { 
                    User = c.User!, 
                    Role = "Coordenador - " + (c.School!.Name ?? "Escola") 
                })
                .ToListAsync();

            Contacts = administrators.Concat(coordinators)
                .Where(c => c.User.Id != CurrentUserId)
                .OrderBy(c => c.Role)
                .ThenBy(c => c.User.Username)
                .ToList();

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

            var receiverId = request.ReceiverId;

            // Segurança: Verificar se o destinatário é um admin ou um coordenador do mesmo agrupamento
            bool isAdmin = await _context.Administradores.AnyAsync(a => a.UserId == receiverId);
            
            var senderAgrupamentoId = await _context.Diretores
                .Where(d => d.UserId == senderId)
                .Select(d => d.AgrupamentoId)
                .FirstOrDefaultAsync();

            bool isCoordinatorInMyGrouping = await _context.Coordenadores
                .Include(c => c.School)
                .AnyAsync(c => c.UserId == receiverId && c.School != null && c.School.AgrupamentoId == senderAgrupamentoId);

            if (!isAdmin && !isCoordinatorInMyGrouping) return new BadRequestObjectResult("Destinatário não autorizado.");

            var message = new Mensagem
            {
                SenderId = senderId,
                ReceiverId = receiverId,
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
