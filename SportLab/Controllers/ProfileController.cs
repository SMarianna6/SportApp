using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
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
            if (User.IsInRole("Trainer") || User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard", "Trainer");
            }
            return RedirectToAction(nameof(Client));
        }

        public async Task<IActionResult> Client()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            return View(user);
        }

        [Authorize(Roles = "Trainer,Admin")]
        public IActionResult Trainer()
        {
            return RedirectToAction("Dashboard", "Trainer");
        }

        [Authorize(Roles = "Trainer,Admin")]
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

        [Authorize(Roles = "Trainer,Admin")]
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
