using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Client()
        {
            return View();
        }

        public IActionResult Trainer()
        {
            return View();
        }

        public IActionResult Clients()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Progress()
        {
            return View();
        }
    }
}
