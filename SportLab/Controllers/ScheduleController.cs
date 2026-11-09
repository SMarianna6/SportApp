using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
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

        [AllowAnonymous]
        public IActionResult Index()
        {
            var slots = _context.WorkoutSlots
                .Include(w => w.Trainer)
                .Where(w => w.Status != "Cancelled")
                .OrderBy(w => w.StartTime)
                .ToList();

            return View(slots);
        }

        [Authorize(Roles = Roles.ClientRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int id)
        {
            if (!ModelState.IsValid)//Lab7
            {
                TempData["ErrorMessage"] = "Invalid booking request.";
                return RedirectToAction(nameof(Index));
            }

            var userId = CurrentUserId();
            var slot = await _context.WorkoutSlots.FindAsync(id);

            if (slot == null || slot.Status != "Available" || slot.UserId != null)
            {
                TempData["ErrorMessage"] = "This slot is not available.";
                return RedirectToAction(nameof(Index));
            }

            if (slot.StartTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "This session has already started or finished.";
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
            return RedirectToAction(nameof(MyBookings));
        }

        [Authorize(Roles = Roles.ClientRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid cancellation request.";
                return RedirectToAction(nameof(MyBookings));
            }

            var userId = CurrentUserId();
            var slot = await _context.WorkoutSlots.FindAsync(id);

            if (slot == null || slot.UserId != userId)
            {
                TempData["ErrorMessage"] = "You cannot cancel this booking.";
                return RedirectToAction(nameof(MyBookings));
            }

            slot.UserId = null;
            slot.Status = "Available";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking cancelled successfully.";
            return RedirectToAction(nameof(MyBookings));
        }

        [Authorize(Roles = Roles.ClientRoles)]
        public async Task<IActionResult> MyBookings()
        {
            var userId = CurrentUserId();

            var slots = await _context.WorkoutSlots
                .Include(w => w.Trainer)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.StartTime)
                .ToListAsync();

            return View(slots);
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
