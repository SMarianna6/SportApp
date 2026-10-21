using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    public class TariffController : Controller
    {
        private readonly SportLabContext _context;

        public TariffController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Seed initial tariffs if the database table is empty
            if (!_context.Tariffs.Any())
            {
                SeedDefaultTariffs();
            }

            var tariffs = _context.Tariffs.ToList();
            return View(tariffs);
        }

        public IActionResult ConfirmPurchase(int id)
        {
            var tariff = _context.Tariffs.Find(id);
            if (tariff == null)
            {
                return NotFound();
            }

            ViewBag.StartDate = DateTime.Today;
            ViewBag.EndDate = DateTime.Today.AddDays(tariff.Duration > 0 ? tariff.Duration : 30);
            return View(tariff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPurchase(int tariffId, string customerName, string customerPhone, string customerEmail, DateTime? startDate, string paymentMethod)
        {
            var tariff = _context.Tariffs.Find(tariffId);
            if (tariff == null)
            {
                return NotFound();
            }

            var effectiveStartDate = startDate ?? DateTime.Today;
            var effectiveEndDate = effectiveStartDate.AddDays(tariff.Duration > 0 ? tariff.Duration : 30);

            // Find or register client user
            var user = _context.Users.FirstOrDefault(u => u.Email == customerEmail);
            if (user == null)
            {
                user = new User
                {
                    Name = string.IsNullOrWhiteSpace(customerName) ? "Клієнт SportLab" : customerName.Trim(),
                    Email = string.IsNullOrWhiteSpace(customerEmail) ? $"client_{Guid.NewGuid().ToString()[..8]}@sportlab.ua" : customerEmail.Trim(),
                    Password = "UserPass123!",
                    Role = "Client",
                    IsBlocked = false
                };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            // Create active subscription
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

        [HttpPost]
        public IActionResult QuickPurchase([FromBody] QuickPurchaseRequest request)
        {
            if (request == null || request.TariffId <= 0)
            {
                return Json(new { success = false, message = "Некоректні дані запиту." });
            }

            var tariff = _context.Tariffs.Find(request.TariffId);
            if (tariff == null)
            {
                return Json(new { success = false, message = "Обраний тариф не знайдено." });
            }

            var startDate = request.StartDate ?? DateTime.Today;
            var endDate = startDate.AddDays(tariff.Duration > 0 ? tariff.Duration : 30);

            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
            {
                user = new User
                {
                    Name = string.IsNullOrWhiteSpace(request.FullName) ? "Клієнт SportLab" : request.FullName.Trim(),
                    Email = string.IsNullOrWhiteSpace(request.Email) ? $"guest_{Guid.NewGuid().ToString()[..8]}@sportlab.ua" : request.Email.Trim(),
                    Password = "UserPass123!",
                    Role = "Client",
                    IsBlocked = false
                };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

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
                message = "Абонемент успішно придбано та активовано!"
            });
        }

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

            return View(subscription);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Tariff tariff)
        {
            if (ModelState.IsValid)
            {
                _context.Tariffs.Add(tariff);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(tariff);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            var tariff = _context.Tariffs.Find(id);
            if (tariff == null) return NotFound();

            return View(tariff);
        }

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

        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var tariff = _context.Tariffs.Find(id);
            if (tariff != null)
            {
                _context.Tariffs.Remove(tariff);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        private void SeedDefaultTariffs()
        {
            var initialTariffs = new List<Tariff>
            {
                new Tariff
                {
                    Name = "Базовий Старт",
                    Type = "Базовий",
                    Price = 850,
                    Duration = 30,
                    Description = "Доступ до тренажерного залу (07:00 - 16:00)\nВвідний інструктаж чергового тренера\nШафка для речей та душові кабіни\nМобільний додаток для трекінгу тренувань\nБезкоштовна питна вода"
                },
                new Tariff
                {
                    Name = "Безліміт Про",
                    Type = "Популярний",
                    Price = 1450,
                    Duration = 30,
                    Description = "Повний безлімітний доступ до залу 24/7\nУсі групові програми (CrossFit, TRX, Yoga)\nSPA-зона: фінська сауна та джакузі\nГостьовий візит для друга (2 рази на місяць)\nБезкоштовна заморозка абонемента до 7 днів"
                },
                new Tariff
                {
                    Name = "Студентський Драйв",
                    Type = "Студентський",
                    Price = 650,
                    Duration = 30,
                    Description = "Доступ у тренажерну зону з 09:00 до 18:00\nГрупові функціональні тренування\nСпортивні комфортні роздягальні\nДіє за умови пред'явлення студентського квитка\nЗнижка 10% на спортивне харчування в барі"
                },
                new Tariff
                {
                    Name = "Річний VIP Безліміт",
                    Type = "Преміум",
                    Price = 11900,
                    Duration = 365,
                    Description = "Необмежений доступ до всіх зон клубу цілий рік\n5 індивідуальних занять з персональним тренером\nІндивідуальна постійна шафка та свіжий рушник щодня\nПерсональна консультація лікаря та нутриціолога\nЗаморозка абонемента до 30 днів без втрати терміну\nЗнижка 15% у фітнес-магазині SportLab"
                }
            };

            _context.Tariffs.AddRange(initialTariffs);
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
