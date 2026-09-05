using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;
using AssetFlow.Services;

namespace AssetFlow.Controllers
{
    // The IT side of the request flow. Approving a request is what actually checks
    // the asset out, so the manual checkout screen and this one end up writing the
    // same fields on the asset row.
    [Authorize(Roles = "Admin")]
    public class RequestQueueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notifications;

        public RequestQueueController(ApplicationDbContext context, NotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // GET: RequestQueue
        public async Task<IActionResult> Index(string status = "Pending")
        {
            var requests = _context.AssetRequests
                .Include(r => r.Asset)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                requests = requests.Where(r => r.Status == status);
            }

            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = await _context.AssetRequests.CountAsync(r => r.Status == "Pending");
            ViewBag.ApprovedCount = await _context.AssetRequests.CountAsync(r => r.Status == "Approved");
            ViewBag.RejectedCount = await _context.AssetRequests.CountAsync(r => r.Status == "Rejected");

            // Oldest first for the pending queue - whoever has been waiting longest
            // should be dealt with first.
            var ordered = status == "Pending"
                ? requests.OrderBy(r => r.RequestedOn)
                : requests.OrderByDescending(r => r.RequestedOn);

            return View(await ordered.ToListAsync());
        }

        // GET: Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.AssetRequests
                .Include(r => r.Asset)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Other open requests for the same asset. Only one of them can win, and
            // the admin should be able to see that before deciding.
            ViewBag.CompetingRequests = await _context.AssetRequests
                .Where(r => r.AssetId == request.AssetId && r.Id != request.Id && r.Status == "Pending")
                .CountAsync();

            return View(request);
        }

        // GET: Approve
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.AssetRequests
                .Include(r => r.Asset)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] = $"That request is already marked {request.Status}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(request);
        }

        // POST: Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, DateTime expectedReturnDate, string? reviewNotes)
        {
            var request = await _context.AssetRequests
                .Include(r => r.Asset)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] = $"That request is already marked {request.Status}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var asset = request.Asset;

            if (asset == null)
            {
                return NotFound();
            }

            // Somebody may have checked the asset out by hand while the request sat in
            // the queue, so availability gets re-checked at the moment of approval.
            if (asset.Status != "Available")
            {
                TempData["ErrorMessage"] = $"'{asset.Name}' is {asset.Status} and cannot be handed over. Reject the request or free the asset up first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                asset.CheckedOutToEmployee = request.RequesterName;
                asset.EmployeeEmail = request.RequesterEmail;
                asset.EmployeeDepartment = request.RequesterDepartment;
                asset.CheckoutDate = DateTime.Now;
                asset.ExpectedReturnDate = expectedReturnDate;
                asset.CheckoutNotes = request.Reason;
                asset.ActualReturnDate = null;
                asset.Status = "CheckedOut";
                asset.LastUpdated = DateTime.Now;

                request.Status = "Approved";
                request.ReviewedOn = DateTime.Now;
                request.ReviewedBy = User.Identity?.Name;
                request.ReviewNotes = reviewNotes;

                await _context.SaveChangesAsync();

                await _notifications.NotifyRequesterApprovedAsync(request, asset.Name, expectedReturnDate);

                TempData["SuccessMessage"] = $"Approved. '{asset.Name}' is now checked out to {request.RequesterName}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Could not approve the request: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reviewNotes)
        {
            var request = await _context.AssetRequests
                .Include(r => r.Asset)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] = $"That request is already marked {request.Status}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // A rejection without a reason is useless to the person who asked.
            if (string.IsNullOrWhiteSpace(reviewNotes))
            {
                TempData["ErrorMessage"] = "Give a reason for the rejection so the requester knows why.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.Status = "Rejected";
            request.ReviewedOn = DateTime.Now;
            request.ReviewedBy = User.Identity?.Name;
            request.ReviewNotes = reviewNotes;

            await _context.SaveChangesAsync();

            await _notifications.NotifyRequesterRejectedAsync(request, request.Asset?.Name ?? "the equipment", reviewNotes);

            TempData["SuccessMessage"] = $"Request from {request.RequesterName} rejected.";
            return RedirectToAction(nameof(Index));
        }
    }
}
