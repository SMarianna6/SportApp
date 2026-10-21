using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class ReviewController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
