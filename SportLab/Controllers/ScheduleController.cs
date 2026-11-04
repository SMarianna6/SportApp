using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using System.Security.Claims;

namespace SportLab.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly SportLabContext _context;

        public ScheduleController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var slots = _context.WorkoutSlots.Include(w => w.Trainer).ToList();
            return View(slots);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var slot = await _context.WorkoutSlots.FindAsync(id);

            if (slot == null || slot.Status != "Available")
            {
                TempData["ErrorMessage"] = "This slot is not available.";
                return RedirectToAction(nameof(Index));
            }

            if (slot.TrainerId == userId)
            {
                TempData["ErrorMessage"] = "You cannot book your own slot.";
                return RedirectToAction(nameof(Index));
            }

            slot.UserId = userId;
            slot.Status = "Booked";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Workout booked successfully!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var slot = await _context.WorkoutSlots.FindAsync(id);

            if (slot == null || slot.UserId != userId)
            {
                TempData["ErrorMessage"] = "You cannot cancel this booking.";
                return RedirectToAction(nameof(Index));
            }

            slot.UserId = null;
            slot.Status = "Available";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
