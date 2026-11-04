using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Filters;
using SportLab.Models;

namespace SportLab.Controllers
{
    //[AuthorizeRole("Admin")]
    public class AdminController : Controller
    {
        private readonly SportLabContext _context;

        public AdminController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new AdminPanelData
            {
                Tariffs = _context.Plans.ToList(),
                Products = _context.Products.ToList(),
                Users = _context.Users.ToList(),

            };

            return View(model);
        }

        public IActionResult CreateTariff()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTariff([Bind("Id,Name,Description,Price,Duration,Type")] Plan tariff)
        {
            if (ModelState.IsValid)
            {
                _context.Plans.Add(tariff);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(tariff);
        }

        public IActionResult EditTariff(int? id)
        {
            if (id == null) return NotFound();

            var tariff = _context.Plans.Find(id);
            if (tariff == null) return NotFound();

            return View(tariff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTariff(int id, [Bind("Id,Name,Description,Price,Duration,Type")] Plan tariff)
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

        public IActionResult DeleteTariff(int? id)
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

        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public IActionResult EditProduct(int? id)
        {
            if (id == null) return NotFound();

            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProduct(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(product);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public IActionResult DeleteProduct(int? id)
        {
            if (id == null) return NotFound();

            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Users()
        {
            ViewBag.TariffNames = _context.Plans.Select(p => p.Name).ToList();
            return View();
        }

        public IActionResult Tariffs()
        {
            return View();
        }

        public IActionResult Shop()
        {
            return View();
        }

        public IActionResult Schedule()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> GlobalSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new { users = new string[0], products = new string[0], tariffs = new string[0], faqs = new string[0] });

            var users = await _context.Users
                .Where(u => u.Name.Contains(q) || u.Email.Contains(q))
                .Take(4)
                .Select(u => new { id = u.Id, name = u.Name, subtitle = u.Email, url = $"/Admin/Users?openUser={u.Id}" })
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.Name.Contains(q))
                .Take(4)
                .Select(p => new { id = p.Id, name = p.Name, subtitle = p.Category, url = $"/Admin/Shop?openProduct={p.Id}" })
                .ToListAsync();

            var tariffs = await _context.Plans
                .Where(p => p.Name.Contains(q))
                .Take(4)
                .Select(p => new { id = p.Id, name = p.Name, subtitle = p.Price + " $", url = $"/Admin/Tariffs?openTariff={p.Id}" })
                .ToListAsync();

            var faqs = await _context.FAQs
                .Where(f => f.Question.Contains(q))
                .Take(3)
                .Select(f => new { id = f.Id, name = f.Question, subtitle = "FAQ", url = $"/Admin/FAQ?openFAQ=/{f.Id}" })
                .ToListAsync();

            return Json(new { users, products, tariffs, faqs });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersList()
        {
            var users = await _context.Users
                .Include(u => u.Subscriptions)
                .ThenInclude(s => s.Plan)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    role = u.Role,
                    active = !u.IsBlocked,
                    tariff = u.Subscriptions
                    .OrderByDescending(s => s.Id)
                    .Where(s => s.Plan != null)
                    .Select(s => s.Plan!.Name)
                    .FirstOrDefault()
                    ?? (u.Role == "Trainer" || u.Role == "Staff" ? "Staff" : "No Plan")
                })
                .ToListAsync();

            return Json(users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUserApi(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserApi(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsBlocked = !user.IsBlocked;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SaveUserApi([FromBody] UserDto userDto)
        {
            User? user;
            if (userDto.Id == 0)
            {
                if (string.IsNullOrWhiteSpace(userDto.Password))
                {
                    return BadRequest("A password is required for new users.");
                }

                user = new User
                {
                    Name = userDto.Name,
                    Email = userDto.Email,
                    Role = userDto.Role,
                    Password = userDto.Password ?? "Default123!",
                    IsBlocked = false
                };
                _context.Users.Add(user);
            }
            else
            {
                user = await _context.Users.Include(u => u.Subscriptions).FirstOrDefaultAsync(u => u.Id == userDto.Id);
                if (user == null) return NotFound();

                user.Name = userDto.Name;
                user.Email = userDto.Email;
                user.Role = userDto.Role;
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    user.Password = userDto.Password;
                }
            }

            var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Name == userDto.Tariff);

            if (plan != null)
            {
                var lastSubscription = user.Subscriptions.OrderByDescending(s => s.Id).FirstOrDefault();

                if (lastSubscription == null || lastSubscription.PlanId != plan.Id)
                {
                    foreach (var sub in user.Subscriptions.Where(s => s.IsActive))
                    {
                        sub.IsActive = false;
                        _context.Update(sub);
                    }

                    var newSubscription = new Subscription
                    {
                        PlanId = plan.Id,
                        User = user,
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddDays(plan.Duration),
                        IsActive = true
                    };
                    _context.Set<Subscription>().Add(newSubscription);
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }

        public class UserDto
        {
            public int Id { get; set; }
            public required string Name { get; set; }
            public required string Email { get; set; }
            public required string Role { get; set; }
            public required string Password { get; set; }
            public required string Tariff { get; set; }
        }
}
