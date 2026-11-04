using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;

namespace SportLab.Controllers
{
    [Authorize(Roles = "Trainer,Admin")]
    public class TrainerController : Controller
    {
        private readonly SportLabContext _context;

        public TrainerController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            slot.TrainerId = userId;
            slot.Status = "Available";

            ModelState.Remove("Trainer");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.WorkoutSlots.Add(slot);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Workout slot created successfully!";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(slot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSlot(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var slot = await _context.WorkoutSlots.FindAsync(id);

            if (slot == null || slot.TrainerId != userId)
            {
                return NotFound();
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var slot = await _context.WorkoutSlots.FindAsync(id);

            if (slot == null || slot.TrainerId != userId)
            {
                return NotFound();
            }

            slot.Status = "Completed";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Slot marked as completed.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> MyClients()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var slots = await _context.WorkoutSlots
                .Include(w => w.User)
                .Where(w => w.TrainerId == userId && w.StartTime >= DateTime.Today)
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            return View(slots);
        }
    }
}
