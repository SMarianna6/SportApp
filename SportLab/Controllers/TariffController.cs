using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;

namespace SportLab.Controllers
{
    public class TariffController : Controller
    {
        private readonly SportLabContext _context;

        public TariffController(SportLabContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            if (!_context.Plans.Any())
            {
                SeedDefaultTariffs();
            }

            var tariffs = _context.Plans.ToList();
            return View(tariffs);
        }

        [Authorize(Roles = Roles.ClientRoles)]
        public IActionResult ConfirmPurchase(int id)
        {
            var tariff = _context.Plans.Find(id);
            if (tariff == null)
            {
                return NotFound();
            }

            ViewBag.StartDate = DateTime.Today;
            ViewBag.EndDate = DateTime.Today.AddDays(tariff.Duration > 0 ? tariff.Duration : 30);
            return View(tariff);
        }

        [Authorize(Roles = Roles.ClientRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPurchase(int tariffId, string customerName, string customerPhone, string customerEmail, DateTime? startDate, string paymentMethod)
        {
            var tariff = _context.Plans.Find(tariffId);
            if (tariff == null)
            {
                return NotFound();
            }

            var effectiveStartDate = startDate ?? DateTime.Today;
            var effectiveEndDate = effectiveStartDate.AddDays(tariff.Duration > 0 ? tariff.Duration : 30);

            var userId = CurrentUserId();
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return Challenge();
            }

            // Lab 6: the purchase is always bound to the authenticated user,
            // never to an e-mail address supplied by the form.
            DeactivateActiveSubscriptions(userId);

            var subscription = new Subscription
            {
                UserId = user.Id,
                TariffId = tariff.Id,
                StartDate = effectiveStartDate,
                EndDate = effectiveEndDate,
                IsActive = true
            };

            _context.Subscriptions.Add(subscription);
            _context.SaveChanges();

            return RedirectToAction(nameof(PurchaseSuccess), new { id = subscription.Id });
        }

        [Authorize(Roles = Roles.ClientRoles)]
        [HttpPost]
        public IActionResult QuickPurchase([FromBody] QuickPurchaseRequest request)
        {
            if (request == null || request.TariffId <= 0)
            {
                return Json(new { success = false, message = "Invalid request payload." });
            }

            var tariff = _context.Plans.Find(request.TariffId);
            if (tariff == null)
            {
                return Json(new { success = false, message = "Selected membership plan was not found." });
            }

            var startDate = request.StartDate ?? DateTime.Today;
            var endDate = startDate.AddDays(tariff.Duration > 0 ? tariff.Duration : 30);

            var userId = CurrentUserId();
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Please sign in to purchase a membership." });
            }

            DeactivateActiveSubscriptions(userId);

            var subscription = new Subscription
            {
                UserId = user.Id,
                TariffId = tariff.Id,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true
            };

            _context.Subscriptions.Add(subscription);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                subscriptionId = subscription.Id,
                tariffName = tariff.Name,
                price = tariff.Price,
                startDate = startDate.ToString("dd.MM.yyyy"),
                endDate = endDate.ToString("dd.MM.yyyy"),
                userName = user.Name,
                message = "Membership successfully activated!"
            });
        }

        [Authorize(Roles = Roles.ClientRoles)]
        public IActionResult PurchaseSuccess(int id)
        {
            var subscription = _context.Subscriptions
                .Include(s => s.Tariff)
                .Include(s => s.User)
                .FirstOrDefault(s => s.Id == id);

            if (subscription == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsAdmin() && subscription.UserId != CurrentUserId())
            {
                return Forbid();
            }

            return View(subscription);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = Roles.AdminRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Tariff tariff)
        {
            if (ModelState.IsValid)
            {
                _context.Plans.Add(tariff);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(tariff);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            var tariff = _context.Plans.Find(id);
            if (tariff == null) return NotFound();

            return View(tariff);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Tariff tariff)
        {
            if (id != tariff.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(tariff);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(tariff);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var tariff = _context.Plans.Find(id);
            if (tariff != null)
            {
                _context.Plans.Remove(tariff);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Only one membership can be active at a time.
        /// </summary>
        private void DeactivateActiveSubscriptions(int userId)
        {
            var active = _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToList();

            foreach (var subscription in active)
            {
                subscription.IsActive = false;
            }
        }

        private void SeedDefaultTariffs()
        {
            var initialTariffs = new List<Plan>
            {
                new Plan
                {
                    Name = "Basic Start",
                    Type = "Basic",
                    Price = 850,
                    Duration = 30,
                    Description = "Full gym access (07:00 - 16:00)\nIntroductory coach orientation\nPersonal locker & shower facilities\nMobile workout tracking app\nComplimentary fresh drinking water"
                },
                new Plan
                {
                    Name = "Unlimited Pro",
                    Type = "Popular",
                    Price = 1450,
                    Duration = 30,
                    Description = "24/7 Unlimited access to all workout zones\nAll group classes included (CrossFit, TRX, Yoga)\nSpa & Recovery: Finnish sauna & jacuzzi\n2 Free guest passes per month\nFree membership freeze up to 7 days"
                },
                new Plan
                {
                    Name = "Student Drive",
                    Type = "Student",
                    Price = 650,
                    Duration = 30,
                    Description = "Gym access between 09:00 - 18:00\nGroup functional training sessions\nModern comfortable changing rooms\nValid with active student ID\n10% discount at the SportLab nutrition bar"
                },
                new Plan
                {
                    Name = "Annual VIP Unlimited",
                    Type = "Premium",
                    Price = 11900,
                    Duration = 365,
                    Description = "All-inclusive 365-day pass with no restrictions\n5 One-on-one sessions with a master trainer\nDedicated personal locker & fresh towel daily\nCertified doctor & nutritionist assessment\nUp to 30 days membership freeze protection\n15% discount at the SportLab store"
                }
            };

            _context.Plans.AddRange(initialTariffs);
            _context.SaveChanges();
        }
    }

    public class QuickPurchaseRequest
    {
        public int TariffId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? StartDate { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
