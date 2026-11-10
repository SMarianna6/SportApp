using System.Security.Claims;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using SportLab.Data;
using SportLab.Models;
using SportLab.Services;

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
        public async Task<IActionResult> Login(string email, string password)
        {
            ViewBag.EnteredEmail = email;

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !PasswordHasher.Verify(password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View();
            }

            if (!PasswordHasher.IsHashed(user.Password))
            {
                user.Password = PasswordHasher.Hash(password);
                await _context.SaveChangesAsync();
            }

            if (user.IsBlocked)
            {
                ModelState.AddModelError(string.Empty, "Your account is blocked.");
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", Roles.Normalize(user.Role));

            var role = Roles.Normalize(user.Role);

            var claims = new List<Claim> //Lab6 cookie auth by claims
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (role == Roles.Administrator)
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (role == Roles.Trainer)
            {
                return RedirectToAction("Trainer", "Profile");
            }
            else if (role == Roles.Moderator)
            {
                return RedirectToAction("Index", "Moderation");
            }
            else
            {
                return RedirectToAction("Client", "Profile");
            }
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                return RedirectToAction("Login");
            }

            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            var name = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email not received from Google.");
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            bool isNewUser = false;

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Name = name ?? email.Split('@')[0],
                    Password = PasswordHasher.Hash(Guid.NewGuid().ToString()),
                    Role = Roles.Client,
                    IsBlocked = false
                };

                _context.Users.Add(user);
                _context.SaveChanges();
                isNewUser = true;
            }

            if (user.IsBlocked)
            {
                return RedirectToAction("Login");
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", Roles.Normalize(user.Role));

            var role = Roles.Normalize(user.Role);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (isNewUser)
            {
                return RedirectToAction("CompleteProfile", "Account");
            }

            if (role == Roles.Administrator)
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (role == Roles.Trainer)
            {
                return RedirectToAction("Trainer", "Profile");
            }
            else if (role == Roles.Moderator)
            {
                return RedirectToAction("Index", "Moderation");
            }
            else
            {
                return RedirectToAction("Client", "Profile");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string? code, string action)
        {
            ViewBag.Email = email;
            ViewBag.Password = password;

            if (action == "send")
            {
                ModelState.Remove("code");

                if (!ModelState.IsValid || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError(string.Empty, "All fields are required.");
                    return View();
                }

                var random = new Random();
                string generatedCode = random.Next(100000, 999999).ToString();

                HttpContext.Session.SetString("RegEmail", email);
                HttpContext.Session.SetString("RegPassword", password);
                HttpContext.Session.SetString("RegCode", generatedCode);

                try
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("SportHub Support", "sporthubproject01@gmail.com"));
                    message.To.Add(new MailboxAddress("", email));
                    message.Subject = "Registration Verification Code - SportHub";

                    message.Body = new TextPart("plain")
                    {
                        Text = $"Welcome to SportHub!\n\nYour registration verification code is: {generatedCode}"
                    };

                    using (var client = new SmtpClient())
                    {
                        client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                        client.Authenticate("sporthubproject01@gmail.com", "zcfe yyad ezet tirb");
                        client.Send(message);
                        client.Disconnect(true);
                    }

                    ViewBag.Message = "Verification code has been successfully sent to your email!";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error sending email: " + ex.Message);
                    return View();
                }

                return View();
            }

            if (action == "verify")
            {
                string savedCode = HttpContext.Session.GetString("RegCode");
                string savedEmail = HttpContext.Session.GetString("RegEmail");
                string savedPassword = HttpContext.Session.GetString("RegPassword");

                if (string.IsNullOrEmpty(savedCode) || code != savedCode || email != savedEmail)
                {
                    ModelState.AddModelError(string.Empty, "Invalid verification code or email.");
                    return View();
                }

                if (_context.Users.Any(u => u.Email == savedEmail))
                {
                    ModelState.AddModelError(string.Empty, "This email is already registered.");
                    return View();
                }

                var newUser = new User
                {
                    Name = email.Split('@')[0],
                    Email = savedEmail,
                    Password = PasswordHasher.Hash(savedPassword),
                    Role = Roles.Client,
                    IsBlocked = false
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                HttpContext.Session.Remove("RegCode");
                HttpContext.Session.Remove("RegEmail");
                HttpContext.Session.Remove("RegPassword");

                HttpContext.Session.SetInt32("UserId", newUser.Id);
                HttpContext.Session.SetString("UserRole", Roles.Client);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, newUser.Name ?? newUser.Email),
                    new Claim(ClaimTypes.Email, newUser.Email),
                    new Claim(ClaimTypes.Role, Roles.Client)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("CompleteProfile", "Account");
            }

            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult CompleteProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = _context.Users.Find(userId);
            return View(user);
        }

        [HttpPost]
        [Authorize]
        public IActionResult CompleteProfile(string? name, string? avatarUrl, float? initialWeight)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = _context.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }


            if (!string.IsNullOrWhiteSpace(name))
            {
                user.Name = name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                user.AvatarUrl = avatarUrl.Trim();
            }

            if (initialWeight.HasValue && initialWeight.Value >= 20 && initialWeight.Value <= 300)
            {
                var metricLog = new MetricLog
                {
                    UserId = user.Id,
                    Weight = initialWeight.Value,
                    RecordDate = DateTime.Now,
                    Notes = "Initial weight"
                };
                _context.MetricLogs.Add(metricLog);
            }

            _context.SaveChanges();

            return RedirectToAction("Client", "Profile");
        }
    }
}
