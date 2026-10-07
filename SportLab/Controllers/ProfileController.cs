using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View("Client");
        }

        public IActionResult Client()
        {
            return View();
        }

        public IActionResult Trainer()
        {
            return View();
        }
    }
}
