using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;
using AssetFlow.Models;

namespace AssetFlow.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets()
        {
            return await _context.Assets.ToListAsync();
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(int id)
        {
            var asset = await _context.Assets.FindAsync(id);

            if (asset == null)
            {
                return NotFound();
            }

            return asset;
        }

        // GET: search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Asset>>> SearchAssets([FromQuery] string q)
        {
            if (string.IsNullOrEmpty(q))
            {
                return BadRequest("Search query is required");
            }

            var assets = await _context.Assets
                .Where(a => a.Name.Contains(q) || a.SerialNumber.Contains(q))
                .ToListAsync();

            return assets;
        }

        // GET: status=available
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssetsByStatus(string status)
        {
            var assets = await _context.Assets
                .Where(a => a.Status == status)
                .ToListAsync();

            return assets;
        }

        // GET: assets summary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            var total = await _context.Assets.CountAsync();
            var available = await _context.Assets.CountAsync(a => a.Status == "Available");
            var checkedOut = await _context.Assets.CountAsync(a => a.Status == "CheckedOut");
            var maintenance = await _context.Assets.CountAsync(a => a.Status == "Maintenance");
            var totalValue = await _context.Assets.SumAsync(a => a.PurchasePrice);

            return new
            {
                TotalAssets = total,
                Available = available,
                CheckedOut = checkedOut,
                Maintenance = maintenance,
                TotalValue = totalValue
            };
        }
    }
}