using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SportLab.Data;
using SportLab.Models;
using SportLab.Services;
using System.Security.Claims;
using MimeKit;

namespace SportLab.Controllers
{
    public class TariffController : Controller
    {
        private readonly SportLabContext _context;
        private readonly IStringLocalizer<TariffController> _localizer;
        private readonly IEmailSender _emailSender;

        public TariffController(SportLabContext context, IStringLocalizer<TariffController> localizer, IEmailSender emailSender)
        {
            _context = context;
            _localizer = localizer;
            _emailSender = emailSender;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            if (!_context.Plans.Any())
            {
                SeedDefaultTariffs();
            }

            bool isAuthenticated = User.Identity?.IsAuthenticated == true;
            ViewBag.IsAuthenticated = isAuthenticated;

            if (isAuthenticated)
            {
                var userId = CurrentUserId();
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    ViewBag.CurrentUserName = user.Name;
                    ViewBag.CurrentUserEmail = user.Email;
                }
            }

            var tariffs = _context.Plans.ToList();
            return View(tariffs);
        }

        [Authorize(Roles = Roles.ClientRoles)]
        public IActionResult ConfirmPurchase(int id)
        {
            var tariff = _context.Plans.Find(id);
            if (tariff == null)
            {
                return NotFound();
            }

            var userId = CurrentUserId();
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                ViewBag.CurrentUserName = user.Name;
                ViewBag.CurrentUserEmail = user.Email;
            }

            ViewBag.StartDate = DateTime.Today;
            ViewBag.EndDate = DateTime.Today.AddDays(tariff.Duration > 0 ? tariff.Duration : 30);
            return View(tariff);
        }

        [Authorize(Roles = Roles.ClientRoles)]
        [HttpPost]
        public async Task<IActionResult> QuickPurchase([FromBody] QuickPurchaseRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = _localizer["InvalidRequestData"].Value });
            }

            if (request == null || request.TariffId <= 0)
            {
                return Json(new { success = false, message = _localizer["InvalidPayload"].Value });
            }

            var tariff = _context.Plans.Find(request.TariffId);
            if (tariff == null)
            {
                return Json(new { success = false, message = _localizer["PlanNotFound"].Value });
            }

            var startDate = request.StartDate ?? DateTime.Today;
            var endDate = startDate.AddDays(tariff.Duration > 0 ? tariff.Duration : 30);

            var userId = CurrentUserId();
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return Json(new { success = false, message = _localizer["SignInRequired"].Value });
            }

            DeactivateActiveSubscriptions(userId);

            var subscription = new Subscription
            {
                UserId = user.Id,
                TariffId = tariff.Id,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true
            };

            _context.Subscriptions.Add(subscription);
            _context.SaveChanges();

            try
            {
                var mailMessage = new MimeMessage
                {
                    Subject = _localizer["EmailSubject", tariff.Name].Value
                };

                mailMessage.To.Add(MailboxAddress.Parse(user.Email));

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f8f9fa; padding: 20px; border-radius: 10px;'>
                        <div style='background: linear-gradient(135deg, #0d6efd, #0dcaf0); padding: 30px; text-align: center; border-radius: 8px 8px 0 0; color: white;'>
                            <h1 style='margin: 0; font-size: 24px;'>{_localizer["EmailWelcomeTitle"]}</h1>
                            <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>{_localizer["EmailWelcomeSubtitle"]}</p>
                        </div>
                        <div style='background: white; padding: 30px; border-radius: 0 0 8px 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                            <h2 style='color: #333; font-size: 20px; margin-top: 0;'>{_localizer["EmailHello", user.Name]}</h2>
                            <p style='color: #555; line-height: 1.5;'>
                                {_localizer["EmailIntro"]}
                            </p>
                            <div style='background: #f1f3f5; padding: 15px 20px; border-radius: 6px; margin: 20px 0;'>
                                <p style='margin: 5px 0; color: #333;'><strong>{_localizer["EmailPlan"]}</strong> {tariff.Name}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>{_localizer["EmailDuration"]}</strong> {_localizer["EmailDurationValue", tariff.Duration]}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>{_localizer["EmailValidity"]}</strong> {startDate:dd.MM.yyyy} — {endDate:dd.MM.yyyy}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>{_localizer["EmailTotalPaid"]}</strong> {tariff.Price:N0} ₴</p>
                            </div>
                            <p style='color: #555; line-height: 1.5;'>
                                {_localizer["EmailProfileNote"]}
                            </p>
                        </div>
                        <div style='text-align: center; margin-top: 20px; color: #adb5bd; font-size: 12px;'>
                            <p>{_localizer["EmailRights", DateTime.Now.Year]}</p>
                        </div>
                    </div>";

                mailMessage.Body = bodyBuilder.ToMessageBody();

                await _emailSender.SendAsync(mailMessage, _localizer["EmailFromName"].Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
            }

            return Json(new
            {
                success = true,
                subscriptionId = subscription.Id,
                tariffName = tariff.Name,
                price = tariff.Price,
                startDate = startDate.ToString("dd.MM.yyyy"),
                endDate = endDate.ToString("dd.MM.yyyy"),
                userName = user.Name,
                message = _localizer["SuccessMessage"].Value
            });
        }

        [Authorize(Roles = Roles.ClientRoles)]
        public IActionResult PurchaseSuccess(int id)
        {
            var subscription = _context.Subscriptions
                .Include(s => s.Tariff)
                .Include(s => s.User)
                .FirstOrDefault(s => s.Id == id);

            if (subscription == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsAdmin() && subscription.UserId != CurrentUserId())
            {
                return Forbid();
            }

            return View(subscription);
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private void DeactivateActiveSubscriptions(int userId)
        {
            var active = _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToList();

            foreach (var subscription in active)
            {
                subscription.IsActive = false;
            }
        }

        private void SeedDefaultTariffs()
        {
            var initialTariffs = new List<Plan>
            {
                new Plan
                {
                    Name = "Basic Start",
                    Type = "Basic",
                    Price = 850,
                    Duration = 30,
                    Description = "Full gym access (07:00 - 16:00)\nIntroductory coach orientation\nPersonal locker & shower facilities\nMobile workout tracking app\nComplimentary fresh drinking water"
                },
                new Plan
                {
                    Name = "Unlimited Pro",
                    Type = "Popular",
                    Price = 1450,
                    Duration = 30,
                    Description = "24/7 Unlimited access to all workout zones\nAll group classes included (CrossFit, TRX, Yoga)\nSpa & Recovery: Finnish sauna & jacuzzi\n2 Free guest passes per month\nFree membership freeze up to 7 days"
                },
                new Plan
                {
                    Name = "Student Drive",
                    Type = "Student",
                    Price = 650,
                    Duration = 30,
                    Description = "Gym access between 09:00 - 18:00\nGroup functional training sessions\nModern comfortable changing rooms\nValid with active student ID\n10% discount at the SportHub nutrition bar"
                },
                new Plan
                {
                    Name = "Annual VIP Unlimited",
                    Type = "Premium",
                    Price = 11900,
                    Duration = 365,
                    Description = "All-inclusive 365-day pass with no restrictions\n5 One-on-one sessions with a master trainer\nDedicated personal locker & fresh towel daily\nCertified doctor & nutritionist assessment\nUp to 30 days membership freeze protection\n15% discount at the SportHub store"
                }
            };

            _context.Plans.AddRange(initialTariffs);
            _context.SaveChanges();
        }
    }

    public class QuickPurchaseRequest
    {
        public int TariffId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? StartDate { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
