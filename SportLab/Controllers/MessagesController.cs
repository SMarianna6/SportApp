using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportLab.Data;
using SportLab.Models;

namespace SportLab.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly SportLabContext _context;

        public MessagesController(SportLabContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId();
            var sportLabContext = _context.Messages
                .Include(m => m.Receiver)
                .Include(m => m.Sender)
                .Where(m => User.IsAdmin() || m.SenderId == userId || m.ReceiverId == userId);
            return View(await sportLabContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages
                .Include(m => m.Receiver)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        public IActionResult Create()
        {
            ViewData["ReceiverId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SenderId,ReceiverId,Text,SentDate")] Message message)
        {
            if (!User.IsAdmin())
            {
                message.SenderId = CurrentUserId();
            }

            if (ModelState.IsValid)
            {
                _context.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReceiverId"] = new SelectList(_context.Users, "Id", "Email", message.ReceiverId);
            ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Email", message.SenderId);
            return View(message);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }
            ViewData["ReceiverId"] = new SelectList(_context.Users, "Id", "Email", message.ReceiverId);
            ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Email", message.SenderId);
            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SenderId,ReceiverId,Text,SentDate")] Message message)
        {
            if (id != message.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(message);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MessageExists(message.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReceiverId"] = new SelectList(_context.Users, "Id", "Email", message.ReceiverId);
            ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Email", message.SenderId);
            return View(message);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Messages
                .Include(m => m.Receiver)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                _context.Messages.Remove(message);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MessageExists(int id)
        {
            return _context.Messages.Any(e => e.Id == id);
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
