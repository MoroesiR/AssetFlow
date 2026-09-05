using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;
using AssetFlow.Services;

namespace AssetFlow.Controllers
{
    // The bell menu. Both roles use the same list - what differs is only what the
    // sweep puts in it.
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notifications;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notifications)
        {
            _context = context;
            _userManager = userManager;
            _notifications = notifications;
        }

        // GET: Notifications
        public async Task<IActionResult> Index(string filter = "All")
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            // Opening the list is what triggers the overdue and maintenance sweep. No
            // background service in this project, and a nightly job would be overkill
            // for something only read when somebody looks at it.
            await _notifications.SweepSystemNoticesAsync(userId, User.IsInRole(DbSeeder.AdminRole));

            var mine = _context.Notifications.Where(n => n.UserId == userId);

            if (filter == "Unread")
            {
                mine = mine.Where(n => !n.IsRead);
            }

            ViewBag.Filter = filter;
            ViewBag.UnreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return View(await mine.OrderByDescending(n => n.CreatedOn).ToListAsync());
        }

        // GET: Open - marks read on the way through, then sends you where it points.
        // Doing it on a GET is normally wrong, but the whole purpose of following a
        // notification is that you have now seen it.
        public async Task<IActionResult> Open(int id)
        {
            var userId = _userManager.GetUserId(User);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadOn = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            if (string.IsNullOrWhiteSpace(notification.Link))
            {
                return RedirectToAction(nameof(Index));
            }

            // Only ever redirect inside this app. The link is written by our own code,
            // but a stored value that turns into a redirect gets checked anyway.
            if (!Url.IsLocalUrl(notification.Link))
            {
                return RedirectToAction(nameof(Index));
            }

            return Redirect(notification.Link);
        }

        // POST: MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _userManager.GetUserId(User);

            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
                notification.ReadOn = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = unread.Count == 0
                ? "Nothing was unread."
                : $"Marked {unread.Count} notification(s) as read.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Clear - drops everything already read. Unread items stay put so this
        // cannot be used to lose something you have not looked at yet.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearRead()
        {
            var userId = _userManager.GetUserId(User);

            var read = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead)
                .ToListAsync();

            _context.Notifications.RemoveRange(read);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cleared {read.Count} read notification(s).";

            return RedirectToAction(nameof(Index));
        }
    }
}
