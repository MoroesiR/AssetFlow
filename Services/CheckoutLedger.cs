using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;

namespace AssetFlow.Services
{
    // Opens and closes checkout episodes. Three places hand an asset over - the manual
    // checkout screen, approving a request, and the importer never does - and two close
    // one, so the bookkeeping lives here rather than being copy-pasted into each.
    public class CheckoutLedger
    {
        private readonly ApplicationDbContext _context;

        public CheckoutLedger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OpenAsync(
            Asset asset,
            string employeeName,
            string? employeeEmail,
            string? department,
            DateTime? dueOn,
            string? notes,
            string source,
            int? assetRequestId = null)
        {
            // An asset should never have two episodes open at once. If one is somehow
            // still open, close it at today rather than stacking a second on top -
            // silently double-counting would poison every utilisation figure.
            var stillOpen = await _context.CheckoutRecords
                .Where(r => r.AssetId == asset.Id && r.ReturnedOn == null)
                .ToListAsync();

            foreach (var orphan in stillOpen)
            {
                orphan.ReturnedOn = DateTime.Now;
                orphan.ConditionOnReturn = "Closed automatically - the asset was checked out again";
            }

            _context.CheckoutRecords.Add(new CheckoutRecord
            {
                AssetId = asset.Id,
                AssetRequestId = assetRequestId,
                EmployeeName = employeeName,
                EmployeeEmail = employeeEmail,
                Department = department,
                CheckedOutOn = DateTime.Now,
                DueOn = dueOn,
                CheckoutNotes = notes,
                Source = source
            });
        }

        // Closes the open episode for an asset. Saving is left to the caller so the
        // record lands in the same unit of work as the asset row it describes.
        public async Task CloseAsync(int assetId, string? conditionNotes)
        {
            var open = await _context.CheckoutRecords
                .Where(r => r.AssetId == assetId && r.ReturnedOn == null)
                .OrderByDescending(r => r.CheckedOutOn)
                .FirstOrDefaultAsync();

            if (open == null)
            {
                // Checked out before this ledger existed, so there is nothing to close.
                // Not an error worth stopping a check-in over.
                return;
            }

            open.ReturnedOn = DateTime.Now;
            open.ConditionOnReturn = conditionNotes;
        }

        // Everything that was already checked out when this table was added has an
        // episode that started in the past and is still running. Without this the
        // utilisation figures would show those assets as never having been used.
        //
        // Episodes that had already been returned cannot be recovered - check-in used
        // to null the holder and the checkout date, so that history is genuinely gone.
        public static async Task BackfillAsync(ApplicationDbContext context)
        {
            if (await context.CheckoutRecords.AnyAsync())
            {
                return;
            }

            var live = await context.Assets
                .Where(a => a.Status == "CheckedOut" && a.CheckoutDate != null)
                .ToListAsync();

            foreach (var asset in live)
            {
                context.CheckoutRecords.Add(new CheckoutRecord
                {
                    AssetId = asset.Id,
                    EmployeeName = string.IsNullOrWhiteSpace(asset.CheckedOutToEmployee)
                        ? "Unknown"
                        : asset.CheckedOutToEmployee,
                    EmployeeEmail = asset.EmployeeEmail,
                    Department = asset.EmployeeDepartment,
                    CheckedOutOn = asset.CheckoutDate!.Value,
                    DueOn = asset.ExpectedReturnDate,
                    CheckoutNotes = asset.CheckoutNotes,
                    Source = "Backfilled"
                });
            }

            if (live.Count > 0)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
