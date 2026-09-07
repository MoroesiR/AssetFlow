using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;

namespace AssetFlow.Controllers
{
    // Pick the columns and the filters, see the result, export it. The fixed reports
    // answer the questions somebody thought of in advance; this one exists for the
    // ones nobody did.
    [Authorize(Roles = "Admin")]
    public class ReportBuilderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportBuilderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // The columns on offer, and how each one reads a value off an asset. Keeping
        // the list here rather than reflecting over the model means a column can never
        // be requested that the report does not know how to render, and nothing the
        // user sends is ever used to reach into the object graph.
        private static readonly Dictionary<string, (string Label, Func<Asset, object?> Read)> Columns =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = ("Asset", a => a.Name),
                ["SerialNumber"] = ("Serial", a => a.SerialNumber),
                ["Category"] = ("Category", a => a.Category),
                ["Status"] = ("Status", a => a.Status),
                ["Location"] = ("Location", a => a.Location),
                ["Vendor"] = ("Vendor", a => a.Vendor),
                ["PurchasePrice"] = ("Price", a => a.PurchasePrice),
                ["PurchaseDate"] = ("Purchased", a => a.PurchaseDate),
                ["WarrantyExpiry"] = ("Warranty", a => a.WarrantyExpiry),
                ["CheckedOutToEmployee"] = ("Held By", a => a.CheckedOutToEmployee),
                ["EmployeeDepartment"] = ("Department", a => a.EmployeeDepartment),
                ["ExpectedReturnDate"] = ("Due Back", a => a.ExpectedReturnDate),
                ["NextMaintenanceDue"] = ("Service Due", a => a.NextMaintenanceDue),
                ["LastMaintenanceDate"] = ("Last Serviced", a => a.LastMaintenanceDate),
                ["Notes"] = ("Notes", a => a.Notes)
            };

        private static readonly string[] DefaultColumns =
            { "Name", "SerialNumber", "Category", "Status", "PurchasePrice" };

        public static IReadOnlyDictionary<string, (string Label, Func<Asset, object?> Read)> Available => Columns;

        // GET: ReportBuilder
        public async Task<IActionResult> Index(
            string[]? columns,
            string status,
            string category,
            string department,
            decimal? minPrice,
            decimal? maxPrice,
            DateTime? purchasedFrom,
            DateTime? purchasedTo,
            string flag,
            string sort,
            string export)
        {
            var chosen = (columns == null || columns.Length == 0)
                ? DefaultColumns
                : columns.Where(c => Columns.ContainsKey(c)).ToArray();

            if (chosen.Length == 0)
            {
                chosen = DefaultColumns;
            }

            var query = BuildQuery(status, category, department, minPrice, maxPrice,
                purchasedFrom, purchasedTo, flag);

            query = sort switch
            {
                "price_desc" => query.OrderByDescending(a => a.PurchasePrice),
                "price" => query.OrderBy(a => a.PurchasePrice),
                "purchased" => query.OrderBy(a => a.PurchaseDate),
                "purchased_desc" => query.OrderByDescending(a => a.PurchaseDate),
                "category" => query.OrderBy(a => a.Category).ThenBy(a => a.Name),
                _ => query.OrderBy(a => a.Name)
            };

            var assets = await query.ToListAsync();

            if (string.Equals(export, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return ExportCsv(assets, chosen);
            }

            ViewBag.Chosen = chosen;
            ViewBag.Columns = Columns;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedDepartment = department;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.PurchasedFrom = purchasedFrom;
            ViewBag.PurchasedTo = purchasedTo;
            ViewBag.SelectedFlag = flag;
            ViewBag.SelectedSort = sort;

            ViewBag.Categories = await _context.Assets.Select(a => a.Category).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Departments = await _context.Assets
                .Where(a => a.EmployeeDepartment != null && a.EmployeeDepartment != "")
                .Select(a => a.EmployeeDepartment!).Distinct().OrderBy(d => d).ToListAsync();

            return View(assets);
        }

        private IQueryable<Asset> BuildQuery(
            string status, string category, string department,
            decimal? minPrice, decimal? maxPrice,
            DateTime? purchasedFrom, DateTime? purchasedTo, string flag)
        {
            var query = _context.Assets.AsQueryable();

            if (!string.IsNullOrEmpty(status)) { query = query.Where(a => a.Status == status); }
            if (!string.IsNullOrEmpty(category)) { query = query.Where(a => a.Category == category); }
            if (!string.IsNullOrEmpty(department)) { query = query.Where(a => a.EmployeeDepartment == department); }
            if (minPrice.HasValue) { query = query.Where(a => a.PurchasePrice >= minPrice.Value); }
            if (maxPrice.HasValue) { query = query.Where(a => a.PurchasePrice <= maxPrice.Value); }
            if (purchasedFrom.HasValue) { query = query.Where(a => a.PurchaseDate >= purchasedFrom.Value); }
            if (purchasedTo.HasValue) { query = query.Where(a => a.PurchaseDate <= purchasedTo.Value); }

            var today = DateTime.Today;

            query = flag switch
            {
                "Overdue" => query.Where(a => a.Status == "CheckedOut"
                                              && a.ExpectedReturnDate != null
                                              && a.ExpectedReturnDate < today),
                "MaintenanceDue" => query.Where(a => a.RequiresMaintenance
                                                     || (a.NextMaintenanceDue != null && a.NextMaintenanceDue < today)),
                "WarrantyExpired" => query.Where(a => a.WarrantyExpiry != null && a.WarrantyExpiry < today),
                "NoLocation" => query.Where(a => a.Location == null || a.Location == ""),
                _ => query
            };

            return query;
        }

        private IActionResult ExportCsv(List<Asset> assets, string[] chosen)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", chosen.Select(c => Quote(Columns[c].Label))));

            foreach (var asset in assets)
            {
                var cells = chosen.Select(c => Quote(Format(Columns[c].Read(asset))));
                sb.AppendLine(string.Join(",", cells));
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()),
                "text/csv",
                $"assetflow-report-{DateTime.Now:yyyyMMdd-HHmm}.csv");
        }

        public static string Format(object? value)
        {
            return value switch
            {
                null => "",
                DateTime d => d.ToString("yyyy-MM-dd"),
                decimal m => m.ToString("0.00"),
                bool b => b ? "Yes" : "No",
                _ => value.ToString() ?? ""
            };
        }

        // Everything is quoted and inner quotes doubled. A name with a comma in it
        // would otherwise split into two columns, which is the same trap the importer
        // had to be written around.
        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
