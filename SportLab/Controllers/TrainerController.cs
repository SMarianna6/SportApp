using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    [Authorize(Roles = Roles.TrainerRoles)]
    public class TrainerController : Controller
    {
        private readonly SportLabContext _context;

        public TrainerController(SportLabContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var trainers = await _context.Users
                .Include(u => u.TrainerProfile)
                .Where(u => u.Role == Roles.Trainer)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View(trainers);
        }

        // Œ¡–Œ¡ ¿ ¡–ŒÕﬁ¬¿ÕÕﬂ «≤ —“Œ–≤Õ » “–≈Õ≈–≤¬
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookTrainer(int trainerId)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["ErrorMessage"] = "Please log in or register to book a session.";
                return RedirectToAction("Login", "Account");
            }

            if (!User.IsInRole(Roles.Client))
            {
                TempData["ErrorMessage"] = "Only clients can book training sessions. You are logged in as a different role.";

                return RedirectToAction(nameof(Index));
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
           var hasActiveSubscription = await _context.Subscriptions
                .AnyAsync(s => s.UserId == userId && s.IsActive && s.EndDate >= DateTime.Today);

            if (!hasActiveSubscription)
            {
                TempData["ErrorMessage"] = "You need an active membership to book a session. Please choose a plan.";
                return RedirectToAction("Index", "Tariff");
            }

            TempData["SuccessMessage"] = "Membership verified! Please select a time slot below to complete your booking.";

            return RedirectToAction("Index", "Schedule", new { trainerId = trainerId });
        }

        private int CurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        private bool CanManage(WorkoutSlot slot)
        {
            return User.IsAdmin() || slot.TrainerId == CurrentUserId();
        }
    }

    public class TrainerSlotRequest
    {
        public string ActivityName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int Capacity { get; set; } = 15;
    }
}