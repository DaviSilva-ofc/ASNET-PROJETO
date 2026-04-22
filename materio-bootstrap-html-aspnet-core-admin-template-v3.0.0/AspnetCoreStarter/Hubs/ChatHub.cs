using Microsoft.AspNetCore.SignalR;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreStarter.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int receiverId, string content)
        {
            var senderIdStr = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderIdStr)) return;

            int senderId = int.Parse(senderIdStr);

            // Segurança: Validar se a comunicação é permitida
            // 1. Um dos intervenientes é Administrador
            bool isAllowed = await _context.Administradores.AnyAsync(a => a.UserId == senderId || a.UserId == receiverId);
            
            if (!isAllowed)
            {
                // 2. Existe um vínculo por Ticket Ativo e Aceite (Técnico <-> Solicitante)
                isAllowed = await _context.Tickets.AnyAsync(t => 
                    ((t.TechnicianId == senderId && t.RequestedByUserId == receiverId) ||
                     (t.TechnicianId == receiverId && t.RequestedByUserId == senderId))
                    && t.Status != "Concluído" && t.Status != "Recusado");
            }

            if (!isAllowed)
            {
                // 3. Comunicação interna de Escola/Agrupamento
                // Buscar dados de infraestrutura dos dois utilizadores
                var senderProf = await _context.Professores.Include(p => p.Bloco).ThenInclude(b => b.School).FirstOrDefaultAsync(p => p.UserId == senderId);
                var senderCoord = await _context.Coordenadores.FirstOrDefaultAsync(c => c.UserId == senderId);
                var senderDir = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == senderId);

                var receiverProf = await _context.Professores.Include(p => p.Bloco).ThenInclude(b => b.School).FirstOrDefaultAsync(p => p.UserId == receiverId);
                var receiverCoord = await _context.Coordenadores.FirstOrDefaultAsync(c => c.UserId == receiverId);
                var receiverDir = await _context.Diretores.FirstOrDefaultAsync(d => d.UserId == receiverId);

                // Mesma Escola
                int? sSchool = senderProf?.Bloco?.SchoolId ?? senderCoord?.SchoolId;
                int? rSchool = receiverProf?.Bloco?.SchoolId ?? receiverCoord?.SchoolId;
                if (sSchool.HasValue && rSchool.HasValue && sSchool == rSchool) isAllowed = true;

                // Mesmo Agrupamento (Diretor <-> Alguém da escola)
                if (!isAllowed)
                {
                    int? sAgrup = senderDir?.AgrupamentoId ?? senderProf?.Bloco?.School?.AgrupamentoId ?? senderCoord?.School?.AgrupamentoId;
                    int? rAgrup = receiverDir?.AgrupamentoId ?? receiverProf?.Bloco?.School?.AgrupamentoId ?? receiverCoord?.School?.AgrupamentoId;
                    if (sAgrup.HasValue && rAgrup.HasValue && sAgrup == rAgrup) isAllowed = true;
                }
            }

            if (!isAllowed) return;

            // Save to database (optional here if handled by PageModel, but safer here for real-time)
            var message = new Mensagem
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Mensagens.Add(message);
            await _context.SaveChangesAsync();

            // Fetch sender details for the UI (photo)
            var senderUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId);
            string senderPhoto = senderUser?.ProfilePhotoPath ?? "";

            // Broadcast to the specifically targeted user and the sender
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = senderId,
                senderName = senderUser?.Username ?? "Utilizador",
                senderPhoto = senderPhoto,
                content = content,
                createdAt = message.CreatedAt.ToString("HH:mm"),
                isMe = false
            });

            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = senderId,
                senderName = senderUser?.Username ?? "Utilizador",
                senderPhoto = senderPhoto,
                content = content,
                createdAt = message.CreatedAt.ToString("HH:mm"),
                isMe = true
            });
        }

        public async Task<string> DeleteMessage(int messageId)
        {
            try
            {
                var senderIdStr = Context.UserIdentifier;
                if (string.IsNullOrEmpty(senderIdStr)) return "Utilizador não autenticado.";

                int userId;
                if (!int.TryParse(senderIdStr, out userId)) return "ID de utilizador inválido.";

                var message = await _context.Mensagens.FindAsync(messageId);
                if (message == null) return "Mensagem não encontrada ou já foi apagada.";

                if (message.SenderId != userId) return "Não tem permissão para apagar esta mensagem.";

                _context.Mensagens.Remove(message);
                await _context.SaveChangesAsync();

                await Clients.User(message.ReceiverId.ToString()).SendAsync("MessageDeleted", messageId);
                await Clients.User(message.SenderId.ToString()).SendAsync("MessageDeleted", messageId);

                return null; // Success
            }
            catch (Exception ex)
            {
                return "Erro interno do servidor: " + ex.Message;
            }
        }

        public override async Task OnConnectedAsync()
        {
            // Optional: Handle online status
            await base.OnConnectedAsync();
        }
    }
}
