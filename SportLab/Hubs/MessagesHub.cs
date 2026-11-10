using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SportLab.Hubs
{
    public class MessagesHub : Hub
    {
        private readonly SportLabContext _context;

        public MessagesHub(SportLabContext context)
        {
            _context = context;
        }

        public async Task JoinConversation(int otherUserId)
        {
            var userId = int.Parse(Context.GetHttpContext().User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Add to group named by conversation ID (ordered pair for consistency)
            var conversationId = otherUserId < userId
                ? $"{otherUserId}_{userId}"
                : $"{userId}_{otherUserId}";

            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

            // Send existing messages from the database
            var messageEntities = await _context.Messages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentDate)
                .ToListAsync();

            var messages = messageEntities.Select(m => new
            {
                m.Id,
                SenderName = m.Sender?.Name ?? "Unknown",
                ReceiverName = m.Receiver?.Name ?? "Unknown",
                m.Text,
                m.SentDate,
                FromCurrentUser = m.SenderId == userId
            }).ToList();

            await Clients.Caller.SendAsync("ReceiveMessages", conversationId, messages);
        }

        public async Task SendMessage(string receiverId, string text)
        {
            var userId = int.Parse(Context.GetHttpContext().User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var senderId = userId;

            // Admin broadcast: send message to all trainers
            if ((Context.User.IsInRole(Roles.Administrator) || Context.User.IsInRole(Roles.AdminAlias)) &&
                receiverId == "BROADCAST_TRAINERS")
            {
                var trainers = await _context.Users
                    .Where(u => u.Role == Roles.Trainer)
                    .ToListAsync();

                foreach (var trainer in trainers)
                {
                    var broadcastMessage = new Message
                    {
                        SenderId = senderId,
                        ReceiverId = trainer.Id,
                        Text = text,
                        SentDate = DateTime.Now
                    };

                    _context.Messages.Add(broadcastMessage);
                    await _context.SaveChangesAsync();

                    var broadcastConversationId = senderId < trainer.Id
                        ? $"{senderId}_{trainer.Id}"
                        : $"{trainer.Id}_{senderId}";

                    await Clients.Group(broadcastConversationId).SendAsync("NewMessage", new
                    {
                        Id = broadcastMessage.Id,
                        SenderName = broadcastMessage.Sender?.Name ?? "Unknown",
                        Text = broadcastMessage.Text,
                        SentDate = broadcastMessage.SentDate,
                        FromCurrentUser = broadcastMessage.SenderId == senderId
                    });
                }

                return;
            }

            // Determine receiver for direct messages
            var receiverIdActual = int.Parse(receiverId);

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverIdActual,
                Text = text,
                SentDate = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Broadcast to the conversation group
            var otherUserId = receiverIdActual;
            var conversationId = otherUserId < senderId
                ? $"{otherUserId}_{senderId}"
                : $"{senderId}_{otherUserId}";

            await Clients.Group(conversationId).SendAsync("NewMessage", new
            {
                Id = message.Id,
                SenderName = message.Sender?.Name ?? "Unknown",
                Text = message.Text,
                SentDate = message.SentDate,
                FromCurrentUser = message.SenderId == senderId
            });
        }
    }
}