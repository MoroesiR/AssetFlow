using System;
using System.Linq;
using System.Threading.Tasks;
using AssetFlow.Data;
using AssetFlow.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetFlow.Tests
{
    // Runs the real import against a throwaway SQLite database rather than mocking it,
    // because the parts worth testing - duplicate detection against existing rows,
    // dry run writing nothing - only mean anything with a database behind them.
    public class AssetImportServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly AssetImportService _import;

        public AssetImportServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _import = new AssetImportService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        private const string Header = "Name,SerialNumber,PurchasePrice,PurchaseDate,Status";

        // The bug that shipped and had to be fixed: "R 4 250,00" was being read as
        // 425000, because stripping every comma treats a decimal comma as a thousands
        // separator. Both conventions have to land on the same number.
        [Theory]
        [InlineData("\"R 4 250,00\"", 4250.00)]
        [InlineData("\"4,250.00\"", 4250.00)]
        [InlineData("4250.00", 4250.00)]
        [InlineData("\"4,250\"", 4250.00)]
        [InlineData("\"12,50\"", 12.50)]
        [InlineData("\"R 1 234 567,89\"", 1234567.89)]
        [InlineData("\"1,234,567.89\"", 1234567.89)]
        [InlineData("4250", 4250.00)]
        public async Task Reads_prices_in_either_convention(string raw, decimal expected)
        {
            var csv = $"{Header}\nThing,SER-1,{raw},2026-01-01,Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Empty(result.Rows[0].Errors);
            Assert.Equal(expected, result.Rows[0].Asset!.PurchasePrice);
        }

        [Fact]
        public async Task Rejects_a_price_that_is_not_a_number()
        {
            var csv = $"{Header}\nThing,SER-1,not-a-number,2026-01-01,Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Contains(result.Rows[0].Errors, e => e.Contains("not a number"));
        }

        [Theory]
        [InlineData("2026-03-04")]
        [InlineData("04/03/2026")]
        [InlineData("2026/03/04")]
        public async Task Reads_the_date_formats_people_actually_export(string raw)
        {
            var csv = $"{Header}\nThing,SER-1,100,{raw},Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Empty(result.Rows[0].Errors);
            Assert.Equal(new DateTime(2026, 3, 4), result.Rows[0].Asset!.PurchaseDate);
        }

        [Fact]
        public async Task Rejects_a_date_it_cannot_read()
        {
            var csv = $"{Header}\nThing,SER-1,100,31st of Never,Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Contains(result.Rows[0].Errors, e => e.Contains("not a date"));
        }

        [Fact]
        public async Task A_bad_row_is_skipped_and_the_good_ones_still_import()
        {
            var csv = $"{Header}\n" +
                      ",NO-NAME,100,2026-01-01,Available\n" +
                      "Good One,GOOD-1,100,2026-01-01,Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: false);

            Assert.Equal(1, result.ValidCount);
            Assert.Equal(1, result.InvalidCount);
            Assert.Equal(1, await _context.Assets.CountAsync());
        }

        [Fact]
        public async Task A_serial_repeated_inside_the_file_is_caught()
        {
            var csv = $"{Header}\n" +
                      "First,DUP-1,100,2026-01-01,Available\n" +
                      "Second,DUP-1,100,2026-01-01,Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Equal(1, result.ValidCount);
            Assert.Contains(result.Rows[1].Errors, e => e.Contains("already exists"));
        }

        [Fact]
        public async Task A_serial_that_collides_with_an_existing_asset_is_caught()
        {
            _context.Assets.Add(new AssetFlow.Models.Asset { Name = "Existing", SerialNumber = "TAKEN-1" });
            await _context.SaveChangesAsync();

            var csv = $"{Header}\nNew One,TAKEN-1,100,2026-01-01,Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Contains(result.Rows[0].Errors, e => e.Contains("already exists"));
        }

        [Fact]
        public async Task A_dry_run_writes_nothing()
        {
            var csv = $"{Header}\nThing,SER-1,100,2026-01-01,Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Equal(1, result.ValidCount);
            Assert.Equal(0, await _context.Assets.CountAsync());
        }

        [Fact]
        public async Task An_asset_cannot_arrive_already_checked_out()
        {
            var csv = $"{Header}\nThing,SER-1,100,2026-01-01,CheckedOut";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Contains(result.Rows[0].Errors, e => e.Contains("cannot be CheckedOut"));
        }

        [Fact]
        public async Task A_file_missing_a_required_column_is_rejected_whole()
        {
            var result = await _import.ImportAsync("Name,PurchasePrice\nThing,100", "t.csv", dryRun: true);

            Assert.Contains(result.FileErrors, e => e.Contains("SerialNumber"));
            Assert.Empty(result.Rows);
        }

        [Fact]
        public async Task Column_order_does_not_matter()
        {
            var csv = "SerialNumber,PurchasePrice,Name\nSER-9,100,Backwards";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Empty(result.Rows[0].Errors);
            Assert.Equal("Backwards", result.Rows[0].Asset!.Name);
            Assert.Equal("SER-9", result.Rows[0].Asset!.SerialNumber);
        }

        [Fact]
        public async Task Line_numbers_match_the_file_so_they_match_Excel()
        {
            var csv = $"{Header}\n" +
                      "Fine,OK-1,100,2026-01-01,Available\n" +
                      ",BAD,100,2026-01-01,Available";

            var result = await _import.ImportAsync(csv, "t.csv", dryRun: true);

            Assert.Equal(2, result.Rows[0].LineNumber);
            Assert.Equal(3, result.Rows[1].LineNumber);
        }
    }
}
