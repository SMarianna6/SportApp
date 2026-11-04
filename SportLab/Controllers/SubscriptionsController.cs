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
    public class SubscriptionsController : Controller
    {
        private readonly SportLabContext _context;

        public SubscriptionsController(SportLabContext context)
        {
            _context = context;
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public async Task<IActionResult> Index()
        {
            var subscriptions = _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.User);
            return View(await subscriptions.ToListAsync());
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subscription == null)
            {
                return NotFound();
            }

            return View(subscription);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public IActionResult Create()
        {
            ViewData["PlanId"] = new SelectList(_context.Plans, "Id", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name");
            return View();
        }

        [Authorize(Roles = Roles.AdminRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,PlanId,StartDate,EndDate,IsActive")] Subscription subscription)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subscription);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PlanId"] = new SelectList(_context.Plans, "Id", "Name", subscription.PlanId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", subscription.UserId);
            return View(subscription);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
            {
                return NotFound();
            }
            ViewData["PlanId"] = new SelectList(_context.Plans, "Id", "Name", subscription.PlanId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", subscription.UserId);
            return View(subscription);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,PlanId,StartDate,EndDate,IsActive")] Subscription subscription)
        {
            if (id != subscription.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(subscription);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubscriptionExists(subscription.Id))
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
            ViewData["PlanId"] = new SelectList(_context.Plans, "Id", "Name", subscription.PlanId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Name", subscription.UserId);
            return View(subscription);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (subscription == null)
            {
                return NotFound();
            }

            return View(subscription);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription != null)
            {
                _context.Subscriptions.Remove(subscription);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = Roles.ClientRoles)]
        public async Task<IActionResult> My()
        {
            var userId = CurrentUserId();

            var subscriptions = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            return View(subscriptions);
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool SubscriptionExists(int id)
        {
            return _context.Subscriptions.Any(e => e.Id == id);
        }
    }
}
