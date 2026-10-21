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
<<<<<<< HEAD
        public IActionResult Progress()
=======

        public IActionResult Schedule()
>>>>>>> 9a8850d4176e8c55ab3dca41d5deb484bb40ac55
        {
            return View();
        }
    }
}
