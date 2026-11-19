using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;

namespace SportLab.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly SportLabContext _context;
        private readonly IStringLocalizer<NotificationController> _localizer;

        public NotificationController(SportLabContext context, IStringLocalizer<NotificationController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var notifications = new List<object>();
            var now = DateTime.Now;

            // 1. MESSAGES (All Roles) - Show unread messages from the last 7 days
            var recentMessages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == userId && m.SentDate >= now.AddDays(-7))
                .OrderByDescending(m => m.SentDate)
                .Take(3)
                .Select(m => new { m.Id, SenderName = m.Sender.Name, m.Text, m.SentDate })
                .ToListAsync();

            foreach (var msg in recentMessages)
            {
                notifications.Add(new
                {
                    type = "message",
                    title = _localizer["NewMessageFrom", msg.SenderName].Value,
                    text = msg.Text.Length > 40 ? msg.Text.Substring(0, 40) + "..." : msg.Text,
                    date = msg.SentDate,
                    url = "/Messages"
                });
            }

            // 2. UPCOMING WORKOUTS (Clients & Trainers)
            if (User.IsInRole(Roles.Client))
            {
                var upcoming = await _context.WorkoutBookings
                    .Include(b => b.WorkoutSlot)
                    .ThenInclude(w => w.Trainer)
                    .Where(b => b.UserId == userId &&
                                b.WorkoutSlot.StartTime > now &&
                                b.WorkoutSlot.StartTime <= now.AddHours(24) &&
                                b.WorkoutSlot.Status != "Cancelled")
                    .ToListAsync();

                foreach (var booking in upcoming)
                {
                    notifications.Add(new
                    {
                        type = "workout",
                        title = _localizer["UpcomingWorkout"].Value,
                        text = _localizer["UpcomingWorkoutText",
                            booking.WorkoutSlot.ActivityName,
                            booking.WorkoutSlot.Trainer.Name,
                            booking.WorkoutSlot.StartTime.ToString("HH:mm")].Value,
                        date = now,
                        url = "/Profile/Schedule"
                    });
                }

                // Cancellations
                var cancelled = await _context.WorkoutBookings
                    .Include(b => b.WorkoutSlot)
                    .Where(b => b.UserId == userId &&
                                b.WorkoutSlot.Status == "Cancelled" &&
                                b.WorkoutSlot.StartTime >= now.AddDays(-2))
                    .ToListAsync();

                foreach (var cancel in cancelled)
                {
                    notifications.Add(new
                    {
                        type = "danger",
                        title = _localizer["WorkoutCancelled"].Value,
                        text = _localizer["WorkoutCancelledText",
                            cancel.WorkoutSlot.ActivityName,
                            cancel.WorkoutSlot.StartTime.ToString("MMM dd")].Value,
                        date = now,
                        url = "/Schedule"
                    });
                }
            }
            else if (User.IsInRole(Roles.Trainer))
            {
                var upcoming = await _context.WorkoutSlots
                    .Where(w => w.TrainerId == userId &&
                                w.StartTime > now &&
                                w.StartTime <= now.AddHours(24) &&
                                w.Status != "Cancelled")
                    .ToListAsync();

                foreach (var slot in upcoming)
                {
                    notifications.Add(new
                    {
                        type = "workout",
                        title = _localizer["UpcomingSession"].Value,
                        text = _localizer["UpcomingSessionText",
                            slot.ActivityName,
                            slot.StartTime.ToString("HH:mm"),
                            slot.Booked].Value,
                        date = now,
                        url = "/Profile/Schedule"
                    });
                }
            }
            // 3. ADMIN ALERTS (Low Stock, New Users)
            else if (User.IsAdmin())
            {
                var lowStock = await _context.Products
                    .Where(p => p.StockQuantity < 5)
                    .Take(3)
                    .ToListAsync();

                foreach (var prod in lowStock)
                {
                    notifications.Add(new
                    {
                        type = "warning",
                        title = _localizer["LowStockAlert"].Value,
                        text = _localizer["LowStockText", prod.Name, prod.StockQuantity].Value,
                        date = now,
                        url = "/Admin/Shop"
                    });
                }

                var newUsers = await _context.Users
                    .Where(u => u.Id != userId && u.Role == Roles.Client) // Assuming no created date, just show recent clients
                    .OrderByDescending(u => u.Id)
                    .Take(2)
                    .ToListAsync();

                foreach (var usr in newUsers)
                {
                    notifications.Add(new
                    {
                        type = "user",
                        title = _localizer["NewClientRegistered"].Value,
                        text = _localizer["NewClientText", usr.Name].Value,
                        date = now,
                        url = "/Admin/Users"
                    });
                }
            }

            // Sort all by date if needed (since we used 'now' for some, they appear first)
            return Json(new { success = true, count = notifications.Count, items = notifications });
        }
    }
}