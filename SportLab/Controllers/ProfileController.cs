using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;

namespace SportLab.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly SportLabContext _context;

        public ProfileController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (User.IsAdmin())
            {
                return RedirectToAction("Index", "Admin");
            }

            if (User.IsInRole(Roles.Trainer))
            {
                return RedirectToAction("Dashboard", "Trainer");
            }

            if (User.IsInRole(Roles.Moderator))
            {
                return RedirectToAction("Index", "Moderation");
            }

            return RedirectToAction(nameof(Client));
        }

        public async Task<IActionResult> Client()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            // Lab 6: the client dashboard shows live membership and booking data.
            ViewBag.ActiveSubscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.IsActive && s.EndDate >= DateTime.Today)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            ViewBag.NextBooking = await _context.WorkoutSlots
                .Include(w => w.Trainer)
                .Where(w => w.UserId == userId && w.StartTime >= DateTime.Now && w.Status != "Cancelled")
                .OrderBy(w => w.StartTime)
                .FirstOrDefaultAsync();

            ViewBag.BookingCount = await _context.WorkoutSlots
                .CountAsync(w => w.UserId == userId && w.StartTime >= DateTime.Now && w.Status != "Cancelled");

            return View(user);
        }

        [Authorize(Roles = Roles.TrainerRoles)]
        public IActionResult Trainer()
        {
            return RedirectToAction("Dashboard", "Trainer");
        }

        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> Clients()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var clients = await _context.WorkoutSlots
                .Include(w => w.User)
                .Where(w => w.TrainerId == userId && w.UserId != null)
                .Select(w => w.User!)
                .Distinct()
                .ToListAsync();

            return View(clients);
        }

        public IActionResult Settings()
        {
            return View();
        }

        [Authorize(Roles = Roles.TrainerRoles)]
        public async Task<IActionResult> Schedule()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var slots = await _context.WorkoutSlots
                .Include(w => w.User)
                .Where(w => w.TrainerId == userId)
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            return View(slots);
        }
    }
}
