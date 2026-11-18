using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly SportLabContext _context;
        private readonly IStringLocalizer<ScheduleController> _localizer;

        public ScheduleController(SportLabContext context, IStringLocalizer<ScheduleController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // PUBLIC SCHEDULE � returns current-week valid slots only
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            int diff = ((int)today.DayOfWeek + 6) % 7; // Monday = 0
            var weekStart = today.AddDays(-diff);
            var weekEnd = weekStart.AddDays(7);

            var now = DateTime.Now;

            // Only fetch future or currently running workouts for this week
            var slots = await _context.WorkoutSlots
                .AsNoTracking()
                .Include(w => w.Trainer)
                .Include(w => w.Bookings)
                .Where(w =>
                    w.Status != "Cancelled" &&
                    w.StartTime >= weekStart &&
                    w.StartTime < weekEnd &&
                    w.EndTime > now)
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            foreach (var slot in slots)
            {
                slot.Booked = slot.Bookings.Count;
                if (slot.Status != "Completed")
                {
                    slot.Status = slot.Booked >= slot.Capacity ? "Booked" : "Available";
                }
            }

            ViewBag.IsClient = User.IsInRole(Roles.Client);
            ViewBag.WeekStart = weekStart;

            return View(slots);
        }

        // JSON endpoint � returns current-week slots for async Fetch
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetWeekSlots()
        {
            var today = DateTime.Today;
            int diff = ((int)today.DayOfWeek + 6) % 7;
            var weekStart = today.AddDays(-diff);
            var weekEnd = weekStart.AddDays(7);

            int? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid))
            {
                currentUserId = uid;
            }

            var now = DateTime.Now;

            var slots = await _context.WorkoutSlots
                .AsNoTracking()
                .Include(w => w.Trainer)
                .Include(w => w.Bookings)
                .Where(w =>
                    w.Status != "Cancelled" &&
                    w.StartTime >= weekStart &&
                    w.StartTime < weekEnd &&
                    w.EndTime > now)
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            var result = slots.Select(w =>
            {
                int booked = w.Bookings.Count;
                bool isPast = w.StartTime <= now;
                bool isFull = booked >= w.Capacity;
                bool isUserBooked = currentUserId.HasValue && w.Bookings.Any(b => b.UserId == currentUserId.Value);

                string status = w.Status == "Completed"
                    ? "Completed"
                    : isPast
                        ? "Past"
                        : isFull
                            ? "Booked"
                            : "Available";

                return new
                {
                    id = w.Id,
                    day = ((int)w.StartTime.DayOfWeek + 6) % 7,
                    hour = w.StartTime.Hour,
                    timeStart = w.StartTime.ToString("HH:mm"),
                    timeEnd = w.EndTime.ToString("HH:mm"),
                    startDateTime = w.StartTime.ToString("o"),
                    category = w.ActivityName ?? "HIIT",
                    trainer = w.Trainer?.Name ?? "TBA",
                    difficulty = 2,
                    capacity = w.Capacity,
                    booked,
                    status,
                    isBooked = isUserBooked,
                    isPast,
                    isFull
                };
            });

            return Json(result);
        }

        // JSON BOOK � called via Fetch; no redirects
        [Authorize(Roles = Roles.Client)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookJson([FromForm] int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = _localizer["InvalidWorkout"].Value });

            int userId = CurrentUserId();

            var hasActiveSubscription = await _context.Subscriptions
                .AnyAsync(s => s.UserId == userId && s.IsActive && s.EndDate >= DateTime.Today);

            if (!hasActiveSubscription)
            {
                return BadRequest(new
                {
                    success = false,
                    requireSubscription = true,
                    message = _localizer["NeedMembership"].Value
                });
            }
            // -------------------------------

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var slot = await _context.WorkoutSlots
                .Include(w => w.Bookings)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
                return NotFound(new { success = false, message = _localizer["WorkoutNotFound"].Value });

            if (slot.Status == "Cancelled" || slot.Status == "Completed")
                return BadRequest(new { success = false, message = _localizer["NoLongerAvailable"].Value });

            if (slot.StartTime <= DateTime.Now)
                return BadRequest(new { success = false, message = _localizer["AlreadyStartedFinished"].Value });

            if (slot.TrainerId == userId)
                return BadRequest(new { success = false, message = _localizer["CannotBookOwn"].Value });

            if (slot.Bookings.Any(b => b.UserId == userId))
                return BadRequest(new { success = false, message = _localizer["AlreadyBooked"].Value });

            int current = slot.Bookings.Count;

            if (current >= slot.Capacity)
            {
                slot.Booked = current;
                slot.Status = "Booked";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return BadRequest(new { success = false, message = _localizer["FullyBooked"].Value });
            }

            _context.WorkoutBookings.Add(new WorkoutBooking
            {
                WorkoutSlotId = slot.Id,
                UserId = userId,
                BookingDate = DateTime.Now
            });

            int updated = current + 1;
            slot.Booked = updated;
            slot.Status = updated >= slot.Capacity ? "Booked" : "Available";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new
            {
                success = true,
                message = _localizer["BookedSuccess"].Value,
                booked = slot.Booked,
                status = slot.Status,
                capacity = slot.Capacity
            });
        }

        // JSON CANCEL BOOKING � called via Fetch; no redirects
        [Authorize(Roles = Roles.Client)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBookingJson([FromForm] int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = _localizer["InvalidBooking"].Value });

            int userId = CurrentUserId();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var booking = await _context.WorkoutBookings
                .Include(b => b.WorkoutSlot)
                    .ThenInclude(w => w.Bookings)
                .FirstOrDefaultAsync(b =>
                    b.WorkoutSlotId == id &&
                    b.UserId == userId);

            if (booking == null)
                return NotFound(new { success = false, message = _localizer["BookingNotFound"].Value });

            var slot = booking.WorkoutSlot!;

            if (slot.StartTime <= DateTime.Now)
                return BadRequest(new { success = false, message = _localizer["CannotCancelStarted"].Value });

            if (slot.Status == "Cancelled" || slot.Status == "Completed")
                return BadRequest(new { success = false, message = _localizer["NoLongerAvailable"].Value });

            _context.WorkoutBookings.Remove(booking);

            int remaining = slot.Bookings.Count - 1;
            slot.Booked = remaining;
            slot.Status = remaining >= slot.Capacity ? "Booked" : "Available";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new
            {
                success = true,
                message = _localizer["BookingCancelled"].Value,
                booked = slot.Booked,
                status = slot.Status,
                capacity = slot.Capacity
            });
        }

        private int CurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int parsedId))
            {
                throw new InvalidOperationException("The authenticated user does not have a valid ID claim.");
            }
            return parsedId;
        }
    }
}