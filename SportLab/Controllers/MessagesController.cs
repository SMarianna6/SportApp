using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            // Build contact list based on role and actual booking relationships
            var contacts = new List<ContactViewModel>();

            if (userRole == "Admin")
            {
                // Admin: show all trainers and clients
                var allUsers = await _context.Users
                    .Where(u => u.Role == Roles.Trainer || u.Role == Roles.Client)
                    .ToListAsync();

                foreach (var u in allUsers)
                {
                    // Count unread messages from this user to admin
                    int unreadCount = 0;
                    if (User.IsInRole(Roles.Administrator) || User.IsInRole(Roles.AdminAlias))
                    {
                        unreadCount = _context.Messages
                            .Count(m => m.SenderId == u.Id && m.ReceiverId == userId && !m.IsRead);
                    }

                    // Get last message with this user
                    var lastMessage = _context.Messages
                        .Where(m => m.SenderId == u.Id && m.ReceiverId == userId ||
                                   m.SenderId == userId && m.ReceiverId == u.Id)
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

                // Broadcast contact for Admin: mass mailing to all trainers
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
                // Trainer: show clients who booked slots + Admin
                var clientIds = await _context.WorkoutSlots
                    .Where(w => w.TrainerId == userId && w.UserId != null && w.Status == "Booked")
                    .Select(w => w.User!.Id)
                    .Distinct()
                    .ToListAsync();

                // Add admin - find actual admin user
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Role == Roles.Administrator || u.Role == Roles.AdminAlias);
                if (adminUser != null)
                {
                    int adminUnreadCount = _context.Messages
                        .Count(m => m.SenderId == adminUser.Id && m.ReceiverId == userId && !m.IsRead);
                    contacts.Add(new ContactViewModel
                    {
                        Id = adminUser.Id.ToString(),
                        Name = adminUser.Name,
                        Role = adminUser.Role,
                        UnreadCount = adminUnreadCount,
                        HasConversation = _context.Messages
                            .Any(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) ||
                                      (m.SenderId == userId && m.ReceiverId == adminUser.Id)),
                        LastMessageText = _context.Messages
                            .Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) ||
                                       (m.SenderId == userId && m.ReceiverId == adminUser.Id))
                            .OrderByDescending(m => m.SentDate)
                            .Select(m => m.Text)
                            .FirstOrDefault(),
                        LastMessageDate = _context.Messages
                            .Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) ||
                                       (m.SenderId == userId && m.ReceiverId == adminUser.Id))
                            .OrderByDescending(m => m.SentDate)
                            .Select(m => (DateTime?)m.SentDate)
                            .FirstOrDefault() ?? DateTime.MinValue
                    });
                }

                // Add clients who booked slots
                foreach (var clientId in clientIds)
                {
                    var user = await _context.Users.FindAsync(clientId);
                    if (user != null)
                    {
                        int unreadCount = _context.Messages
                            .Count(m => m.SenderId == clientId && m.ReceiverId == userId && !m.IsRead);
                        // Get last message with this client
                        var lastMessage = _context.Messages
                            .Where(m => m.SenderId == clientId && m.ReceiverId == userId ||
                                       m.SenderId == userId && m.ReceiverId == clientId)
                            .OrderByDescending(m => m.SentDate)
                            .FirstOrDefault();
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
                // Client: show trainers they have bookings with + Admin
                var trainerIds = await _context.WorkoutSlots
                    .Where(w => w.UserId == userId && w.TrainerId != null && w.Status == "Booked")
                    .Select(w => w.Trainer!.Id)
                    .Distinct()
                    .ToListAsync();

                // Add admin - find actual admin user
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Role == Roles.Administrator || u.Role == Roles.AdminAlias);
                if (adminUser != null)
                {
                    int adminUnreadCount = _context.Messages
                        .Count(m => m.SenderId == adminUser.Id && m.ReceiverId == userId && !m.IsRead);
                    contacts.Add(new ContactViewModel
                    {
                        Id = adminUser.Id.ToString(),
                        Name = adminUser.Name,
                        Role = adminUser.Role,
                        UnreadCount = adminUnreadCount,
                        HasConversation = _context.Messages
                            .Any(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) ||
                                       (m.SenderId == userId && m.ReceiverId == adminUser.Id)),
                        LastMessageText = _context.Messages
                            .Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) ||
                                       (m.SenderId == userId && m.ReceiverId == adminUser.Id))
                            .OrderByDescending(m => m.SentDate)
                            .Select(m => m.Text)
                            .FirstOrDefault(),
                        LastMessageDate = _context.Messages
                            .Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) ||
                                       (m.SenderId == userId && m.ReceiverId == adminUser.Id))
                            .OrderByDescending(m => m.SentDate)
                            .Select(m => (DateTime?)m.SentDate)
                            .FirstOrDefault() ?? DateTime.MinValue
                    });
                }

                // Add trainers who have bookings with this client
                foreach (var trainerId in trainerIds)
                {
                    var user = await _context.Users.FindAsync(trainerId);
                    if (user != null)
                    {
                        int unreadCount = _context.Messages
                            .Count(m => m.SenderId == trainerId && m.ReceiverId == userId && !m.IsRead);
                        // Get last message with this trainer
                        var lastMessage = _context.Messages
                            .Where(m => m.SenderId == trainerId && m.ReceiverId == userId ||
                                       m.SenderId == userId && m.ReceiverId == trainerId)
                            .OrderByDescending(m => m.SentDate)
                            .FirstOrDefault();
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
            // Broadcast conversation (admin -> all trainers)
            if (contactId == "BROADCAST_TRAINERS")
            {
                var broadcastMessages = await _context.Messages
                    .Where(m => m.SenderId == CurrentUserId())
                    .OrderBy(m => m.SentDate)
                    .ToListAsync();

                var broadcastConversation = new
                {
                    id = "BROADCAST_TRAINERS",
                    name = "📢 All Trainers",
                    online = false,
                    typing = false,
                    unread = 0,
                    time = broadcastMessages.Any() ? broadcastMessages.Last().SentDate.ToString("HH:mm") : "",
                    goal = "",
                    weight = "",
                    next = "",
                    accent = "#7D51FE",
                    msgs = broadcastMessages.Select(m => new
                    {
                        from = "out",
                        text = m.Text,
                        time = m.SentDate.ToString("HH:mm"),
                        read = m.IsRead
                    }).ToList()
                };

                return Ok(new[] { broadcastConversation });
            }

            // Safe parse of regular user contact id
            if (!int.TryParse(contactId, out var parsedContactId))
            {
                return BadRequest(new { success = false, message = "Invalid contact identifier" });
            }

            var currentUserId = CurrentUserId();
            var otherUser = await _context.Users.FindAsync(parsedContactId);
            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == parsedContactId) ||
                            (m.SenderId == parsedContactId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentDate)
                .ToListAsync();

            var conversation = new
            {
                id = parsedContactId,
                name = otherUser?.Name ?? "Unknown",
                online = false,
                typing = false,
                unread = messages.Count(m => m.ReceiverId == currentUserId && m.SenderId == parsedContactId && !m.IsRead),
                time = messages.Any() ? messages.Last().SentDate.ToString("HH:mm") : "",
                goal = "",
                weight = "",
                next = "",
                accent = "#7D51FE",
                msgs = messages.Select(m => new
                {
                    from = m.SenderId == currentUserId ? "out" : "in",
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
            var senderId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(text))
            {
                return BadRequest(new { success = false, message = "Message text is required" });
            }

            if (receiverId == "BROADCAST_TRAINERS" &&
                (User.IsInRole(Roles.Administrator) || User.IsInRole(Roles.AdminAlias)))
            {
                // Mass mailing to all trainers
                var trainers = await _context.Users
                    .Where(u => u.Role == Roles.Trainer)
                    .ToListAsync();

                foreach (var trainer in trainers)
                {
                    _context.Messages.Add(new Message
                    {
                        SenderId = senderId,
                        ReceiverId = trainer.Id,
                        Text = text,
                        SentDate = DateTime.Now,
                        IsRead = false
                    });
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                int rId = int.Parse(receiverId);
                _context.Messages.Add(new Message
                {
                    SenderId = senderId,
                    ReceiverId = rId,
                    Text = text,
                    SentDate = DateTime.Now,
                    IsRead = false
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages
                .Include(m => m.Receiver)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        public IActionResult Create()
        {
            ViewData["ReceiverId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SenderId,ReceiverId,Text,SentDate,IsRead")] Message message)
        {
            if (!User.IsInRole(Roles.Administrator) && !User.IsInRole(Roles.AdminAlias))
            {
                message.SenderId = CurrentUserId();
            }

            if (ModelState.IsValid)
            {
                _context.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReceiverId"] = new SelectList(_context.Users, "Id", "Email", message.ReceiverId);
            ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Email", message.SenderId);
            return View(message);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }
            ViewData["ReceiverId"] = new SelectList(_context.Users, "Id", "Email", message.ReceiverId);
            ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Email", message.SenderId);
            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SenderId,ReceiverId,Text,SentDate,IsRead")] Message message)
        {
            if (id != message.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(message);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MessageExists(message.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReceiverId"] = new SelectList(_context.Users, "Id", "Email", message.ReceiverId);
            ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Email", message.SenderId);
            return View(message);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages
                .Include(m => m.Receiver)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                _context.Messages.Remove(message);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MessageExists(int id)
        {
            return _context.Messages.Any(e => e.Id == id);
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}