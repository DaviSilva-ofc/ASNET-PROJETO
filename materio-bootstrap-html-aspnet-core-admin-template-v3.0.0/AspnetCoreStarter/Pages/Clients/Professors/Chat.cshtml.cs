using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AspnetCoreStarter.Pages.Clients.Professors
{
    public class ProfessorChatModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProfessorChatModel(AppDbContext context)
        {
            _context = context;
        }

        public List<User> Contacts { get; set; } = new();
        public List<Mensagem> Messages { get; set; } = new();
        public int CurrentUserId { get; set; }
        public int? SelectedContactId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? contactId)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Auth/Login");
            CurrentUserId = int.Parse(userIdStr);

            // 1. Get the Agrupamento for this teacher to find their Director
            var professor = await _context.Professores
                .Include(p => p.Bloco)
                    .ThenInclude(b => b.School)
                .FirstOrDefaultAsync(p => p.UserId == CurrentUserId);

            int? agrupamentoId = professor?.Bloco?.School?.AgrupamentoId;

            // 2. Load Director(s) for this Agrupamento
            var directors = new List<User>();
            if (agrupamentoId.HasValue)
            {
                directors = await _context.Diretores
                    .Include(d => d.User)
                    .Where(d => d.AgrupamentoId == agrupamentoId && d.User != null)
                    .Select(d => d.User!)
                    .ToListAsync();
            }

            // 3. Load Technicians who accepted an active ticket from this teacher
            var technicians = await _context.Tickets
                .Where(t => t.RequestedByUserId == CurrentUserId && t.TechnicianId != null && t.Status != "Concluído")
                .Select(t => t.Technician!)
                .Distinct()
                .ToListAsync();

            // 4. Load all Admins
            var admins = await _context.Users
                .Join(_context.Administradores, u => u.Id, a => a.UserId, (u, a) => u)
                .ToListAsync();

            // Combine and unique contacts
            Contacts = directors.Concat(technicians).Concat(admins)
                .Where(u => u.Id != CurrentUserId)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .ToList();

            if (contactId.HasValue)
            {
                SelectedContactId = contactId;
                Messages = await _context.Mensagens
                    .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == contactId) || 
                                (m.SenderId == contactId && m.ReceiverId == CurrentUserId))
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

            // Segurança: Verificar se o destinatário está na lista permitida (Admin, Diretor ou Técnico com Ticket ativo)
            bool isAdmin = await _context.Administradores.AnyAsync(a => a.UserId == receiverId);
            bool isTechnicianWithActiveTicket = await _context.Tickets.AnyAsync(t => t.RequestedByUserId == senderId && t.TechnicianId == receiverId && t.Status != "Concluído");
            
            // Também validar Diretor do agrupamento
            var professor = await _context.Professores
                .Include(p => p.Bloco)
                    .ThenInclude(b => b.School)
                .FirstOrDefaultAsync(p => p.UserId == senderId);
            int? agrupamentoId = professor?.Bloco?.School?.AgrupamentoId;
            bool isDirector = agrupamentoId.HasValue && await _context.Diretores.AnyAsync(d => d.AgrupamentoId == agrupamentoId && d.UserId == receiverId);

            if (!isAdmin && !isTechnicianWithActiveTicket && !isDirector)
            {
                return new BadRequestObjectResult("Destinatário não autorizado.");
            }

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
