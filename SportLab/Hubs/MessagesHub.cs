using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SportLab.Hubs
{
    [Authorize]
    public class MessagesHub : Hub
    {
        private readonly SportLabContext _context;

        public MessagesHub(SportLabContext context)
        {
            _context = context;
        }

        public async Task JoinConversation(int otherUserId)
        {
            var userId = CurrentUserId();

            if (otherUserId <= 0 || !await _context.Users.AnyAsync(u => u.Id == otherUserId))
            {
                throw new HubException("Conversation participant was not found.");
            }

            var conversationId = ConversationId(userId, otherUserId);

            if (!IsParticipant(conversationId, userId))
            {
                throw new HubException("You are not a participant of this conversation.");
            }

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
            var senderId = CurrentUserId();

            // Admin broadcast: send message to all trainers
            if ((Context.User.IsInRole(Roles.Administrator) || Context.User.IsInRole(Roles.AdminAlias)) &&
                receiverId == "BROADCAST_TRAINERS")
            {
                var trainers = await _context.Users
                    .Where(u => u.Role == Roles.Trainer)
                    .ToListAsync();

                foreach (var trainer in trainers)
                {
                    var broadcastConversationId = ConversationId(senderId, trainer.Id);

                    if (!IsParticipant(broadcastConversationId, senderId))
                    {
                        throw new HubException("You are not a participant of this conversation.");
                    }

                    var broadcastMessage = new Message
                    {
                        SenderId = senderId,
                        ReceiverId = trainer.Id,
                        Text = text,
                        SentDate = DateTime.Now
                    };

                    _context.Messages.Add(broadcastMessage);
                    await _context.SaveChangesAsync();

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

            if (!int.TryParse(receiverId, out var receiverIdActual) || receiverIdActual <= 0 ||
                !await _context.Users.AnyAsync(u => u.Id == receiverIdActual))
            {
                throw new HubException("Message receiver was not found.");
            }

            var conversationId = ConversationId(senderId, receiverIdActual);

            if (!IsParticipant(conversationId, senderId))
            {
                throw new HubException("You are not a participant of this conversation.");
            }

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
            await Clients.Group(conversationId).SendAsync("NewMessage", new
            {
                Id = message.Id,
                SenderName = message.Sender?.Name ?? "Unknown",
                Text = message.Text,
                SentDate = message.SentDate,
                FromCurrentUser = message.SenderId == senderId
            });
        }

        private int CurrentUserId() =>
            int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private static string ConversationId(int firstUserId, int secondUserId) =>
            firstUserId < secondUserId
                ? $"{firstUserId}_{secondUserId}"
                : $"{secondUserId}_{firstUserId}";

        private static bool IsParticipant(string conversationId, int userId)
        {
            var parts = conversationId.Split('_');
            return parts.Length == 2 &&
                   int.TryParse(parts[0], out var firstUserId) &&
                   int.TryParse(parts[1], out var secondUserId) &&
                   (firstUserId == userId || secondUserId == userId);
        }
    }
}
