using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;

namespace SportLab.Controllers
{
    public class ReviewController : Controller
    {
        private readonly SportLabContext _context;

        public ReviewController(SportLabContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Index(int? filterRating)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ModerationStatus == "Approved" || r.ModerationStatus == "Published");

            if (filterRating.HasValue && filterRating.Value >= 1 && filterRating.Value <= 5)
            {
                query = query.Where(r => r.Rating == filterRating.Value);
            }

            var reviews = query.OrderByDescending(r => r.CreatedDate).ToList();

            var allReviews = _context.Reviews.Where(r => r.ModerationStatus == "Approved" || r.ModerationStatus == "Published").ToList();
            ViewBag.AverageRating = allReviews.Any() ? Math.Round(allReviews.Average(r => r.Rating), 1) : 5.0;
            ViewBag.TotalReviews = allReviews.Count;
            ViewBag.FilterRating = filterRating;

            return View(reviews);
        }

        [Authorize(Roles = Roles.ClientRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int rating, string? comment)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid review data.";
                return RedirectToAction(nameof(Index));
            }

            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Please select a rating and write your review feedback.";
                return RedirectToAction(nameof(Index));
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!_context.Users.Any(u => u.Id == userId))
            {
                return Challenge();
            }

            var review = new Review
            {
                UserId = userId,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedDate = DateTime.UtcNow,
                // Lab 6: reviews must be approved by a moderator before publication.
                ModerationStatus = "Pending"
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thank you! Your review has been sent for moderation.";
            return RedirectToAction(nameof(Index));
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null) return NotFound();

            return View(review);
        }

        [Authorize(Roles = Roles.ModeratorRoles)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", review.UserId);
            return View(review);
        }

        [Authorize(Roles = Roles.ModeratorRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Comment,Rating,CreatedDate,ModerationStatus")] Review review)
        {
            if (id != review.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(review);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", review.UserId);
            return View(review);
        }

        [Authorize(Roles = Roles.ModeratorRoles)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null) return NotFound();

            return View(review);
        }

        [Authorize(Roles = Roles.ModeratorRoles)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null) _context.Reviews.Remove(review);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.Id == id);
        }
    }
}
