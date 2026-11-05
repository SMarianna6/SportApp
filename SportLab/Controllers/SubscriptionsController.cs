using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SportLab.Controllers
{
    public class SubscriptionsController : Controller
    {
        private readonly SportLabContext _context;

        public SubscriptionsController(SportLabContext context)
        {
            _context = context;
        }

        [Authorize(Roles = Roles.ClientRoles)]
        public async Task<IActionResult> My()
        {
            var userId = CurrentUserId();

            var subscriptions = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            return View(subscriptions);
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    }
}
