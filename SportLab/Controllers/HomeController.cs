using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SportLabContext _context;

        public HomeController(ILogger<HomeController> logger, SportLabContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Trainers = await _context.Users
                .Include(u => u.TrainerProfile)
                .Where(u => u.Role == Roles.Trainer)
                .Take(3)
                .ToListAsync();

            ViewBag.Plans = await _context.Plans.ToListAsync();

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}