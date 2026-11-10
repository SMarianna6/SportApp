using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using System.Security.Claims;

namespace SportLab.ViewComponents
{
    public class UserAvatarViewComponent : ViewComponent
    {
        private readonly SportLabContext _context;

        public UserAvatarViewComponent(SportLabContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdStr = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Content("");

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return View(user);
        }
    }
}