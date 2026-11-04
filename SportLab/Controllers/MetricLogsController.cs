using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SportLab.Controllers
{
    [Authorize]
    public class MetricLogsController : Controller
    {
        private readonly SportLabContext _context;

        public MetricLogsController(SportLabContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.MetricLogs
                .Include(m => m.User)
                .OrderBy(m => m.RecordDate)
                .AsQueryable();

            if (!User.IsAdmin() && !User.IsInRole(Roles.Trainer))
            {
                var userId = CurrentUserId();
                query = query.Where(m => m.UserId == userId);
            }

            return View(await query.ToListAsync());
        }

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

        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,Weight,RecordDate,Notes")] MetricLog metricLog)
        {
            if (!User.IsAdmin())
            {
                metricLog.UserId = CurrentUserId();
            }

            if (ModelState.IsValid)
            {
                _context.Add(metricLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", metricLog.UserId);
            return View(metricLog);
        }

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

            if (!User.IsAdmin() && !User.IsInRole(Roles.Trainer) && metricLog.UserId != CurrentUserId())
            {
                return Forbid();
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", metricLog.UserId);
            return View(metricLog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Weight,RecordDate,Notes")] MetricLog metricLog)
        {
            if (id != metricLog.Id)
            {
                return NotFound();
            }

            var existing = await _context.MetricLogs.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!User.IsAdmin() && !User.IsInRole(Roles.Trainer) && existing.UserId != CurrentUserId())
            {
                return Forbid();
            }

            if (!User.IsAdmin())
            {
                metricLog.UserId = existing.UserId;
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

            if (!User.IsAdmin() && !User.IsInRole(Roles.Trainer) && metricLog.UserId != CurrentUserId())
            {
                return Forbid();
            }

            return View(metricLog);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var metricLog = await _context.MetricLogs.FindAsync(id);
            if (metricLog == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsAdmin() && !User.IsInRole(Roles.Trainer) && metricLog.UserId != CurrentUserId())
            {
                return Forbid();
            }

            _context.MetricLogs.Remove(metricLog);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MetricLogExists(int id)
        {
            return _context.MetricLogs.Any(e => e.Id == id);
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
