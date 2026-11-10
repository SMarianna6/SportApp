using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly SportLabContext _context;

        public MessagesController(SportLabContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId();
            var userRole = User.IsInRole(Roles.Trainer) ? "Trainer" : User.IsInRole(Roles.Client) ? "Client" : "Admin";

            var contacts = new List<ContactViewModel>();

            if (userRole == "Admin")
            {
                var allUsers = await _context.Users
                    .Where(u => u.Role == Roles.Trainer || u.Role == Roles.Client)
                    .ToListAsync();

                foreach (var u in allUsers)
                {
                    int unreadCount = _context.Messages.Count(m => m.SenderId == u.Id && m.ReceiverId == userId && !m.IsRead);
                    var lastMessage = _context.Messages
                        .Where(m => (m.SenderId == u.Id && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == u.Id))
                        .OrderByDescending(m => m.SentDate)
                        .FirstOrDefault();

                    contacts.Add(new ContactViewModel
                    {
                        Id = u.Id.ToString(),
                        Name = u.Name,
                        Role = u.Role,
                        UnreadCount = unreadCount,
                        HasConversation = lastMessage != null,
                        LastMessageText = lastMessage?.Text,
                        LastMessageDate = lastMessage?.SentDate ?? DateTime.MinValue
                    });
                }

                contacts.Add(new ContactViewModel
                {
                    Id = "BROADCAST_TRAINERS",
                    Name = "📢 All Trainers",
                    Role = "Broadcast",
                    UnreadCount = 0,
                    HasConversation = true,
                    LastMessageText = null,
                    LastMessageDate = DateTime.MinValue
                });
            }
            else if (userRole == "Trainer")
            {
                var clientIds = await _context.WorkoutSlots
                    .Where(w => w.TrainerId == userId && w.UserId != null && w.Status == "Booked")
                    .Select(w => w.User!.Id)
                    .Distinct()
                    .ToListAsync();

                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == Roles.Administrator || u.Role == Roles.AdminAlias);
                if (adminUser != null)
                {
                    int adminUnreadCount = _context.Messages.Count(m => m.SenderId == adminUser.Id && m.ReceiverId == userId && !m.IsRead);
                    contacts.Add(new ContactViewModel
                    {
                        Id = adminUser.Id.ToString(),
                        Name = adminUser.Name,
                        Role = adminUser.Role,
                        UnreadCount = adminUnreadCount,
                        HasConversation = _context.Messages.Any(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == adminUser.Id)),
                        LastMessageText = _context.Messages.Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == adminUser.Id)).OrderByDescending(m => m.SentDate).Select(m => m.Text).FirstOrDefault(),
                        LastMessageDate = _context.Messages.Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == adminUser.Id)).OrderByDescending(m => m.SentDate).Select(m => (DateTime?)m.SentDate).FirstOrDefault() ?? DateTime.MinValue
                    });
                }

                foreach (var clientId in clientIds)
                {
                    var user = await _context.Users.FindAsync(clientId);
                    if (user != null)
                    {
                        int unreadCount = _context.Messages.Count(m => m.SenderId == clientId && m.ReceiverId == userId && !m.IsRead);
                        var lastMessage = _context.Messages.Where(m => (m.SenderId == clientId && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == clientId)).OrderByDescending(m => m.SentDate).FirstOrDefault();
                        contacts.Add(new ContactViewModel
                        {
                            Id = clientId.ToString(),
                            Name = user.Name,
                            Role = user.Role,
                            UnreadCount = unreadCount,
                            HasConversation = lastMessage != null,
                            LastMessageText = lastMessage?.Text,
                            LastMessageDate = lastMessage?.SentDate ?? DateTime.MinValue
                        });
                    }
                }
            }
            else // Client
            {
                var trainerIds = await _context.WorkoutSlots
                    .Where(w => w.UserId == userId && w.TrainerId != null && w.Status == "Booked")
                    .Select(w => w.Trainer!.Id)
                    .Distinct()
                    .ToListAsync();

                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == Roles.Administrator || u.Role == Roles.AdminAlias);
                if (adminUser != null)
                {
                    int adminUnreadCount = _context.Messages.Count(m => m.SenderId == adminUser.Id && m.ReceiverId == userId && !m.IsRead);
                    contacts.Add(new ContactViewModel
                    {
                        Id = adminUser.Id.ToString(),
                        Name = adminUser.Name,
                        Role = adminUser.Role,
                        UnreadCount = adminUnreadCount,
                        HasConversation = _context.Messages.Any(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == adminUser.Id)),
                        LastMessageText = _context.Messages.Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == adminUser.Id)).OrderByDescending(m => m.SentDate).Select(m => m.Text).FirstOrDefault(),
                        LastMessageDate = _context.Messages.Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == adminUser.Id)).OrderByDescending(m => m.SentDate).Select(m => (DateTime?)m.SentDate).FirstOrDefault() ?? DateTime.MinValue
                    });
                }

                foreach (var trainerId in trainerIds)
                {
                    var user = await _context.Users.FindAsync(trainerId);
                    if (user != null)
                    {
                        int unreadCount = _context.Messages.Count(m => m.SenderId == trainerId && m.ReceiverId == userId && !m.IsRead);
                        var lastMessage = _context.Messages.Where(m => (m.SenderId == trainerId && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == trainerId)).OrderByDescending(m => m.SentDate).FirstOrDefault();
                        contacts.Add(new ContactViewModel
                        {
                            Id = trainerId.ToString(),
                            Name = user.Name,
                            Role = user.Role,
                            UnreadCount = unreadCount,
                            HasConversation = lastMessage != null,
                            LastMessageText = lastMessage?.Text,
                            LastMessageDate = lastMessage?.SentDate ?? DateTime.MinValue
                        });
                    }
                }
            }

            ViewData["Contacts"] = contacts;
            ViewData["CurrentUserRole"] = userRole;
            return View(contacts);
        }

        [HttpGet]
        public async Task<IActionResult> GetConversation(string contactId)
        {
            var userId = CurrentUserId();

            if (contactId == "BROADCAST_TRAINERS")
            {
                return Ok(new[] { new { id = "BROADCAST_TRAINERS", name = "📢 All Trainers", msgs = new object[0] } });
            }

            if (!int.TryParse(contactId, out int targetId))
            {
                return BadRequest();
            }

            var otherUser = await _context.Users.FindAsync(targetId);
            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == targetId) || (m.SenderId == targetId && m.ReceiverId == userId))
                .OrderBy(m => m.SentDate)
                .ToListAsync();

            var conversation = new
            {
                id = targetId,
                name = otherUser?.Name ?? "Unknown",
                msgs = messages.Select(m => new
                {
                    from = m.SenderId == userId ? "out" : "in",
                    text = m.Text,
                    time = m.SentDate.ToString("HH:mm"),
                    read = m.IsRead
                }).ToList()
            };

            return Ok(new[] { conversation });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverId, string text)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(receiverId))
                return BadRequest();

            if (receiverId == "BROADCAST_TRAINERS" && (User.IsInRole(Roles.Administrator) || User.IsInRole(Roles.AdminAlias)))
            {
                var trainers = await _context.Users.Where(u => u.Role == Roles.Trainer).ToListAsync();
                foreach (var t in trainers)
                {
                    _context.Messages.Add(new Message { SenderId = userId, ReceiverId = t.Id, Text = text, SentDate = DateTime.Now, IsRead = false });
                }
            }
            else if (int.TryParse(receiverId, out int targetId))
            {
                _context.Messages.Add(new Message { SenderId = userId, ReceiverId = targetId, Text = text, SentDate = DateTime.Now, IsRead = false });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}