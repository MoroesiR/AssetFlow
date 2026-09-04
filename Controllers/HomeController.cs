using AssetFlow.Data;
using AssetFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AssetFlow.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Two very different landing pages behind the one URL. An employee has
                // no business seeing inventory totals or the maintenance backlog.
                if (!User.IsInRole("Admin"))
                {
                    return View("Employee", await BuildEmployeeViewModel());
                }

                var recentAssets = await _context.Assets
                    .OrderByDescending(a => a.LastUpdated)
                    .Take(5)
                    .ToListAsync();

                var totalAssets = await _context.Assets.CountAsync();
                var availableAssets = await _context.Assets.CountAsync(a => a.Status == "Available");
                var checkedOutAssets = await _context.Assets.CountAsync(a => a.Status == "CheckedOut");
                var maintenanceAssets = await _context.Assets.CountAsync(a => a.Status == "Maintenance");
                var overdueAssets = await _context.Assets.CountAsync(a =>
                    a.Status == "CheckedOut" &&
                    a.ExpectedReturnDate.HasValue &&
                    a.ExpectedReturnDate.Value < DateTime.Today);

                var viewModel = new HomeViewModel
                {
                    RecentAssets = recentAssets,
                    TotalAssets = totalAssets,
                    AvailableAssets = availableAssets,
                    CheckedOutAssets = checkedOutAssets,
                    MaintenanceAssets = maintenanceAssets,
                    OverdueAssets = overdueAssets,
                    PendingRequests = await _context.AssetRequests.CountAsync(r => r.Status == "Pending"),
                    OldestPendingRequest = await _context.AssetRequests
                        .Where(r => r.Status == "Pending")
                        .OrderBy(r => r.RequestedOn)
                        .Select(r => (DateTime?)r.RequestedOn)
                        .FirstOrDefaultAsync()
                };

                return View(viewModel);
            }

            return View();
        }

        private async Task<EmployeeHomeViewModel> BuildEmployeeViewModel()
        {
            var user = await _userManager.GetUserAsync(User);

            var myRequests = await _context.AssetRequests
                .Include(r => r.Asset)
                .Where(r => r.RequesterId == user.Id)
                .OrderByDescending(r => r.RequestedOn)
                .Take(5)
                .ToListAsync();

            var myEquipment = await _context.Assets
                .Where(a => a.Status == "CheckedOut" && a.EmployeeEmail == user.Email)
                .OrderBy(a => a.ExpectedReturnDate)
                .ToListAsync();

            return new EmployeeHomeViewModel
            {
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName,
                Department = user.Department,
                AvailableAssets = await _context.Assets.CountAsync(a => a.Status == "Available"),
                PendingRequests = await _context.AssetRequests
                    .CountAsync(r => r.RequesterId == user.Id && r.Status == "Pending"),
                ApprovedRequests = await _context.AssetRequests
                    .CountAsync(r => r.RequesterId == user.Id && r.Status == "Approved"),
                MyRequests = myRequests,
                MyEquipment = myEquipment
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }


    public class HomeViewModel
    {
        public List<Asset> RecentAssets { get; set; } = new List<Asset>();
        public int TotalAssets { get; set; }
        public int AvailableAssets { get; set; }
        public int CheckedOutAssets { get; set; }
        public int MaintenanceAssets { get; set; }
        public int OverdueAssets { get; set; }
        public int PendingRequests { get; set; }
        public DateTime? OldestPendingRequest { get; set; }
    }

    public class EmployeeHomeViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public int AvailableAssets { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public List<AssetRequest> MyRequests { get; set; } = new List<AssetRequest>();
        public List<Asset> MyEquipment { get; set; } = new List<Asset>();
    }
}
