using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class ScheduleController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
