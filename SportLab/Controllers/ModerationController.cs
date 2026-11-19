using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    /// <summary>
    /// Lab 6: moderation of reviews and comments.
    /// Available to the Moderator role and to administrators.
    /// </summary>
    [Authorize(Roles = Roles.ModeratorRoles)]
    public class ModerationController : Controller
    {
        private readonly SportLabContext _context;
        private readonly IStringLocalizer<ModerationController> _localizer;

        public ModerationController(SportLabContext context, IStringLocalizer<ModerationController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderBy(r => r.ModerationStatus == "Pending" ? 0 : 1)
                .ThenByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            review.ModerationStatus = "Approved";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["ReviewApproved"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            review.ModerationStatus = "Hidden";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["ReviewHidden"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["ReviewDeleted"].Value;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
