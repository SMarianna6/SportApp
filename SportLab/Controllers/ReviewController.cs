using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    public class ReviewController : Controller
    {
        private readonly SportLabContext _context;

        public ReviewController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? filterRating)
        {
            if (!_context.Reviews.Any())
            {
                SeedDefaultReviews();
            }

            var query = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ModerationStatus == "Approved" || r.ModerationStatus == "Published");

            if (filterRating.HasValue && filterRating.Value >= 1 && filterRating.Value <= 5)
            {
                query = query.Where(r => r.Rating == filterRating.Value);
            }

            var reviews = query.OrderByDescending(r => r.CreatedDate).ToList();

            // Calculate overall rating stats
            var allReviews = _context.Reviews.Where(r => r.ModerationStatus == "Approved" || r.ModerationStatus == "Published").ToList();
            ViewBag.AverageRating = allReviews.Any() ? Math.Round(allReviews.Average(r => r.Rating), 1) : 5.0;
            ViewBag.TotalReviews = allReviews.Count;
            ViewBag.FilterRating = filterRating;

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string authorName, string authorEmail, int rating, string comment)
        {
            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Please select a rating and write your review feedback.";
                return RedirectToAction(nameof(Index));
            }

            var email = string.IsNullOrWhiteSpace(authorEmail) ? $"client_{Guid.NewGuid().ToString()[..8]}@sportlab.ua" : authorEmail.Trim();
            var name = string.IsNullOrWhiteSpace(authorName) ? "Anonymous Member" : authorName.Trim();

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Name = name,
                    Email = email,
                    Password = "UserPass123!",
                    Role = "Client",
                    IsBlocked = false
                };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            var review = new Review
            {
                UserId = user.Id,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedDate = DateTime.UtcNow,
                ModerationStatus = "Approved"
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thank you! Your review has been published successfully.";
            return RedirectToAction(nameof(Index));
        }

        private void SeedDefaultReviews()
        {
            // Seed a representative user if needed
            var user1 = _context.Users.FirstOrDefault(u => u.Email == "anton.p@example.com");
            if (user1 == null)
            {
                user1 = new User { Name = "Anton Pavlenko", Email = "anton.p@example.com", Password = "Pass!", Role = "Client" };
                _context.Users.Add(user1);
            }

            var user2 = _context.Users.FirstOrDefault(u => u.Email == "olena.m@example.com");
            if (user2 == null)
            {
                user2 = new User { Name = "Olena Melnyk", Email = "olena.m@example.com", Password = "Pass!", Role = "Client" };
                _context.Users.Add(user2);
            }

            var user3 = _context.Users.FirstOrDefault(u => u.Email == "dmytro.s@example.com");
            if (user3 == null)
            {
                user3 = new User { Name = "Dmytro Shevchenko", Email = "dmytro.s@example.com", Password = "Pass!", Role = "Client" };
                _context.Users.Add(user3);
            }

            var user4 = _context.Users.FirstOrDefault(u => u.Email == "kate.v@example.com");
            if (user4 == null)
            {
                user4 = new User { Name = "Kateryna Vasylenko", Email = "kate.v@example.com", Password = "Pass!", Role = "Client" };
                _context.Users.Add(user4);
            }

            _context.SaveChanges();

            var defaultReviews = new List<Review>
            {
                new Review
                {
                    UserId = user1.Id,
                    Rating = 5,
                    Comment = "Best gym in town without a doubt! Outstanding equipment variety, pristine cleanliness, and super spacious even during peak hours. The sauna after workout is pure bliss.",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    ModerationStatus = "Approved"
                },
                new Review
                {
                    UserId = user2.Id,
                    Rating = 5,
                    Comment = "The personal trainers here are on another level. Iryna completely restructured my training regimen and helped me reach my deadlift PR safely. Highly recommend!",
                    CreatedDate = DateTime.UtcNow.AddDays(-7),
                    ModerationStatus = "Approved"
                },
                new Review
                {
                    UserId = user3.Id,
                    Rating = 4,
                    Comment = "Great fitness shop selection! I love that I can pick up high-grade whey protein, aminos, and pre-workout right at the front desk or order online with immediate pickup.",
                    CreatedDate = DateTime.UtcNow.AddDays(-12),
                    ModerationStatus = "Approved"
                },
                new Review
                {
                    UserId = user4.Id,
                    Rating = 5,
                    Comment = "The group Yoga and TRX sessions are incredible. Friendly instructors, positive athletic vibe, and smooth online booking. SportLab became my second home!",
                    CreatedDate = DateTime.UtcNow.AddDays(-16),
                    ModerationStatus = "Approved"
                }
            };

            _context.Reviews.AddRange(defaultReviews);
            _context.SaveChanges();
        }
    }
}
