using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;
using System.Globalization;

namespace AssetFlow.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AssetValue
        public async Task<IActionResult> AssetValueReport()
        {
            var report = await _context.Assets
                .GroupBy(a => a.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(a => a.PurchasePrice),
                    AverageValue = g.Average(a => a.PurchasePrice)
                })
                .ToListAsync();

            ViewBag.Categories = report.Select(r => r.Category).ToList();
            ViewBag.Counts = report.Select(r => r.Count).ToList();
            ViewBag.TotalValues = report.Select(r => r.TotalValue).ToList();
            ViewBag.AverageValues = report.Select(r => r.AverageValue).ToList();

            return View();
        }

        // GET:CheckoutHistory
        public async Task<IActionResult> CheckoutHistory(int? days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days.Value);

            var history = await _context.Assets
                .Where(a => a.CheckoutDate.HasValue && a.CheckoutDate >= startDate)
                .OrderByDescending(a => a.CheckoutDate)
                .Select(a => new CheckoutHistoryViewModel
                {
                    AssetName = a.Name,
                    AssetSerial = a.SerialNumber,
                    EmployeeName = a.CheckedOutToEmployee,
                    Department = a.EmployeeDepartment,
                    CheckoutDate = a.CheckoutDate.Value,
                    ExpectedReturnDate = a.ExpectedReturnDate,
                    ActualReturnDate = a.ActualReturnDate,
                    Status = a.Status
                })
                .ToListAsync();

            return View(history);
        }

        // GET:MaintenanceSchedule
        public async Task<IActionResult> MaintenanceSchedule()
        {
            var schedule = await _context.Assets
                .Where(a => a.NextMaintenanceDue.HasValue)
                .OrderBy(a => a.NextMaintenanceDue)
                .Select(a => new MaintenanceScheduleViewModel
                {
                    Id = a.Id,
                    AssetName = a.Name,
                    SerialNumber = a.SerialNumber,
                    Category = a.Category,
                    LastMaintenanceDate = a.LastMaintenanceDate,
                    NextMaintenanceDue = a.NextMaintenanceDue.Value,
                    MaintenanceNotes = a.MaintenanceNotes,
                    Status = a.Status,
                    DaysUntilDue = (a.NextMaintenanceDue.Value - DateTime.Today).Days
                })
                .ToListAsync();

            return View(schedule);
        }

        // GET:Export
        public async Task<IActionResult> ExportAssets(string format = "csv")
        {
            var assets = await _context.Assets.ToListAsync();

            if (format.ToLower() == "csv")
            {
                return ExportToCsv(assets);
            }

            return View("AssetValueReport");
        }

        private IActionResult ExportToCsv(List<Asset> assets)
        {
            var csv = "ID,Name,SerialNumber,Category,Status,PurchasePrice,PurchaseDate,Location,WarrantyExpiry,LastUpdated\n";

            foreach (var asset in assets)
            {
                csv += $"{asset.Id},\"{asset.Name}\",\"{asset.SerialNumber}\",{asset.Category},{asset.Status},{asset.PurchasePrice},{asset.PurchaseDate:yyyy-MM-dd},\"{asset.Location}\",{asset.WarrantyExpiry?.ToString("yyyy-MM-dd")},{asset.LastUpdated:yyyy-MM-dd}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"assets_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }

    public class CheckoutHistoryViewModel
    {
        public string AssetName { get; set; }
        public string AssetSerial { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public DateTime CheckoutDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Status { get; set; }
    }

    public class MaintenanceScheduleViewModel
    {
        public int Id { get; set; }
        public string AssetName { get; set; }
        public string SerialNumber { get; set; }
        public string Category { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime NextMaintenanceDue { get; set; }
        public string MaintenanceNotes { get; set; }
        public string Status { get; set; }
        public int DaysUntilDue { get; set; }
    }
}