using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SportLab.Data;
using SportLab.Filters;
using SportLab.Models;

namespace SportLab.Controllers
{
    [AuthorizeRole("Admin")]
    public class AdminController : Controller
    {
        private readonly SportLabContext _context;

        public AdminController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index(string userSearch, string userStatus, string productSearch, string tariffSearch)
        {
            var usersQuery = from u in _context.Users select u;

            if (!string.IsNullOrEmpty(userSearch))
            {
                usersQuery = usersQuery.Where(u => u.Name != null && u.Name.Contains(userSearch) ||
                                                   u.Email != null && u.Email.Contains(userSearch));
            }

            if (!string.IsNullOrEmpty(userStatus))
            {
                if (userStatus == "Blocked")
                {
                    usersQuery = usersQuery.Where(u => u.IsBlocked);
                }
                else if (userStatus == "Active")
                {
                    usersQuery = usersQuery.Where(u => !u.IsBlocked);
                }
                else
                {
                    usersQuery = usersQuery.Where(u => u.Role == userStatus);
                }
            }

            var productsQuery = from p in _context.Products select p;
            if (!string.IsNullOrEmpty(productSearch))
            {
                productsQuery = productsQuery.Where(p => p.Name != null && p.Name.Contains(productSearch));
            }

            var tariffsQuery = from t in _context.Plans select t;
            if (!string.IsNullOrEmpty(tariffSearch))
            {
                tariffsQuery = tariffsQuery.Where(t => t.Name != null && t.Name.Contains(tariffSearch));
            }

            var model = new AdminPanelData
            {
                Tariffs = tariffsQuery.ToList(),
                Products = productsQuery.ToList(),
                Users = usersQuery.ToList()
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

        public IActionResult CreateUser()
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

        public IActionResult EditUser(int? id)
        {
            if (id == null) return NotFound();
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int id, User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        public IActionResult DeleteUser(int? id)
        {
            if (id == null) return NotFound();
            var user = _context.Users.Find(id);

            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleBlockUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsBlocked = !user.IsBlocked;
                _context.Update(user);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
