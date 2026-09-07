using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;

namespace AssetFlow.Services
{
    // The numbers behind the analytics reports. Every one of them reads the checkout
    // ledger, which is why the ledger had to exist first - before it, a returned
    // checkout left no trace and none of this could be answered.
    public class AnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Rough service life by category, used for straight-line depreciation. Nobody
        // has ever been asked to enter a useful life per asset, and adding a field
        // that has to be filled in on every row would leave it mostly empty, so this
        // is keyed off the category that already exists.
        private static readonly Dictionary<string, int> UsefulLifeByCategory =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["IT Equipment"] = 3,
                ["AV Equipment"] = 5,
                ["Furniture"] = 10,
                ["Vehicle"] = 8,
                ["Tools"] = 5
            };

        private const int DefaultUsefulLifeYears = 5;

        public static int UsefulLifeFor(string category)
        {
            return UsefulLifeByCategory.TryGetValue(category ?? "", out var years)
                ? years
                : DefaultUsefulLifeYears;
        }

        public async Task<UsageReportViewModel> UsageAsync()
        {
            var assets = await _context.Assets.ToListAsync();
            var records = await _context.CheckoutRecords.ToListAsync();

            var byAsset = records.GroupBy(r => r.AssetId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Backfilled episodes carry a checkout date that predates this ledger, so
            // they say nothing about how long it has been watching. Coverage is
            // measured from the earliest episode the system actually saw happen.
            var observed = records
                .Where(r => !string.Equals(r.Source, "Backfilled", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var model = new UsageReportViewModel
            {
                TotalEpisodes = records.Count,
                EarliestRecord = records.Count == 0 ? null : records.Min(r => r.CheckedOutOn),
                ObservedFrom = observed.Count == 0 ? null : observed.Min(r => r.CheckedOutOn),
                BackfilledEpisodes = records.Count - observed.Count
            };

            foreach (var asset in assets)
            {
                byAsset.TryGetValue(asset.Id, out var episodes);
                episodes ??= new List<CheckoutRecord>();

                // Measured from whichever came first, the purchase or the earliest
                // recorded checkout. A backfilled episode can predate a purchase date
                // that was typed in carelessly, and a negative window would divide
                // utilisation into nonsense.
                var start = asset.PurchaseDate.Date;

                if (episodes.Count > 0)
                {
                    var firstOut = episodes.Min(e => e.CheckedOutOn).Date;
                    if (firstOut < start)
                    {
                        start = firstOut;
                    }
                }

                var tracked = (DateTime.Today - start).Days;

                model.Rows.Add(new AssetUsageRow
                {
                    AssetId = asset.Id,
                    Name = asset.Name,
                    SerialNumber = asset.SerialNumber,
                    Category = asset.Category,
                    Status = asset.Status,
                    PurchasePrice = asset.PurchasePrice,
                    TimesCheckedOut = episodes.Count,
                    DaysOut = episodes.Sum(e => e.DaysHeld),
                    DaysTracked = tracked < 1 ? 1 : tracked,
                    LastCheckedOut = episodes.Count == 0 ? null : episodes.Max(e => e.CheckedOutOn),
                    LastReturned = episodes.Where(e => e.ReturnedOn.HasValue)
                        .Select(e => e.ReturnedOn)
                        .DefaultIfEmpty(null)
                        .Max(),
                    CurrentlyOut = episodes.Any(e => e.ReturnedOn == null)
                });
            }

            return model;
        }

        public async Task<List<DepartmentUsageRow>> ByDepartmentAsync()
        {
            var records = await _context.CheckoutRecords.ToListAsync();
            var assets = await _context.Assets.ToListAsync();

            var rows = records
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Department) ? "Unassigned" : r.Department!)
                .Select(g => new DepartmentUsageRow
                {
                    Department = g.Key,
                    TotalCheckouts = g.Count(),
                    TotalDaysOut = g.Sum(r => r.DaysHeld),
                    DistinctAssets = g.Select(r => r.AssetId).Distinct().Count()
                })
                .ToDictionary(r => r.Department, StringComparer.OrdinalIgnoreCase);

            // What each department is holding right now, which is a different question
            // from what it has borrowed over time - a department can be a heavy user
            // and be holding nothing today.
            foreach (var asset in assets.Where(a => a.Status == "CheckedOut"))
            {
                var dept = string.IsNullOrWhiteSpace(asset.EmployeeDepartment)
                    ? "Unassigned"
                    : asset.EmployeeDepartment!;

                if (!rows.TryGetValue(dept, out var row))
                {
                    row = new DepartmentUsageRow { Department = dept };
                    rows[dept] = row;
                }

                row.CurrentlyHolding++;
                row.ValueHeld += asset.PurchasePrice;
            }

            return rows.Values.OrderByDescending(r => r.TotalCheckouts).ThenBy(r => r.Department).ToList();
        }

        public async Task<List<DepreciationRow>> DepreciationAsync()
        {
            var assets = await _context.Assets.ToListAsync();
            var records = await _context.CheckoutRecords.ToListAsync();

            var daysOut = records.GroupBy(r => r.AssetId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.DaysHeld));

            return assets
                .Select(a => new DepreciationRow
                {
                    AssetId = a.Id,
                    Name = a.Name,
                    Category = a.Category,
                    PurchaseDate = a.PurchaseDate,
                    PurchasePrice = a.PurchasePrice,
                    UsefulLifeYears = UsefulLifeFor(a.Category),
                    DaysOut = daysOut.TryGetValue(a.Id, out var d) ? d : 0
                })
                .OrderByDescending(r => r.PurchasePrice)
                .ToList();
        }

        // Checkouts and returns per month. Returns are counted in the month the item
        // came back, not the month it went out, so the two lines answer different
        // questions and a busy month of returns shows up where it happened.
        public async Task<List<MonthlyTrendPoint>> MonthlyTrendAsync(int months = 12)
        {
            var from = DateTime.Today.AddMonths(-(months - 1));
            from = new DateTime(from.Year, from.Month, 1);

            var records = await _context.CheckoutRecords
                .Where(r => r.CheckedOutOn >= from || (r.ReturnedOn != null && r.ReturnedOn >= from))
                .ToListAsync();

            var points = new List<MonthlyTrendPoint>();

            // Every month in the window is emitted, including empty ones. A gap in a
            // trend line reads as "nothing happened", which is true; a missing point
            // silently shortens the axis and makes the shape a lie.
            for (var i = 0; i < months; i++)
            {
                var month = from.AddMonths(i);

                points.Add(new MonthlyTrendPoint
                {
                    Year = month.Year,
                    Month = month.Month,
                    CheckedOut = records.Count(r => r.CheckedOutOn.Year == month.Year
                                                    && r.CheckedOutOn.Month == month.Month),
                    Returned = records.Count(r => r.ReturnedOn.HasValue
                                                  && r.ReturnedOn.Value.Year == month.Year
                                                  && r.ReturnedOn.Value.Month == month.Month)
                });
            }

            return points;
        }
    }
}
