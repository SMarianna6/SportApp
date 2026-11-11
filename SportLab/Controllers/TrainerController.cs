using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    /// <summary>
    /// Trainer features.
    /// Only trainers and administrators may access this controller.
    /// </summary>
    [Authorize(Roles = Roles.TrainerRoles)]
    public class TrainerController : Controller
    {
        private readonly SportLabContext _context;

        public TrainerController(SportLabContext context)
        {
            _context = context;
        }

        // PUBLIC TRAINERS PAGE

        // PUBLIC TRAINERS PAGE
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Fetch all trainers and their profile details from the DB
            var trainers = await _context.Users
                .Include(u => u.TrainerProfile)
                .Where(u => u.Role == Roles.Trainer)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View(trainers);
        }

        // TRAINER DASHBOARD

        [HttpGet]
        public IActionResult Dashboard()
        {
            return RedirectToAction("Trainer", "Profile");
        }

        // DASHBOARD SCHEDULE: TODAY AND TOMORROW

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            int trainerId = CurrentUserId();

            DateTime today = DateTime.Today;
            DateTime dayAfterTomorrow = today.AddDays(2);

            var slots = await _context.WorkoutSlots
                .AsNoTracking()
                .Include(w => w.Bookings)
                    .ThenInclude(b => b.User)
                .Where(w =>
                    w.TrainerId == trainerId &&
                    w.StartTime >= today &&
                    w.StartTime < dayAfterTomorrow &&
                    w.Status != "Cancelled")
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            var result = slots.Select(slot => new
            {
                id = slot.Id,
                activityName = slot.ActivityName,
                date = slot.StartTime.ToString("yyyy-MM-dd"),
                startTime = slot.StartTime.ToString("HH:mm"),
                endTime = slot.EndTime.ToString("HH:mm"),
                status = slot.Status,
                capacity = slot.Capacity,
                booked = slot.Bookings.Count,
                clients = slot.Bookings
                    .Select(b => new
                    {
                        id = b.UserId,
                        name = b.User.Name
                    })
                    .ToList()
            });

            return Json(new
            {
                success = true,
                slots = result
            });
        }

        // OPTIONAL STANDALONE CREATE SLOT PAGE

        [HttpGet]
        public IActionResult CreateSlot()
        {
            return View();
        }

        // CREATE SLOT FROM TRAINER DASHBOARD

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSlotApi(
            [FromForm] TrainerSlotRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request."
                });
            }

            if (string.IsNullOrWhiteSpace(request.ActivityName) ||
                request.ActivityName.Trim().Length < 2 ||
                request.ActivityName.Trim().Length > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Activity name must contain between 2 and 100 characters."
                });
            }

            if (!DateTime.TryParseExact(
                    $"{request.Date} {request.StartTime}",
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime startTime) ||
                !DateTime.TryParseExact(
                    $"{request.Date} {request.EndTime}",
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime endTime))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid date or time."
                });
            }

            if (startTime <= DateTime.Now)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "A workout must start in the future."
                });
            }

            if (endTime <= startTime)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "End time must be later than start time."
                });
            }

            if (startTime.TimeOfDay < TimeSpan.FromHours(8) ||
                endTime.TimeOfDay > TimeSpan.FromHours(20))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Workouts must be scheduled between 08:00 and 20:00."
                });
            }

            if (request.Capacity < 1 || request.Capacity > 1000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Capacity must be between 1 and 1000."
                });
            }

            var slot = new WorkoutSlot
            {
                ActivityName = request.ActivityName.Trim(),
                TrainerId = CurrentUserId(),
                StartTime = startTime,
                EndTime = endTime,
                Capacity = request.Capacity,
                Booked = 0,
                Status = "Available"
            };

            _context.WorkoutSlots.Add(slot);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Workout slot created successfully.",
                slotId = slot.Id
            });
        }

        // CANCEL SLOT AND NOTIFY ALL BOOKED CLIENTS

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSlotApi(
            [FromForm] int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid workout ID."
                });
            }

            var slot = await _context.WorkoutSlots
                .Include(w => w.Bookings)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Workout not found."
                });
            }

            if (slot.TrainerId != CurrentUserId())
                return Forbid();

            if (slot.Status == "Cancelled" ||
                slot.Status == "Completed")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This workout can no longer be cancelled."
                });
            }

            int trainerId = CurrentUserId();

            foreach (var booking in slot.Bookings)
            {
                _context.Messages.Add(new Message
                {
                    SenderId = trainerId,
                    ReceiverId = booking.UserId,
                    Text =
                        $"Your training session \"{slot.ActivityName}\" " +
                        $"on {slot.StartTime:dd.MM.yyyy HH:mm} " +
                        "was cancelled by the trainer. " +
                        "Please book another available slot if needed.",
                    SentDate = DateTime.Now
                });
            }

            slot.Status = "Cancelled";

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Workout cancelled. All booked clients have been notified."
            });
        }

        // GET CLIENTS WHO HAVE BOOKED THIS TRAINER'S SESSIONS

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            int trainerId = CurrentUserId();

            var bookings = await _context.WorkoutBookings
                .AsNoTracking()
                .Where(b => b.WorkoutSlot.TrainerId == trainerId)
                .Select(b => new
                {
                    b.UserId,
                    name = b.User.Name,
                    email = b.User.Email,
                    avatarUrl = b.User.AvatarUrl,
                    goal = b.User.Goal,
                    startTime = b.WorkoutSlot.StartTime,
                    status = b.WorkoutSlot.Status,
                    b.BookingDate
                })
                .ToListAsync();

            var userIds = bookings.Select(b => b.UserId).Distinct().ToList();
            var today = DateTime.Today;

            var weights = await _context.MetricLogs
                .AsNoTracking()
                .Where(m => userIds.Contains(m.UserId))
                .OrderBy(m => m.RecordDate)
                .Select(m => new { m.UserId, m.Weight, m.RecordDate })
                .ToListAsync();

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => userIds.Contains(s.UserId))
                .Select(s => new { s.UserId, s.IsActive, s.EndDate })
                .ToListAsync();

            var clients = bookings
                .GroupBy(b => b.UserId)
                .Select(g =>
                {
                    var first = g.First();
                    var past = g.Where(b =>
                        b.startTime < DateTime.Now &&
                        b.status != "Cancelled").ToList();
                    var lastWorkout = (past.Count > 0 ? past : g.ToList())
                        .Max(b => b.startTime);
                    var series = weights
                        .Where(w => w.UserId == g.Key)
                        .Select(w => w.Weight)
                        .ToList();
                    var sub = subscriptions
                        .Where(s => s.UserId == g.Key)
                        .OrderByDescending(s => s.EndDate)
                        .FirstOrDefault();

                    string membership = "Inactive";
                    if (sub != null && sub.IsActive && sub.EndDate.Date >= today)
                    {
                        membership = sub.EndDate.Date <= today.AddDays(7)
                            ? "Expiring"
                            : "Active";
                    }

                    return new
                    {
                        id = g.Key,
                        name = first.name,
                        email = first.email,
                        avatarUrl = first.avatarUrl,
                        goal = string.IsNullOrWhiteSpace(first.goal) ? "Not set" : first.goal,
                        workouts = g.Count(b => b.status != "Cancelled"),
                        lastWorkout,
                        lastBooking = g.Max(b => b.BookingDate),
                        status = membership,
                        weights = series
                    };
                })
                .OrderByDescending(c => c.lastBooking)
                .ToList();

            return Json(new
            {
                success = true,
                clients
            });
        }

        // EDIT SLOT: GET

        [HttpGet]
        public async Task<IActionResult> EditSlot(int id)
        {
            var slot = await _context.WorkoutSlots
                .Include(w => w.Bookings)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
                return NotFound();

            if (!CanManage(slot))
                return Forbid();

            if (slot.Bookings.Any())
            {
                TempData["ErrorMessage"] =
                    "A booked slot cannot be edited.";

                return RedirectToAction(nameof(Dashboard));
            }

            if (slot.Status == "Cancelled" ||
                slot.Status == "Completed")
            {
                TempData["ErrorMessage"] =
                    "Cancelled or completed workouts cannot be edited.";

                return RedirectToAction(nameof(Dashboard));
            }

            return View(slot);
        }

        // EDIT SLOT: POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSlot(
            int id,
            [Bind("Id,ActivityName,StartTime,EndTime")] WorkoutSlot form)
        {
            if (id != form.Id)
                return NotFound();

            var slot = await _context.WorkoutSlots
                .Include(w => w.Bookings)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
                return NotFound();

            if (!CanManage(slot))
                return Forbid();

            if (slot.Bookings.Any())
            {
                TempData["ErrorMessage"] =
                    "A booked slot cannot be edited.";

                return RedirectToAction(nameof(Dashboard));
            }

            if (slot.Status == "Cancelled" ||
                slot.Status == "Completed")
            {
                TempData["ErrorMessage"] =
                    "Cancelled or completed workouts cannot be edited.";

                return RedirectToAction(nameof(Dashboard));
            }

            if (form.StartTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(form.StartTime),
                    "A workout must be scheduled for a future time.");
            }

            if (form.EndTime <= form.StartTime)
            {
                ModelState.AddModelError(
                    nameof(form.EndTime),
                    "End time must be later than the start time.");
            }

            if (form.StartTime.TimeOfDay < TimeSpan.FromHours(8) ||
                form.EndTime.TimeOfDay > TimeSpan.FromHours(20))
            {
                ModelState.AddModelError(
                    nameof(form.StartTime),
                    "Workouts must be scheduled between 08:00 and 20:00.");
            }

            if (string.IsNullOrWhiteSpace(form.ActivityName) ||
                form.ActivityName.Trim().Length < 2 ||
                form.ActivityName.Trim().Length > 100)
            {
                ModelState.AddModelError(
                    nameof(form.ActivityName),
                    "Activity name must contain between 2 and 100 characters.");
            }

            if (!ModelState.IsValid)
                return View(form);

            slot.ActivityName = form.ActivityName!.Trim();
            slot.StartTime = form.StartTime;
            slot.EndTime = form.EndTime;
            slot.Status = "Available";
            slot.Booked = 0;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Workout slot updated.";

            return RedirectToAction(nameof(Dashboard));
        }

        // DELETE EMPTY SLOT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            var slot = await _context.WorkoutSlots
                .Include(w => w.Bookings)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
                return NotFound();

            if (!CanManage(slot))
                return Forbid();

            if (slot.Bookings.Any())
            {
                TempData["ErrorMessage"] =
                    "A workout with bookings cannot be deleted. Cancel it instead.";

                return RedirectToAction(nameof(Dashboard));
            }

            _context.WorkoutSlots.Remove(slot);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Workout slot deleted.";

            return RedirectToAction(nameof(Dashboard));
        }

        // MARK SLOT AS COMPLETED

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSlot(int id)
        {
            var slot = await _context.WorkoutSlots
                .FirstOrDefaultAsync(w => w.Id == id);

            if (slot == null)
                return NotFound();

            if (!CanManage(slot))
                return Forbid();

            if (slot.Status == "Cancelled")
            {
                TempData["ErrorMessage"] =
                    "A cancelled workout cannot be marked as completed.";

                return RedirectToAction(nameof(Dashboard));
            }

            if (slot.EndTime > DateTime.Now)
            {
                TempData["ErrorMessage"] =
                    "A workout can only be marked as completed after it has ended.";

                return RedirectToAction(nameof(Dashboard));
            }

            slot.Status = "Completed";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Slot marked as completed.";

            return RedirectToAction(nameof(Dashboard));
        }

        // HELPERS

        private int CurrentUserId()
        {
            return int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        private bool CanManage(WorkoutSlot slot)
        {
            return User.IsAdmin() ||
                   slot.TrainerId == CurrentUserId();
        }
    }

    // REQUEST MODEL FOR DASHBOARD SLOT CREATION

    public class TrainerSlotRequest
    {
        public string ActivityName { get; set; } = string.Empty;

        public string Date { get; set; } = string.Empty;

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public int Capacity { get; set; } = 15;
    }
}