using Microsoft.AspNetCore.Mvc;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    public class FAQController : Controller
    {
        private readonly SportLabContext _context;

        public FAQController(SportLabContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (!_context.FAQs.Any())
            {
                SeedDefaultFAQs();
            }

            var faqs = _context.FAQs
                .Where(f => f.IsActive)
                .OrderBy(f => f.SortOrder)
                .ToList();

            return View(faqs);
        }

        private void SeedDefaultFAQs()
        {
            var defaultFaqs = new List<FAQ>
            {
                new FAQ
                {
                    Question = "What are SportLab's operating hours?",
                    Answer = "Our facility is open Monday through Sunday from 07:00 AM to 11:00 PM for standard members. Unlimited Pro and VIP members enjoy 24/7 access 365 days a year via smart RFID keycards and mobile NFC entry.",
                    SortOrder = 1,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Can I pause or freeze my membership while traveling?",
                    Answer = "Yes! All Unlimited Pro plans include up to 7 days of complimentary membership freeze, and Annual VIP plans include up to 30 days. You can easily activate the freeze directly in your online profile or by contacting reception.",
                    SortOrder = 2,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Do I need to bring my own towel and locker padlock?",
                    Answer = "No, complimentary freshly sanitized towels are provided at the front desk upon every check-in. Our locker rooms feature secure digital keycode lockers, so you do not need to bring a padlock.",
                    SortOrder = 3,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "Are group fitness classes included in the membership?",
                    Answer = "Group classes (CrossFit, TRX, Yoga, Pilates, and Spinning) are fully included in the Unlimited Pro and VIP memberships. Basic membership holders can purchase single-class drop-ins or upgrade anytime.",
                    SortOrder = 4,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "How do I book an introductory session with a personal trainer?",
                    Answer = "Every new member receives a complimentary 60-minute orientation workout and InBody body composition analysis. You can schedule this in the Workout Schedule tab or directly with any of our certified coaches.",
                    SortOrder = 5,
                    IsActive = true
                },
                new FAQ
                {
                    Question = "What is the return/exchange policy for store merchandise?",
                    Answer = "Any unopened nutrition supplement or unworn apparel item purchased through our store or front desk can be exchanged or returned for a full refund within 14 days with receipt.",
                    SortOrder = 6,
                    IsActive = true
                }
            };

            _context.FAQs.AddRange(defaultFaqs);
            _context.SaveChanges();
        }
    }
}
