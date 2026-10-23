using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // GET: FAQ/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var fAQ = await _context.FAQs.FirstOrDefaultAsync(m => m.Id == id);
            if (fAQ == null) return NotFound();

            return View(fAQ);
        }

        // GET: FAQ/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FAQ/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Question,Answer,SortOrder,IsActive")] FAQ fAQ)
        {
            if (ModelState.IsValid)
            {
                _context.Add(fAQ);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fAQ);
        }

        // GET: FAQ/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var fAQ = await _context.FAQs.FindAsync(id);
            if (fAQ == null) return NotFound();

            return View(fAQ);
        }

        // POST: FAQ/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Question,Answer,SortOrder,IsActive")] FAQ fAQ)
        {
            if (id != fAQ.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fAQ);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FAQExists(fAQ.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(fAQ);
        }

        // GET: FAQ/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var fAQ = await _context.FAQs.FirstOrDefaultAsync(m => m.Id == id);
            if (fAQ == null) return NotFound();

            return View(fAQ);
        }

        // POST: FAQ/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fAQ = await _context.FAQs.FindAsync(id);
            if (fAQ != null) _context.FAQs.Remove(fAQ);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FAQExists(int id)
        {
            return _context.FAQs.Any(e => e.Id == id);
        }
    }
}
