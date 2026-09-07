using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;
using AssetFlow.Services;

namespace AssetFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AssetImportService _import;

        public AssetsController(ApplicationDbContext context, AssetImportService import)
        {
            _context = context;
            _import = import;
        }

        // GET: Assets
        public async Task<IActionResult> Index(
            string search,
            string status,
            string category,
            string vendor,
            string department,
            decimal? minPrice,
            decimal? maxPrice,
            string flag,
            string sort)
        {
            var assets = _context.Assets.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                // Vendor and the holder's name are in here because "who has the Dell"
                // and "what did we buy from Incredible Connection" are both things the
                // IT desk actually asks the search box.
                assets = assets.Where(a =>
                    a.Name.Contains(search) ||
                    a.SerialNumber.Contains(search) ||
                    (a.Location != null && a.Location.Contains(search)) ||
                    (a.Vendor != null && a.Vendor.Contains(search)) ||
                    (a.CheckedOutToEmployee != null && a.CheckedOutToEmployee.Contains(search)));
            }

            if (!string.IsNullOrEmpty(status) && status != "All Status")
            {
                assets = assets.Where(a => a.Status == status);
            }

            if (!string.IsNullOrEmpty(category) && category != "All Categories")
            {
                assets = assets.Where(a => a.Category == category);
            }

            if (!string.IsNullOrEmpty(vendor))
            {
                assets = assets.Where(a => a.Vendor == vendor);
            }

            if (!string.IsNullOrEmpty(department))
            {
                assets = assets.Where(a => a.EmployeeDepartment == department);
            }

            if (minPrice.HasValue)
            {
                assets = assets.Where(a => a.PurchasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                assets = assets.Where(a => a.PurchasePrice <= maxPrice.Value);
            }

            // The saved views. IsOverdue and IsMaintenanceDue are NotMapped, so the same
            // rules are repeated here as query expressions - the model properties cannot
            // cross into SQL.
            var today = DateTime.Today;

            switch (flag)
            {
                case "Overdue":
                    assets = assets.Where(a => a.Status == "CheckedOut"
                                               && a.ExpectedReturnDate != null
                                               && a.ExpectedReturnDate < today);
                    break;

                case "MaintenanceDue":
                    assets = assets.Where(a => a.RequiresMaintenance
                                               || (a.NextMaintenanceDue != null && a.NextMaintenanceDue < today));
                    break;

                case "WarrantyExpired":
                    assets = assets.Where(a => a.WarrantyExpiry != null && a.WarrantyExpiry < today);
                    break;

                case "NoLocation":
                    assets = assets.Where(a => a.Location == null || a.Location == "");
                    break;
            }

            assets = sort switch
            {
                "name_desc" => assets.OrderByDescending(a => a.Name),
                "price" => assets.OrderBy(a => a.PurchasePrice),
                "price_desc" => assets.OrderByDescending(a => a.PurchasePrice),
                "purchased" => assets.OrderBy(a => a.PurchaseDate),
                "purchased_desc" => assets.OrderByDescending(a => a.PurchaseDate),
                "status" => assets.OrderBy(a => a.Status).ThenBy(a => a.Name),
                _ => assets.OrderBy(a => a.Name)
            };

            // The dropdowns were hardcoded, which meant an asset in a category nobody had
            // thought of was unreachable from the filters. They come off the data now.
            ViewBag.Categories = await _context.Assets
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Vendors = await _context.Assets
                .Where(a => a.Vendor != null && a.Vendor != "")
                .Select(a => a.Vendor!)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            ViewBag.Departments = await _context.Assets
                .Where(a => a.EmployeeDepartment != null && a.EmployeeDepartment != "")
                .Select(a => a.EmployeeDepartment!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedVendor = vendor;
            ViewBag.SelectedDepartment = department;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SelectedFlag = flag;
            ViewBag.SelectedSort = sort;
            ViewBag.TotalAssets = await _context.Assets.CountAsync();

            return View(await assets.ToListAsync());
        }

        // GET: Import
        public IActionResult Import()
        {
            ViewBag.Columns = AssetImportService.Columns;
            return View(new AssetImportResult());
        }

        // POST: Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Import(IFormFile? file, bool dryRun)
        {
            ViewBag.Columns = AssetImportService.Columns;

            var result = new AssetImportResult { WasDryRun = dryRun };

            if (file == null || file.Length == 0)
            {
                result.FileErrors.Add("Choose a CSV file to upload.");
                return View(result);
            }

            // Five megabytes of CSV is somewhere north of fifty thousand assets. Anything
            // larger is a mistake, and reading it into a string would be the wrong shape.
            if (file.Length > 5 * 1024 * 1024)
            {
                result.FileErrors.Add("That file is larger than 5 MB.");
                return View(result);
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                result.FileErrors.Add("The importer only reads .csv files. Save the spreadsheet as CSV first.");
                return View(result);
            }

            string csv;

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                csv = await reader.ReadToEndAsync();
            }

            result = await _import.ImportAsync(csv, file.FileName, dryRun);

            if (!dryRun && result.ValidCount > 0)
            {
                TempData["SuccessMessage"] = $"Imported {result.ValidCount} asset(s) from {file.FileName}.";
            }

            return View(result);
        }

        // GET: ImportTemplate - the blank file people fill in.
        public IActionResult ImportTemplate()
        {
            return File(Encoding.UTF8.GetBytes(AssetImportService.TemplateCsv()),
                "text/csv",
                "assetflow-import-template.csv");
        }

        // GET: Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets
                .FirstOrDefaultAsync(m => m.Id == id);
            if (asset == null)
            {
                return NotFound();
            }

            return View(asset);
        }

        // GET: Create
        public IActionResult Create()
        {
            return View();
        }

        // POST:Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asset asset)  
        {
            Console.WriteLine($"=== CREATE FORM SUBMITTED ===");
            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Validation Errors:");
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"  {entry.Key}: {error.ErrorMessage}");
                    }
                }
            }

            Console.WriteLine($"Received Asset Data:");
            Console.WriteLine($"  Name: {asset.Name}");
            Console.WriteLine($"  Serial: {asset.SerialNumber}");
            Console.WriteLine($"  Price: {asset.PurchasePrice}");
            Console.WriteLine($"  Category: {asset.Category}");
            Console.WriteLine($"  Status: {asset.Status}");
            Console.WriteLine($"  Notes: {asset.Notes}");  

            if (ModelState.IsValid)
            {
                try
                {
                    asset.LastUpdated = DateTime.Now;

                    _context.Add(asset);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"SUCCESS: Asset '{asset.Name}' saved to database!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    ModelState.AddModelError("", $"Error saving asset: {ex.Message}");
                }
            }

            return View(asset);
        }

        // GET:Search
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrEmpty(q))
            {
                return RedirectToAction(nameof(Index));
            }

            var assets = await _context.Assets
                .Where(a => a.Name.Contains(q) ||
                           a.SerialNumber.Contains(q) ||
                           a.Location.Contains(q) ||
                           (a.Notes != null && a.Notes.Contains(q)))
                .ToListAsync();

            ViewBag.SearchQuery = q;
            return View(assets);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }
            return View(asset);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,SerialNumber,PurchasePrice,PurchaseDate,Category,Status,Location,Vendor,WarrantyExpiry")] Asset asset)
        {
            if (id != asset.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                   
                    var existingAsset = await _context.Assets.FindAsync(id);
                    if (existingAsset != null)
                    {
                       
                        existingAsset.Name = asset.Name;
                        existingAsset.SerialNumber = asset.SerialNumber;
                        existingAsset.PurchasePrice = asset.PurchasePrice;
                        existingAsset.PurchaseDate = asset.PurchaseDate;
                        existingAsset.Category = asset.Category;
                        existingAsset.Status = asset.Status;
                        existingAsset.Location = asset.Location;
                        existingAsset.Vendor = asset.Vendor;
                        existingAsset.WarrantyExpiry = asset.WarrantyExpiry;
                        existingAsset.LastUpdated = DateTime.Now;

                        _context.Update(existingAsset);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssetExists(asset.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(asset);
        }

        // GET: Checkout
        public async Task<IActionResult> Checkout(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            // Only available assets = checked out
            if (asset.Status != "Available")
            {
                TempData["ErrorMessage"] = $"Asset '{asset.Name}' is not available for checkout. Current status: {asset.Status}";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(asset);
        }

        // POST: Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id,
    string checkedOutToEmployee,
    string employeeEmail,
    string employeeDepartment,
    DateTime expectedReturnDate,
    string? checkoutNotes)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                   
                    asset.CheckedOutToEmployee = checkedOutToEmployee;
                    asset.EmployeeEmail = employeeEmail;
                    asset.EmployeeDepartment = employeeDepartment;
                    asset.CheckoutDate = DateTime.Now;
                    asset.ExpectedReturnDate = expectedReturnDate;
                    asset.CheckoutNotes = checkoutNotes;
                    asset.Status = "CheckedOut";
                    asset.LastUpdated = DateTime.Now;

                    _context.Update(asset);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Asset '{asset.Name}' checked out to {asset.CheckedOutToEmployee}";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error during checkout: {ex.Message}");
                }
            }

            return View(asset);
        }

        // GET: Checkin
        public async Task<IActionResult> Checkin(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            
            if (asset.Status != "CheckedOut")
            {
                TempData["ErrorMessage"] = $"Asset '{asset.Name}' is not checked out. Current status: {asset.Status}";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(asset);
        }

        // POST: Checkin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkin(int id,
    string conditionNotes,
    bool requiresMaintenance)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    asset.ActualReturnDate = DateTime.Now;
                    asset.ConditionNotes = conditionNotes;
                    asset.RequiresMaintenance = requiresMaintenance;
                    asset.Status = requiresMaintenance ? "Maintenance" : "Available";
                    asset.LastUpdated = DateTime.Now;

                  
                    asset.CheckedOutToEmployee = null;
                    asset.EmployeeEmail = null;
                    asset.EmployeeDepartment = null;
                    asset.CheckoutDate = null;
                    asset.ExpectedReturnDate = null;
                    asset.CheckoutNotes = null;

                    _context.Update(asset);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Asset '{asset.Name}' checked in successfully";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error during checkin: {ex.Message}");
                }
            }

            return View(asset);
        }

        // GET: MarkMaintenance
        public async Task<IActionResult> MarkMaintenance(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            return View(asset);
        }

        // POST: MarkMaintenance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMaintenance(int id, DateTime? nextMaintenanceDue, string? maintenanceNotes, int? maintenanceIntervalDays)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            if (maintenanceIntervalDays.HasValue && (maintenanceIntervalDays < 1 || maintenanceIntervalDays > 3650))
            {
                TempData["ErrorMessage"] = "A service interval is between 1 and 3650 days.";
                return RedirectToAction(nameof(MarkMaintenance), new { id });
            }

            try
            {
                asset.Status = "Maintenance";
                asset.RequiresMaintenance = true;
                asset.LastMaintenanceDate = DateTime.Now;
                asset.MaintenanceNotes = maintenanceNotes;
                asset.LastUpdated = DateTime.Now;

                // Blank clears the schedule, so an asset can be taken off recurring
                // servicing without having to edit it somewhere else.
                asset.MaintenanceIntervalDays = maintenanceIntervalDays;

                if (nextMaintenanceDue.HasValue)
                {
                    asset.NextMaintenanceDue = nextMaintenanceDue;
                }
                else if (!maintenanceIntervalDays.HasValue)
                {
                    // Off the schedule with no date typed in means nothing is booked.
                    // Leaving the date the old schedule had put there would fire a
                    // maintenance notice for an asset nobody is servicing any more.
                    asset.NextMaintenanceDue = null;
                }

                _context.Update(asset);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Asset '{asset.Name}' marked for maintenance";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: MarkAvailable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAvailable(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            try
            {
                asset.Status = "Available";
                asset.RequiresMaintenance = false;
                asset.LastUpdated = DateTime.Now;

                // Coming out of maintenance is the moment the service is finished, so
                // this is where the clock resets and the next one gets booked. Without
                // it the schedule was a single date somebody had to retype every time,
                // which is why nothing ever recurred.
                var completed = DateTime.Now;
                asset.LastMaintenanceDate = completed;

                var scheduled = false;

                if (asset.IsOnMaintenanceSchedule)
                {
                    asset.NextMaintenanceDue = completed.Date.AddDays(asset.MaintenanceIntervalDays!.Value);
                    scheduled = true;
                }
                else if (asset.NextMaintenanceDue.HasValue && asset.NextMaintenanceDue.Value.Date <= completed.Date)
                {
                    // Not on a schedule, but the due date that triggered this service is
                    // now in the past. Leaving it there would keep raising the notice
                    // every day forever.
                    asset.NextMaintenanceDue = null;
                }

                _context.Update(asset);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = scheduled
                    ? $"Asset '{asset.Name}' is available again. Next service booked for {asset.NextMaintenanceDue:dd MMM yyyy}."
                    : $"Asset '{asset.Name}' marked as available";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets
                .FirstOrDefaultAsync(m => m.Id == id);
            if (asset == null)
            {
                return NotFound();
            }

            return View(asset);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset != null)
            {
                _context.Assets.Remove(asset);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AssetExists(int id)
        {
            return _context.Assets.Any(e => e.Id == id);
        }
    }
}
