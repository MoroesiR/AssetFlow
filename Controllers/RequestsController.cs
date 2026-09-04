using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;

namespace AssetFlow.Controllers
{
    // The employee side of the house. Every action here is scoped to the signed in
    // user, so nobody sees anyone else's requests from these pages.
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Requests
        public async Task<IActionResult> Index(string status)
        {
            var userId = _userManager.GetUserId(User);

            var requests = _context.AssetRequests
                .Include(r => r.Asset)
                .Where(r => r.RequesterId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                requests = requests.Where(r => r.Status == status);
            }

            ViewBag.SelectedStatus = status;

            return View(await requests
                .OrderByDescending(r => r.RequestedOn)
                .ToListAsync());
        }

        // GET: Browse
        public async Task<IActionResult> Browse(string search, string category)
        {
            var assets = _context.Assets.Where(a => a.Status == "Available");

            if (!string.IsNullOrEmpty(search))
            {
                assets = assets.Where(a =>
                    a.Name.Contains(search) ||
                    a.Category.Contains(search) ||
                    a.Location.Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                assets = assets.Where(a => a.Category == category);
            }

            var userId = _userManager.GetUserId(User);

            // Assets this user already has in the queue, so the view can grey them out
            // instead of letting duplicate requests pile up.
            ViewBag.AlreadyRequested = await _context.AssetRequests
                .Where(r => r.RequesterId == userId && r.Status == "Pending")
                .Select(r => r.AssetId)
                .ToListAsync();

            ViewBag.SearchTerm = search;
            ViewBag.SelectedCategory = category;

            return View(await assets.OrderBy(a => a.Name).ToListAsync());
        }

        // GET: MyEquipment
        public async Task<IActionResult> MyEquipment()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Checkouts are recorded against the employee email, which is the same
            // value Identity uses for the username.
            var assets = await _context.Assets
                .Where(a => a.Status == "CheckedOut" && a.EmployeeEmail == user.Email)
                .OrderBy(a => a.ExpectedReturnDate)
                .ToListAsync();

            return View(assets);
        }

        // GET: Create
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            if (asset.Status != "Available")
            {
                TempData["ErrorMessage"] = $"'{asset.Name}' is no longer available to request.";
                return RedirectToAction(nameof(Browse));
            }

            var request = new AssetRequest
            {
                AssetId = asset.Id,
                Asset = asset
            };

            return View(request);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int assetId, DateTime neededFrom, DateTime neededUntil, string reason)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var request = new AssetRequest
            {
                AssetId = assetId,
                Asset = asset,
                RequesterId = user.Id,
                RequesterName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName,
                RequesterEmail = user.Email,
                RequesterDepartment = user.Department,
                NeededFrom = neededFrom,
                NeededUntil = neededUntil,
                Reason = reason,
                Status = "Pending",
                RequestedOn = DateTime.Now
            };

            if (asset.Status != "Available")
            {
                ModelState.AddModelError(string.Empty, $"'{asset.Name}' has been taken since you opened this page.");
            }

            if (neededUntil.Date <= neededFrom.Date)
            {
                ModelState.AddModelError("neededUntil", "The return date has to be after the start date.");
            }

            if (neededFrom.Date < DateTime.Today)
            {
                ModelState.AddModelError("neededFrom", "You cannot request equipment for a date that has already passed.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError("reason", "Please say what you need it for.");
            }

            // One open request per person per asset. Without this, a double click on
            // the submit button puts the same request in the queue twice.
            var duplicate = await _context.AssetRequests
                .AnyAsync(r => r.AssetId == assetId && r.RequesterId == user.Id && r.Status == "Pending");

            if (duplicate)
            {
                ModelState.AddModelError(string.Empty, "You already have a pending request for this asset.");
            }

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            _context.AssetRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Request submitted for '{asset.Name}'. IT will review it shortly.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);

            var request = await _context.AssetRequests
                .Include(r => r.Asset)
                .FirstOrDefaultAsync(r => r.Id == id && r.RequesterId == userId);

            if (request == null)
            {
                return NotFound();
            }

            if (!request.CanBeCancelled)
            {
                TempData["ErrorMessage"] = "That request has already been reviewed, so it cannot be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = "Cancelled";
            request.ReviewedOn = DateTime.Now;
            request.ReviewedBy = request.RequesterName;
            request.ReviewNotes = "Cancelled by requester";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Request for '{request.Asset?.Name}' cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
