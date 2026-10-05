using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
