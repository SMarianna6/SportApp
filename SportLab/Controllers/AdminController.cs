using Microsoft.AspNetCore.Mvc;
using SportLab.Data;
using SportLab.Models;
using System.Linq;

namespace SportLab.Controllers
{
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
                Tariffs = _context.Tariffs.ToList(),
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
        public IActionResult CreateTariff(Tariff tariff)
        {
            if (ModelState.IsValid)
            {
                _context.Tariffs.Add(tariff);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(tariff);
        }

        public IActionResult EditTariff(int? id)
        {
            if (id == null) return NotFound();

            var tariff = _context.Tariffs.Find(id);
            if (tariff == null) return NotFound();

            return View(tariff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTariff(int id, Tariff tariff)
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

            var tariff = _context.Tariffs.Find(id);
            if (tariff != null)
            {
                _context.Tariffs.Remove(tariff);
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
