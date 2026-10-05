using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
