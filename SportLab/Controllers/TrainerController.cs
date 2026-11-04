using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;

namespace SportLab.Controllers
{
    /// <summary>
    /// Lab 6, Person 2: trainer features.
    /// Only trainers and administrators may open the trainer cabinet.
    /// </summary>
    [Authorize(Roles = Roles.TrainerRoles)]
    public class TrainerController : Controller
    {
        private readonly SportLabContext _context;

        public TrainerController(SportLabContext context)
        {
            _context = context;
        }

        // Public "Our Trainers" page: accessible to unauthenticated visitors.
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = CurrentUserId();
            var trainer = await _context.Users.FindAsync(userId);

            var slots = await _context.WorkoutSlots
                .Include(w => w.User)
                .Where(w => w.TrainerId == userId)
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            ViewBag.TrainerName = trainer?.Name ?? "Trainer";
            ViewBag.TrainerEmail = trainer?.Email ?? "";
            return View(slots);
        }

        [HttpGet]
        public IActionResult CreateSlot()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSlot([Bind("ActivityName,StartTime,EndTime")] WorkoutSlot slot)
        {
            slot.TrainerId = CurrentUserId();
            slot.Status = "Available";

            ModelState.Remove(nameof(WorkoutSlot.Trainer));
            ModelState.Remove(nameof(WorkoutSlot.User));

            if (slot.EndTime <= slot.StartTime)
            {
                ModelState.AddModelError(nameof(WorkoutSlot.EndTime), "End time must be later than the start time.");
            }

            if (ModelState.IsValid)
            {
                _context.WorkoutSlots.Add(slot);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Workout slot created successfully!";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(slot);
        }

        [HttpGet]
        public async Task<IActionResult> EditSlot(int id)
        {
            var slot = await _context.WorkoutSlots.FindAsync(id);
            if (slot == null)
            {
                return NotFound();
            }

            if (!CanManage(slot))
            {
                return Forbid();
            }

            if (slot.Status == "Booked")
            {
                TempData["ErrorMessage"] = "A booked slot cannot be edited. Cancel the booking first.";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(slot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSlot(int id, [Bind("Id,ActivityName,StartTime,EndTime")] WorkoutSlot form)
        {
            if (id != form.Id)
            {
                return NotFound();
            }

            var slot = await _context.WorkoutSlots.FindAsync(id);
            if (slot == null)
            {
                return NotFound();
            }

            if (!CanManage(slot))
            {
                return Forbid();
            }

            if (slot.Status == "Booked")
            {
                TempData["ErrorMessage"] = "A booked slot cannot be edited. Cancel the booking first.";
                return RedirectToAction(nameof(Dashboard));
            }

            form.TrainerId = slot.TrainerId;
            ModelState.Remove(nameof(WorkoutSlot.Trainer));
            ModelState.Remove(nameof(WorkoutSlot.User));

            if (form.EndTime <= form.StartTime)
            {
                ModelState.AddModelError(nameof(WorkoutSlot.EndTime), "End time must be later than the start time.");
            }

            if (!ModelState.IsValid)
            {
                return View(form);
            }

            slot.ActivityName = form.ActivityName;
            slot.StartTime = form.StartTime;
            slot.EndTime = form.EndTime;
            slot.Status = "Available";

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Workout slot updated.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            var slot = await _context.WorkoutSlots.FindAsync(id);
            if (slot == null)
            {
                return NotFound();
            }

            if (!CanManage(slot))
            {
                return Forbid();
            }

            if (slot.Status == "Booked")
            {
                TempData["ErrorMessage"] = "A booked slot cannot be deleted. Cancel the booking first.";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.WorkoutSlots.Remove(slot);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Workout slot deleted.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSlot(int id)
        {
            var slot = await _context.WorkoutSlots.FindAsync(id);
            if (slot == null)
            {
                return NotFound();
            }

            if (!CanManage(slot))
            {
                return Forbid();
            }

            slot.Status = "Cancelled";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Slot cancelled.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSlot(int id)
        {
            var slot = await _context.WorkoutSlots.FindAsync(id);
            if (slot == null)
            {
                return NotFound();
            }

            if (!CanManage(slot))
            {
                return Forbid();
            }

            slot.Status = "Completed";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Slot marked as completed.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> MyClients()
        {
            var userId = CurrentUserId();

            var clients = await _context.WorkoutSlots
                .Include(w => w.User)
                .Where(w => w.TrainerId == userId && w.UserId != null && w.Status == "Booked")
                .Select(w => w.User!)
                .Distinct()
                .ToListAsync();

            return View(clients);
        }

        public async Task<IActionResult> MySchedule()
        {
            var userId = CurrentUserId();

            var slots = await _context.WorkoutSlots
                .Include(w => w.User)
                .Where(w => w.TrainerId == userId && w.StartTime >= DateTime.Today)
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            return View(slots);
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// A trainer may only modify his/her own slots. Administrators may manage any slot.
        /// </summary>
        private bool CanManage(WorkoutSlot slot) =>
            User.IsAdmin() || slot.TrainerId == CurrentUserId();
    }
}
