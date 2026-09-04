using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetFlow.Data;

namespace AssetFlow.ViewComponents
{
    // The nav badge needs a live count and the layout has no model of its own. Doing
    // it here beats stuffing the number into ViewData from every single action.
    public class PendingRequestCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public PendingRequestCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var pending = await _context.AssetRequests.CountAsync(r => r.Status == "Pending");

            return View(pending);
        }
    }
}
