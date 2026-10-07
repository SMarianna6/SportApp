using Microsoft.AspNetCore.Mvc;
using SportLab.Data;
using SportLab.Models;
using System.Linq;

namespace SportLab.Controllers
{
    public class ShopController : Controller
    {
        private readonly SportLabContext _context;

        public ShopController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View(products);
        }
    }
}
