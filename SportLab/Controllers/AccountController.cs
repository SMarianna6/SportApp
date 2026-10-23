using Microsoft.AspNetCore.Mvc;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    public class AccountController : Controller
    {
        private readonly SportLabContext _context;

        public AccountController(SportLabContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            ViewBag.EnteredEmail = email;

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || user.Password != password)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View();
            }

            if (user.IsBlocked)
            {
                ModelState.AddModelError(string.Empty, "Your account is blocked.");
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role);

            if (user.Role != null && user.Role.ToLower() == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (user.Role != null && user.Role.ToLower() == "trainer")
            {
                return View("~/Views/Profile/Trainer.cshtml", user);
            }
            else
            {
                return View("~/Views/Profile/Client.cshtml", user);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string email, string password, string code, string action)
        {
            ViewBag.Email = email;
            ViewBag.Password = password;

            if (action == "send")
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError(string.Empty, "All fields are required.");
                    return View();
                }

                var random = new Random();
                string generatedCode = random.Next(100000, 999999).ToString();

                HttpContext.Session.SetString("RegEmail", email);
                HttpContext.Session.SetString("RegPassword", password);
                HttpContext.Session.SetString("RegCode", generatedCode);

                TempData["GeneratedCode"] = generatedCode;

                return View();
            }

            if (action == "verify")
            {
                string savedCode = HttpContext.Session.GetString("RegCode");
                string savedEmail = HttpContext.Session.GetString("RegEmail");
                string savedPassword = HttpContext.Session.GetString("RegPassword");

                if (string.IsNullOrEmpty(savedCode) || code != savedCode || email != savedEmail)
                {
                    TempData["GeneratedCode"] = savedCode;
                    return View();
                }

                var newUser = new User
                {
                    Name = email.Split('@')[0],
                    Email = savedEmail,
                    Password = savedPassword,
                    Role = "Client",
                    IsBlocked = false
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                HttpContext.Session.Remove("RegCode");
                HttpContext.Session.Remove("RegEmail");
                HttpContext.Session.Remove("RegPassword");
                TempData.Remove("GeneratedCode");

                return RedirectToAction("Login");
            }

            return View();
        }
    }
}