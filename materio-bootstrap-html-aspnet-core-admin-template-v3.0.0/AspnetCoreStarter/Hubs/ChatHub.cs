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

            // Broadcast to the specifically targeted user and the sender
            // SignalR uses the NameIdentifier claim by default as the UserIdentifier
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = senderId,
                content = content,
                createdAt = message.CreatedAt.ToString("HH:mm"),
                isMe = false
            });

            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = senderId,
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
