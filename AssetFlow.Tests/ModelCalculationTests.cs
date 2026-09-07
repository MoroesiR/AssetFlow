using System;
using AssetFlow.Models;
using Xunit;

namespace AssetFlow.Tests
{
    // The computed properties the reports lean on. These are where an off-by-one or a
    // wrong sign quietly turns a report into a lie, so they get pinned down.
    public class ModelCalculationTests
    {
        [Fact]
        public void An_open_episode_counts_days_up_to_today()
        {
            var record = new CheckoutRecord { CheckedOutOn = DateTime.Today.AddDays(-10) };

            Assert.True(record.IsOpen);
            Assert.Equal(10, record.DaysHeld);
        }

        [Fact]
        public void A_closed_episode_counts_days_to_the_return()
        {
            var record = new CheckoutRecord
            {
                CheckedOutOn = DateTime.Today.AddDays(-10),
                ReturnedOn = DateTime.Today.AddDays(-3)
            };

            Assert.False(record.IsOpen);
            Assert.Equal(7, record.DaysHeld);
        }

        [Fact]
        public void Returning_after_the_due_date_is_late_by_the_difference()
        {
            var record = new CheckoutRecord
            {
                CheckedOutOn = DateTime.Today.AddDays(-10),
                DueOn = DateTime.Today.AddDays(-5),
                ReturnedOn = DateTime.Today.AddDays(-2)
            };

            Assert.True(record.ReturnedLate);
            Assert.Equal(3, record.DaysLate);
        }

        [Fact]
        public void Returning_on_the_due_date_is_not_late()
        {
            var due = DateTime.Today.AddDays(-2);
            var record = new CheckoutRecord
            {
                CheckedOutOn = DateTime.Today.AddDays(-10),
                DueOn = due,
                ReturnedOn = due
            };

            Assert.False(record.ReturnedLate);
            Assert.Null(record.DaysLate);
        }

        [Fact]
        public void An_item_still_out_past_its_due_date_is_not_yet_counted_as_returned_late()
        {
            var record = new CheckoutRecord
            {
                CheckedOutOn = DateTime.Today.AddDays(-10),
                DueOn = DateTime.Today.AddDays(-5)
            };

            Assert.False(record.ReturnedLate);
        }

        [Fact]
        public void An_asset_out_past_its_return_date_is_overdue()
        {
            var asset = new Asset
            {
                Status = "CheckedOut",
                ExpectedReturnDate = DateTime.Today.AddDays(-1)
            };

            Assert.True(asset.IsOverdue);
        }

        [Fact]
        public void An_available_asset_is_never_overdue_whatever_dates_it_carries()
        {
            var asset = new Asset
            {
                Status = "Available",
                ExpectedReturnDate = DateTime.Today.AddDays(-30)
            };

            Assert.False(asset.IsOverdue);
        }

        [Fact]
        public void A_service_interval_puts_an_asset_on_a_schedule()
        {
            Assert.True(new Asset { MaintenanceIntervalDays = 90 }.IsOnMaintenanceSchedule);
            Assert.False(new Asset { MaintenanceIntervalDays = null }.IsOnMaintenanceSchedule);
            Assert.False(new Asset { MaintenanceIntervalDays = 0 }.IsOnMaintenanceSchedule);
        }

        [Fact]
        public void Days_until_maintenance_goes_negative_once_it_is_late()
        {
            var asset = new Asset { NextMaintenanceDue = DateTime.Today.AddDays(-4) };

            Assert.Equal(-4, asset.DaysUntilMaintenance);
            Assert.True(asset.IsMaintenanceDue);
        }

        [Fact]
        public void An_asset_with_no_booked_service_has_no_countdown()
        {
            Assert.Null(new Asset { NextMaintenanceDue = null }.DaysUntilMaintenance);
        }

        [Fact]
        public void Utilisation_is_days_out_over_the_tracked_window()
        {
            var row = new AssetUsageRow { DaysOut = 25, DaysTracked = 100 };

            Assert.Equal(25.0, row.UtilisationPercent);
        }

        [Fact]
        public void Utilisation_does_not_divide_by_a_zero_window()
        {
            var row = new AssetUsageRow { DaysOut = 5, DaysTracked = 0 };

            Assert.Equal(0, row.UtilisationPercent);
        }

        [Fact]
        public void An_asset_out_right_now_is_not_idle()
        {
            var row = new AssetUsageRow
            {
                CurrentlyOut = true,
                LastReturned = DateTime.Today.AddDays(-100)
            };

            Assert.Equal(0, row.IdleDays);
        }

        [Fact]
        public void Idle_is_measured_from_the_last_return()
        {
            var row = new AssetUsageRow
            {
                CurrentlyOut = false,
                LastReturned = DateTime.Today.AddDays(-14)
            };

            Assert.Equal(14, row.IdleDays);
        }

        [Fact]
        public void An_asset_never_checked_out_reports_the_whole_window_as_idle()
        {
            var row = new AssetUsageRow { CurrentlyOut = false, DaysTracked = 200 };

            Assert.True(row.NeverUsed);
            Assert.Equal(200, row.IdleDays);
        }

        [Fact]
        public void Depreciation_is_straight_line_over_the_useful_life()
        {
            var row = new DepreciationRow
            {
                PurchasePrice = 30000m,
                UsefulLifeYears = 3,
                PurchaseDate = DateTime.Today.AddYears(-1)
            };

            Assert.Equal(10000m, row.AnnualDepreciation);
            Assert.InRange(row.BookValue, 19900m, 20100m);
            Assert.False(row.FullyDepreciated);
        }

        [Fact]
        public void An_asset_past_its_life_is_written_off_and_never_goes_negative()
        {
            var row = new DepreciationRow
            {
                PurchasePrice = 30000m,
                UsefulLifeYears = 3,
                PurchaseDate = DateTime.Today.AddYears(-10)
            };

            Assert.Equal(30000m, row.AccumulatedDepreciation);
            Assert.Equal(0m, row.BookValue);
            Assert.True(row.FullyDepreciated);
        }

        [Fact]
        public void Cost_per_day_used_is_undefined_for_something_never_borrowed()
        {
            var row = new DepreciationRow
            {
                PurchasePrice = 30000m,
                UsefulLifeYears = 3,
                PurchaseDate = DateTime.Today.AddYears(-1),
                DaysOut = 0
            };

            Assert.Null(row.CostPerDayUsed);
        }

        [Fact]
        public void A_ledger_with_only_carried_over_episodes_counts_as_shallow_history()
        {
            // The bug this pins down: coverage was read off the earliest record, and a
            // backfilled episode carries a date from long before the ledger existed, so
            // an hour-old ledger claimed months of history and stayed quiet.
            var model = new UsageReportViewModel
            {
                TotalEpisodes = 6,
                EarliestRecord = DateTime.Today.AddMonths(-7),
                ObservedFrom = null,
                BackfilledEpisodes = 6
            };

            Assert.True(model.HistoryIsShallow);
        }

        [Fact]
        public void A_ledger_running_for_a_year_is_not_shallow()
        {
            var model = new UsageReportViewModel
            {
                TotalEpisodes = 50,
                ObservedFrom = DateTime.Today.AddDays(-365)
            };

            Assert.False(model.HistoryIsShallow);
            Assert.Equal(365, model.DaysObserved);
        }
    }
}
