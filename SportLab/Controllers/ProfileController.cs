using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    public class ProfileController : Controller
    {
        private readonly SportLabContext _context;

        public ProfileController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Client()
        {
            return View();
        }

        public IActionResult Trainer()
        {
            return View();
        }

        public IActionResult Clients()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Schedule()
        {
            var slots = _context.Set<WorkoutSlot>().Include(w => w.Trainer).ToList();
            return View(slots);
        }
    }
}
