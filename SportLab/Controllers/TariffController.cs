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
            var tariffs = _context.Tariffs.ToList();
            return View(tariffs);
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
    }
}
