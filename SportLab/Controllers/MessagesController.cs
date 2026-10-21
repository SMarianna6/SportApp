using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
