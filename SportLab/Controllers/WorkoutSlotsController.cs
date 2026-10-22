using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SportLab.Controllers
{
    public class WorkoutSlotsController : Controller
    {
        private readonly SportLabContext _context;

        public WorkoutSlotsController(SportLabContext context)
        {
            _context = context;
        }

        // GET: WorkoutSlots
        public async Task<IActionResult> Index()
        {
            var workoutSlots = _context.WorkoutSlots
                .Include(w => w.Trainer)
                .Include(w => w.User);
            return View(await workoutSlots.ToListAsync());
        }

        // GET: WorkoutSlots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workoutSlot = await _context.WorkoutSlots
                .Include(w => w.Trainer)
                .Include(w => w.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (workoutSlot == null)
            {
                return NotFound();
            }

            return View(workoutSlot);
        }

        // GET: WorkoutSlots/Create
        public IActionResult Create()
        {
            ViewData["TrainerId"] = new SelectList(_context.Users.Where(u => u.Role == "Trainer" || u.Role == "Admin"), "Id", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name");
            return View();
        }

        // POST: WorkoutSlots/Create
        // Захист від надмірної передачі даних (overposting) за допомогою атрибута [Bind]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ActivityName,TrainerId,UserId,StartTime,EndTime,Status")] WorkoutSlot workoutSlot)
        {
            if (ModelState.IsValid)
            {
                _context.Add(workoutSlot);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TrainerId"] = new SelectList(_context.Users.Where(u => u.Role == "Trainer" || u.Role == "Admin"), "Id", "Name", workoutSlot.TrainerId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", workoutSlot.UserId);
            return View(workoutSlot);
        }

        // GET: WorkoutSlots/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workoutSlot = await _context.WorkoutSlots.FindAsync(id);
            if (workoutSlot == null)
            {
                return NotFound();
            }
            ViewData["TrainerId"] = new SelectList(_context.Users.Where(u => u.Role == "Trainer" || u.Role == "Admin"), "Id", "Name", workoutSlot.TrainerId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", workoutSlot.UserId);
            return View(workoutSlot);
        }

        // POST: WorkoutSlots/Edit/5
        // Захист від надмірної передачі даних (overposting) за допомогою атрибута [Bind]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ActivityName,TrainerId,UserId,StartTime,EndTime,Status")] WorkoutSlot workoutSlot)
        {
            if (id != workoutSlot.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workoutSlot);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkoutSlotExists(workoutSlot.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["TrainerId"] = new SelectList(_context.Users.Where(u => u.Role == "Trainer" || u.Role == "Admin"), "Id", "Name", workoutSlot.TrainerId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", workoutSlot.UserId);
            return View(workoutSlot);
        }

        // GET: WorkoutSlots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workoutSlot = await _context.WorkoutSlots
                .Include(w => w.Trainer)
                .Include(w => w.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workoutSlot == null)
            {
                return NotFound();
            }

            return View(workoutSlot);
        }

        // POST: WorkoutSlots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workoutSlot = await _context.WorkoutSlots.FindAsync(id);
            if (workoutSlot != null)
            {
                _context.WorkoutSlots.Remove(workoutSlot);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool WorkoutSlotExists(int id)
        {
            return _context.WorkoutSlots.Any(e => e.Id == id);
        }
    }
}
