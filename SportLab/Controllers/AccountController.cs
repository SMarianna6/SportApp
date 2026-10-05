using Microsoft.AspNetCore.Mvc;

namespace SportLab.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
