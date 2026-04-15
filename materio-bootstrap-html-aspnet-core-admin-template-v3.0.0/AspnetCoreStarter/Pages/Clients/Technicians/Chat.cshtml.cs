using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class TechnicianChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public TechnicianChatModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ContactViewModel> Contacts { get; set; } = new();
        public List<Mensagem> Messages { get; set; } = new();
        public int CurrentUserId { get; set; }
        public int? SelectedContactId { get; set; }

        public class ContactViewModel
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string RoleLabel { get; set; }
            public bool IsAdmin { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? contactId)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            if (!User.IsInRole("Tecnico")) return RedirectToPage("/Index");

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            CurrentUserId = int.Parse(userIdStr);

            // Contactos: Administradores e Solicitantes de Tickets que o técnico aceitou
            var adminUsers = await _context.Users
                .Join(_context.Administradores, u => u.Id, a => a.UserId, (u, a) => u)
                .Where(u => u.Id != CurrentUserId)
                .Select(u => new ContactViewModel { Id = u.Id, Username = u.Username, RoleLabel = "Administrador", IsAdmin = true })
                .ToListAsync();

            var requesterUsers = await _context.Tickets
                .Where(t => t.TechnicianId == CurrentUserId && t.Status != "Concluído" && t.RequestedByUserId != null)
                .Select(t => new ContactViewModel { 
                    Id = t.RequestedBy!.Id, 
                    Username = t.RequestedBy.Username, 
                    RoleLabel = t.Level == "Empréstimo" ? "Cliente (Empréstimo)" : "Cliente (Reparação)",
                    IsAdmin = false 
                })
                .ToListAsync();

            Contacts = adminUsers.Concat(requesterUsers)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .ToList();

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
            var receiverId = request.ReceiverId;

            // Validar se o destinatário é um admin ou o solicitante de um ticket ativo do técnico
            var isAllowed = await _context.Administradores.AnyAsync(a => a.UserId == receiverId) ||
                            await _context.Tickets.AnyAsync(t => t.TechnicianId == senderId && t.RequestedByUserId == receiverId && t.Status != "Concluído" && t.Status != "Recusado");

            if (!isAllowed) return new BadRequestObjectResult("Destinatário inválido.");

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
