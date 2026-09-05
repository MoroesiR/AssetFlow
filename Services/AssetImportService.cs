using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;

namespace AssetFlow.Services
{
    // Turns an uploaded CSV into assets. Every row is validated before anything is
    // written, and a bad row is skipped with a reason rather than failing the whole
    // file - somebody importing 200 assets should not lose 199 of them to one typo.
    public class AssetImportService
    {
        private readonly ApplicationDbContext _context;

        public AssetImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public static readonly string[] Columns =
        {
            "Name", "SerialNumber", "Category", "Status", "PurchasePrice",
            "PurchaseDate", "Location", "Vendor", "WarrantyExpiry", "Notes"
        };

        private static readonly string[] AllowedStatuses =
        {
            "Available", "CheckedOut", "Maintenance", "Retired"
        };

        // The file people download to see what the importer expects. Row two is an
        // example rather than instructions, because an example is what gets copied.
        public static string TemplateCsv()
        {
            return string.Join(",", Columns) + "\n" +
                   "Dell Latitude 5540,DL-5540-0031,IT Equipment,Available,18499.00,2026-02-14,Head Office - 2nd Floor,Dell South Africa,2029-02-14,\"Standard issue laptop, 16GB RAM\"\n";
        }

        public async Task<AssetImportResult> ImportAsync(string csv, string fileName, bool dryRun)
        {
            var result = new AssetImportResult
            {
                FileName = fileName,
                WasDryRun = dryRun
            };

            var rows = CsvReader.Parse(csv);

            if (rows.Count == 0)
            {
                result.FileErrors.Add("The file is empty.");
                return result;
            }

            var header = rows[0].Select(h => h.Trim()).ToList();

            // Name and SerialNumber are the two that cannot be defaulted, so the file is
            // rejected outright if the header does not have them.
            foreach (var required in new[] { "Name", "SerialNumber" })
            {
                if (!header.Any(h => h.Equals(required, StringComparison.OrdinalIgnoreCase)))
                {
                    result.FileErrors.Add($"The header row has no '{required}' column.");
                }
            }

            if (result.FileErrors.Count > 0)
            {
                return result;
            }

            if (rows.Count == 1)
            {
                result.FileErrors.Add("The file has a header but no rows.");
                return result;
            }

            // Serial numbers already on the database, plus the ones seen earlier in this
            // same file. A file that repeats a serial internally is just as broken as one
            // that collides with an existing asset.
            var existingSerials = await _context.Assets
                .Select(a => a.SerialNumber)
                .ToListAsync();

            var seen = new HashSet<string>(existingSerials, StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i < rows.Count; i++)
            {
                var row = ParseRow(header, rows[i], i + 1, seen);
                result.Rows.Add(row);

                if (row.IsValid && !string.IsNullOrWhiteSpace(row.SerialNumber))
                {
                    seen.Add(row.SerialNumber);
                }
            }

            if (dryRun || !result.HasAnythingToImport)
            {
                return result;
            }

            foreach (var row in result.Rows.Where(r => r.IsValid && r.Asset != null))
            {
                _context.Assets.Add(row.Asset!);
            }

            await _context.SaveChangesAsync();

            return result;
        }

        private static AssetImportRow ParseRow(List<string> header, List<string> cells, int lineNumber, HashSet<string> seen)
        {
            var row = new AssetImportRow { LineNumber = lineNumber };

            string Value(string column)
            {
                var index = header.FindIndex(h => h.Equals(column, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < cells.Count ? cells[index].Trim() : string.Empty;
            }

            row.Name = Value("Name");
            row.SerialNumber = Value("SerialNumber");
            row.Category = Value("Category");

            if (string.IsNullOrWhiteSpace(row.Name))
            {
                row.Errors.Add("Name is blank");
            }
            else if (row.Name.Length > 100)
            {
                row.Errors.Add("Name is longer than 100 characters");
            }

            if (string.IsNullOrWhiteSpace(row.SerialNumber))
            {
                row.Errors.Add("SerialNumber is blank");
            }
            else if (row.SerialNumber.Length > 50)
            {
                row.Errors.Add("SerialNumber is longer than 50 characters");
            }
            else if (seen.Contains(row.SerialNumber))
            {
                row.Errors.Add($"Serial '{row.SerialNumber}' already exists");
            }

            var status = Value("Status");

            if (string.IsNullOrWhiteSpace(status))
            {
                status = "Available";
            }
            else if (!AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                row.Errors.Add($"Status '{status}' is not one of {string.Join(", ", AllowedStatuses)}");
            }

            // An imported asset is never checked out to anybody. Bringing a checkout
            // across would need a person to attach it to, and the file has no such column.
            if (status.Equals("CheckedOut", StringComparison.OrdinalIgnoreCase))
            {
                row.Errors.Add("Status cannot be CheckedOut on import - add the asset, then check it out");
            }

            var price = 0m;
            var rawPrice = Value("PurchasePrice");

            if (!string.IsNullOrWhiteSpace(rawPrice))
            {
                if (!TryPrice(rawPrice, out price))
                {
                    row.Errors.Add($"PurchasePrice '{rawPrice}' is not a number");
                }
                else if (price < 0)
                {
                    row.Errors.Add("PurchasePrice is negative");
                }
            }

            var purchaseDate = TryDate(Value("PurchaseDate"), "PurchaseDate", row) ?? DateTime.Today;
            var warranty = TryDate(Value("WarrantyExpiry"), "WarrantyExpiry", row);

            if (!row.IsValid)
            {
                return row;
            }

            row.Asset = new Asset
            {
                Name = row.Name,
                SerialNumber = row.SerialNumber,
                Category = string.IsNullOrWhiteSpace(row.Category) ? "Uncategorized" : row.Category,
                Status = status,
                PurchasePrice = price,
                PurchaseDate = purchaseDate,
                Location = Nullable(Value("Location")),
                Vendor = Nullable(Value("Vendor")),
                WarrantyExpiry = warranty,
                Notes = Nullable(Value("Notes")),
                LastUpdated = DateTime.Now
            };

            return row;
        }

        // Prices arrive in whichever convention the exporting machine was set to. Locally
        // that is "R 4 250,00" - space for thousands, comma for the decimal - while an
        // en-US export of the same value is "4,250.00". Stripping every comma treats the
        // first as four hundred thousand, so the separators have to be worked out rather
        // than deleted.
        private static bool TryPrice(string raw, out decimal value)
        {
            value = 0m;

            // Currency symbols, letters and any kind of space are noise either way.
            var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == ',' || c == '-').ToArray());

            if (cleaned.Length == 0)
            {
                return false;
            }

            var lastDot = cleaned.LastIndexOf('.');
            var lastComma = cleaned.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                // Both present: whichever comes last is the decimal point, the other groups.
                var decimalSeparator = lastDot > lastComma ? '.' : ',';
                var groupSeparator = decimalSeparator == '.' ? ',' : '.';

                cleaned = cleaned.Replace(groupSeparator.ToString(), "");
                cleaned = cleaned.Replace(decimalSeparator, '.');
            }
            else if (lastDot >= 0 || lastComma >= 0)
            {
                var separator = lastDot >= 0 ? '.' : ',';
                var position = lastDot >= 0 ? lastDot : lastComma;
                var occurrences = cleaned.Count(c => c == separator);
                var digitsAfter = cleaned.Length - position - 1;

                // One separator with exactly three digits behind it is a thousands group
                // ("4,250"). Anything else - two digits, or several separators - is a
                // decimal point or grouping that can be dropped.
                if (occurrences > 1 || digitsAfter == 3)
                {
                    cleaned = cleaned.Replace(separator.ToString(), "");
                }
                else
                {
                    cleaned = cleaned.Replace(separator, '.');
                }
            }

            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        // Dates come in as whatever the person's Excel produced. ISO first because that
        // is what the template shows, then the day-first formats used locally.
        private static DateTime? TryDate(string raw, string column, AssetImportRow row)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var formats = new[]
            {
                "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "dd-MM-yyyy",
                "dd MMM yyyy", "d MMM yyyy", "MM/dd/yyyy"
            };

            if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }

            row.Errors.Add($"{column} '{raw}' is not a date the importer recognises (try yyyy-MM-dd)");
            return null;
        }

        private static string? Nullable(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
