using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Threading.Tasks;

namespace SportLab.Controllers
{
    public class MetricLogsController : Controller
    {
        private readonly SportLabContext _context;

        public MetricLogsController(SportLabContext context)
        {
            _context = context;
        }

        // GET: MetricLogs
        public async Task<IActionResult> Index()
        {
            var metricLogs = _context.MetricLogs
                .Include(m => m.User);
            return View(await metricLogs.ToListAsync());
        }

        // GET: MetricLogs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var metricLog = await _context.MetricLogs
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (metricLog == null)
            {
                return NotFound();
            }

            return View(metricLog);
        }

        // GET: MetricLogs/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name");
            return View();
        }

        // POST: MetricLogs/Create
        // Захист від надмірної передачі даних (overposting) за допомогою атрибута [Bind]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,Weight,RecordDate,Notes")] MetricLog metricLog)
        {
            if (ModelState.IsValid)
            {
                _context.Add(metricLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", metricLog.UserId);
            return View(metricLog);
        }

        // GET: MetricLogs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var metricLog = await _context.MetricLogs.FindAsync(id);
            if (metricLog == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", metricLog.UserId);
            return View(metricLog);
        }

        // POST: MetricLogs/Edit/5
        // Захист від надмірної передачі даних (overposting) за допомогою атрибута [Bind]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Weight,RecordDate,Notes")] MetricLog metricLog)
        {
            if (id != metricLog.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(metricLog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MetricLogExists(metricLog.Id))
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", metricLog.UserId);
            return View(metricLog);
        }

        // GET: MetricLogs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var metricLog = await _context.MetricLogs
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (metricLog == null)
            {
                return NotFound();
            }

            return View(metricLog);
        }

        // POST: MetricLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var metricLog = await _context.MetricLogs.FindAsync(id);
            if (metricLog != null)
            {
                _context.MetricLogs.Remove(metricLog);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MetricLogExists(int id)
        {
            return _context.MetricLogs.Any(e => e.Id == id);
        }
    }
}
