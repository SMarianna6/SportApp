using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class TrainerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
