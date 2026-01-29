using AssetFlow.Data;
using AssetFlow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AssetFlow.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
               
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
                    OverdueAssets = overdueAssets
                };

                return View(viewModel);
            }

            return View();
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
    }
}