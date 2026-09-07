using System;
using System.Collections.Generic;

namespace AssetFlow.Models
{
    // One row of the usage report - what an asset has actually done, as opposed to
    // what it is worth or where it lives.
    public class AssetUsageRow
    {
        public int AssetId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string SerialNumber { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal PurchasePrice { get; set; }

        public int TimesCheckedOut { get; set; }

        public int DaysOut { get; set; }

        // Days the asset has been on the books for, measured from the earlier of its
        // purchase date and its first recorded checkout. Assets bought last week
        // should not look badly used next to ones bought three years ago.
        public int DaysTracked { get; set; }

        public DateTime? LastReturned { get; set; }

        public DateTime? LastCheckedOut { get; set; }

        public bool CurrentlyOut { get; set; }

        // Share of the tracked window the asset has spent in somebody's hands.
        public double UtilisationPercent =>
            DaysTracked <= 0 ? 0 : Math.Round(100.0 * DaysOut / DaysTracked, 1);

        // Days since it last came back. An asset that has never been out reports the
        // whole tracked window, which is the honest answer to "how long has this sat
        // there doing nothing".
        public int IdleDays
        {
            get
            {
                if (CurrentlyOut)
                {
                    return 0;
                }

                var since = LastReturned ?? LastCheckedOut;

                return since.HasValue
                    ? (DateTime.Today - since.Value.Date).Days
                    : DaysTracked;
            }
        }

        public bool NeverUsed => TimesCheckedOut == 0;
    }

    public class DepartmentUsageRow
    {
        public string Department { get; set; } = "Unassigned";

        public int CurrentlyHolding { get; set; }

        public decimal ValueHeld { get; set; }

        public int TotalCheckouts { get; set; }

        public int TotalDaysOut { get; set; }

        public int DistinctAssets { get; set; }

        public double AverageDaysPerCheckout =>
            TotalCheckouts == 0 ? 0 : Math.Round((double)TotalDaysOut / TotalCheckouts, 1);
    }

    // Straight-line depreciation. Useful life is per category rather than per asset -
    // the app has never asked anybody to enter one, and inventing a field people have
    // to fill in for every row would leave it mostly empty.
    public class DepreciationRow
    {
        public int AssetId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; }

        public decimal PurchasePrice { get; set; }

        public int UsefulLifeYears { get; set; }

        public double AgeYears => Math.Round((DateTime.Today - PurchaseDate.Date).TotalDays / 365.25, 2);

        public decimal AnnualDepreciation =>
            UsefulLifeYears <= 0 ? 0 : Math.Round(PurchasePrice / UsefulLifeYears, 2);

        // Never depreciates past zero, and never below it.
        public decimal AccumulatedDepreciation
        {
            get
            {
                var accumulated = AnnualDepreciation * (decimal)AgeYears;
                return accumulated > PurchasePrice ? PurchasePrice : Math.Round(accumulated, 2);
            }
        }

        public decimal BookValue => Math.Round(PurchasePrice - AccumulatedDepreciation, 2);

        public bool FullyDepreciated => BookValue <= 0;

        // What each day of actual use has cost so far. An expensive item nobody
        // borrows looks bad here, which is the point of the number.
        public int DaysOut { get; set; }

        public decimal? CostPerDayUsed =>
            DaysOut <= 0 ? null : Math.Round(AccumulatedDepreciation / DaysOut, 2);
    }

    public class MonthlyTrendPoint
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public string Label => new DateTime(Year, Month, 1).ToString("MMM yy");

        public int CheckedOut { get; set; }

        public int Returned { get; set; }
    }

    public class UsageReportViewModel
    {
        public List<AssetUsageRow> Rows { get; set; } = new();

        public int TotalEpisodes { get; set; }

        public DateTime? EarliestRecord { get; set; }

        // When the ledger actually started watching, taken from the earliest episode
        // it observed rather than the earliest date it holds.
        //
        // Those are not the same thing. Backfilled episodes carry the checkout date
        // that was sitting on the asset row, which can be months back, so keying the
        // warning off EarliestRecord made a ledger an hour old claim seven months of
        // history. Only episodes this system saw happen count as coverage.
        public DateTime? ObservedFrom { get; set; }

        public int BackfilledEpisodes { get; set; }

        public bool HistoryIsShallow => !ObservedFrom.HasValue
                                        || (DateTime.Today - ObservedFrom.Value.Date).Days < 90;

        public int DaysObserved => ObservedFrom.HasValue
            ? (DateTime.Today - ObservedFrom.Value.Date).Days
            : 0;
    }
}
