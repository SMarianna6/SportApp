using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class ShopController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
