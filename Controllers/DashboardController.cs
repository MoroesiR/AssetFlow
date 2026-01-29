using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;

namespace AssetFlow.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var assets = await _context.Assets.ToListAsync();

          
            var overdueAssets = assets.Where(a => a.Status == "CheckedOut" && a.IsOverdue).ToList();
            var maintenanceNeeded = assets.Where(a => a.RequiresMaintenance || a.Status == "Maintenance").ToList();
            var warrantyExpiring = assets.Where(a => a.WarrantyExpiry.HasValue &&
                                                    a.WarrantyExpiry.Value >= DateTime.Today &&
                                                    a.WarrantyExpiry.Value <= DateTime.Today.AddDays(30)).ToList();

            var viewModel = new DashboardViewModel
            {
             
                TotalAssets = assets.Count,
                AvailableAssets = assets.Count(a => a.Status == "Available"),
                CheckedOutAssets = assets.Count(a => a.Status == "CheckedOut"),
                MaintenanceAssets = assets.Count(a => a.Status == "Maintenance"),
                TotalAssetValue = assets.Sum(a => a.PurchasePrice),

         
                OverdueAssets = overdueAssets.Count,
                MaintenanceNeeded = maintenanceNeeded.Count,
                WarrantyExpiring = warrantyExpiring.Count,

             
                RecentAssets = assets.OrderByDescending(a => a.LastUpdated).Take(5).ToList(),
                OverdueAssetsList = overdueAssets,
                MaintenanceAssetsList = maintenanceNeeded.Take(5).ToList(),
                WarrantyExpiringList = warrantyExpiring.Take(5).ToList()
            };

            return View(viewModel);
        }
    }

    public class DashboardViewModel
    {
     
        public int TotalAssets { get; set; }
        public int AvailableAssets { get; set; }
        public int CheckedOutAssets { get; set; }
        public int MaintenanceAssets { get; set; }
        public decimal TotalAssetValue { get; set; }

        
        public int OverdueAssets { get; set; }
        public int MaintenanceNeeded { get; set; }
        public int WarrantyExpiring { get; set; }

      
        public System.Collections.Generic.List<AssetFlow.Models.Asset> RecentAssets { get; set; }
            = new System.Collections.Generic.List<AssetFlow.Models.Asset>();

        public System.Collections.Generic.List<AssetFlow.Models.Asset> OverdueAssetsList { get; set; }
            = new System.Collections.Generic.List<AssetFlow.Models.Asset>();

        public System.Collections.Generic.List<AssetFlow.Models.Asset> MaintenanceAssetsList { get; set; }
            = new System.Collections.Generic.List<AssetFlow.Models.Asset>();

        public System.Collections.Generic.List<AssetFlow.Models.Asset> WarrantyExpiringList { get; set; }
            = new System.Collections.Generic.List<AssetFlow.Models.Asset>();
    }
}