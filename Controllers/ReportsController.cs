using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;
using AssetFlow.Services;
using System.Globalization;

namespace AssetFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AnalyticsService _analytics;

        public ReportsController(ApplicationDbContext context, AnalyticsService analytics)
        {
            _context = context;
            _analytics = analytics;
        }

        // GET: Usage - how often each asset actually gets used, and what is sitting idle.
        public async Task<IActionResult> Usage(string sort = "used")
        {
            var model = await _analytics.UsageAsync();

            model.Rows = sort switch
            {
                "least" => model.Rows.OrderBy(r => r.TimesCheckedOut).ThenByDescending(r => r.IdleDays).ToList(),
                "idle" => model.Rows.OrderByDescending(r => r.IdleDays).ToList(),
                "utilisation" => model.Rows.OrderByDescending(r => r.UtilisationPercent).ToList(),
                "value" => model.Rows.OrderByDescending(r => r.PurchasePrice).ToList(),
                _ => model.Rows.OrderByDescending(r => r.TimesCheckedOut).ThenByDescending(r => r.DaysOut).ToList()
            };

            ViewBag.Sort = sort;

            return View(model);
        }

        // GET: Departments
        public async Task<IActionResult> Departments()
        {
            return View(await _analytics.ByDepartmentAsync());
        }

        // GET: Depreciation
        public async Task<IActionResult> Depreciation()
        {
            return View(await _analytics.DepreciationAsync());
        }

        // GET: Trends
        public async Task<IActionResult> Trends(int months = 12)
        {
            if (months < 3) { months = 3; }
            if (months > 36) { months = 36; }

            var points = await _analytics.MonthlyTrendAsync(months);

            ViewBag.Months = months;
            ViewBag.Labels = points.Select(p => p.Label).ToList();
            ViewBag.CheckedOut = points.Select(p => p.CheckedOut).ToList();
            ViewBag.Returned = points.Select(p => p.Returned).ToList();

            return View(points);
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
        //
        // This used to read the checkout fields on the asset row, which check-in clears,
        // so a returned item vanished from its own history and the report only ever
        // listed things still out. It reads the checkout ledger now, so a returned
        // episode stays on the record.
        public async Task<IActionResult> CheckoutHistory(int? days = 30)
        {
            var window = days ?? 30;
            var startDate = DateTime.Now.AddDays(-window);

            var history = await _context.CheckoutRecords
                .Include(r => r.Asset)
                .Where(r => r.CheckedOutOn >= startDate)
                .OrderByDescending(r => r.CheckedOutOn)
                .Select(r => new CheckoutHistoryViewModel
                {
                    AssetName = r.Asset != null ? r.Asset.Name : "(deleted asset)",
                    AssetSerial = r.Asset != null ? r.Asset.SerialNumber : "",
                    EmployeeName = r.EmployeeName,
                    Department = r.Department,
                    CheckoutDate = r.CheckedOutOn,
                    ExpectedReturnDate = r.DueOn,
                    ActualReturnDate = r.ReturnedOn,
                    Status = r.ReturnedOn == null ? "Out" : "Returned"
                })
                .ToListAsync();

            ViewBag.Days = window;

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
