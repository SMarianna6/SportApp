using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;

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
    }
}
