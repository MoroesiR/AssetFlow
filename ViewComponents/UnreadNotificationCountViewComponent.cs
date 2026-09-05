using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AssetFlow.Services;

namespace AssetFlow.ViewComponents
{
    // Same reasoning as the pending request badge - the layout has no model, so the
    // count is fetched here rather than pushed into ViewData by every action.
    public class UnreadNotificationCountViewComponent : ViewComponent
    {
        private readonly NotificationService _notifications;

        public UnreadNotificationCountViewComponent(NotificationService notifications)
        {
            _notifications = notifications;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return View(0);
            }

            return View(await _notifications.UnreadCountAsync(userId));
        }
    }
}
