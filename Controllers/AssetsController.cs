using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;

namespace AssetFlow.Controllers
{
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AssetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Assets
        public async Task<IActionResult> Index(string search, string status, string category)
        {
            var assets = _context.Assets.AsQueryable();

            
            if (!string.IsNullOrEmpty(search))
            {
                assets = assets.Where(a =>
                    a.Name.Contains(search) ||
                    a.SerialNumber.Contains(search) ||
                    a.Location.Contains(search));
            }

            
            if (!string.IsNullOrEmpty(status) && status != "All Status")
            {
                assets = assets.Where(a => a.Status == status);
            }

            
            if (!string.IsNullOrEmpty(category) && category != "All Categories")
            {
                assets = assets.Where(a => a.Category == category);
            }

            
            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedCategory = category;

            return View(await assets.ToListAsync());
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
        public async Task<IActionResult> MarkMaintenance(int id, DateTime? nextMaintenanceDue, string? maintenanceNotes)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            try
            {
                asset.Status = "Maintenance";
                asset.RequiresMaintenance = true;
                asset.LastMaintenanceDate = DateTime.Now; 
                asset.MaintenanceNotes = maintenanceNotes; 
                asset.LastUpdated = DateTime.Now;

                if (nextMaintenanceDue.HasValue)
                {
                    asset.NextMaintenanceDue = nextMaintenanceDue;
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

                _context.Update(asset);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Asset '{asset.Name}' marked as available";
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
