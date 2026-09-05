using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;

namespace AssetFlow.Services
{
    // Everything that raises a notification goes through here. Keeping the wording in
    // one place means the request queue and the employee pages cannot drift apart on
    // how the same event is described.
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<int> UnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        // Called by the employee side when a request is submitted. Every admin gets a
        // copy so read state stays personal to whoever actually looked at it.
        public async Task NotifyAdminsOfNewRequestAsync(AssetRequest request, string assetName)
        {
            var admins = await _userManager.GetUsersInRoleAsync(DbSeeder.AdminRole);

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Category = "Request",
                    Title = "New equipment request",
                    Message = $"{request.RequesterName} ({request.RequesterDepartment}) asked for {assetName} from {request.NeededFrom:dd MMM} to {request.NeededUntil:dd MMM}.",
                    Link = $"/RequestQueue/Details/{request.Id}"
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task NotifyAdminsOfCancellationAsync(AssetRequest request, string assetName)
        {
            var admins = await _userManager.GetUsersInRoleAsync(DbSeeder.AdminRole);

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Category = "Request",
                    Title = "Request withdrawn",
                    Message = $"{request.RequesterName} cancelled their request for {assetName}.",
                    Link = "/RequestQueue"
                });
            }

            await _context.SaveChangesAsync();
        }

        // The decision notices. The rejection reason is repeated in the message itself
        // because that is the whole point of the notification for the requester.
        public async Task NotifyRequesterApprovedAsync(AssetRequest request, string assetName, DateTime returnBy)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = request.RequesterId,
                Category = "Decision",
                Title = "Request approved",
                Message = $"{assetName} is ready for collection from the IT department. It is due back on {returnBy:dd MMM yyyy}.",
                Link = "/Requests/MyEquipment"
            });

            await _context.SaveChangesAsync();
        }

        public async Task NotifyRequesterRejectedAsync(AssetRequest request, string assetName, string reason)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = request.RequesterId,
                Category = "Decision",
                Title = "Request declined",
                Message = $"Your request for {assetName} was declined. {reason}",
                Link = "/Requests"
            });

            await _context.SaveChangesAsync();
        }

        // Overdue and maintenance notices have no user action behind them, so there is
        // nothing to hang them off. They get raised by a sweep that runs when somebody
        // opens the bell menu. SourceKey carries the date so each item is reported once
        // a day rather than once a page load.
        public async Task SweepSystemNoticesAsync(string userId, bool isAdmin)
        {
            if (!isAdmin)
            {
                await SweepEmployeeOverdueAsync(userId);
                return;
            }

            var today = DateTime.Today;

            var overdue = await _context.Assets
                .Where(a => a.Status == "CheckedOut"
                            && a.ExpectedReturnDate != null
                            && a.ExpectedReturnDate < today)
                .ToListAsync();

            foreach (var asset in overdue)
            {
                var key = $"overdue-{asset.Id}-{today:yyyy-MM-dd}";

                if (await ExistsAsync(userId, key))
                {
                    continue;
                }

                var daysLate = (today - asset.ExpectedReturnDate!.Value.Date).Days;

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Category = "Overdue",
                    Title = "Equipment overdue",
                    Message = $"{asset.Name} is {daysLate} day(s) overdue from {asset.CheckedOutToEmployee}.",
                    Link = $"/Assets/Details/{asset.Id}",
                    SourceKey = key
                });
            }

            var dueMaintenance = await _context.Assets
                .Where(a => a.RequiresMaintenance
                            || (a.NextMaintenanceDue != null && a.NextMaintenanceDue < today))
                .ToListAsync();

            foreach (var asset in dueMaintenance)
            {
                var key = $"maintenance-{asset.Id}-{today:yyyy-MM-dd}";

                if (await ExistsAsync(userId, key))
                {
                    continue;
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Category = "Maintenance",
                    Title = "Maintenance due",
                    Message = $"{asset.Name} ({asset.SerialNumber}) is flagged for servicing.",
                    Link = $"/Assets/Details/{asset.Id}",
                    SourceKey = key
                });
            }

            await _context.SaveChangesAsync();
        }

        // An employee only ever hears about their own late equipment, and the reminder
        // is worded as a nudge rather than the admin chasing it up.
        private async Task SweepEmployeeOverdueAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user?.Email == null)
            {
                return;
            }

            var today = DateTime.Today;

            var mine = await _context.Assets
                .Where(a => a.Status == "CheckedOut"
                            && a.EmployeeEmail == user.Email
                            && a.ExpectedReturnDate != null
                            && a.ExpectedReturnDate < today)
                .ToListAsync();

            foreach (var asset in mine)
            {
                var key = $"myoverdue-{asset.Id}-{today:yyyy-MM-dd}";

                if (await ExistsAsync(userId, key))
                {
                    continue;
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Category = "Overdue",
                    Title = "Equipment due back",
                    Message = $"{asset.Name} was due on {asset.ExpectedReturnDate!.Value:dd MMM yyyy}. Please return it to the IT department.",
                    Link = "/Requests/MyEquipment",
                    SourceKey = key
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> ExistsAsync(string userId, string sourceKey)
        {
            return await _context.Notifications
                .AnyAsync(n => n.UserId == userId && n.SourceKey == sourceKey);
        }
    }
}
