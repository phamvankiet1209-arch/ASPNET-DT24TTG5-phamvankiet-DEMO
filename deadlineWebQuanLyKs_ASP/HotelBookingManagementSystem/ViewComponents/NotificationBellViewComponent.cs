using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingManagementSystem.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _noti;
        private readonly UserManager<ApplicationUser> _users;

        public NotificationBellViewComponent(INotificationService noti, UserManager<ApplicationUser> users)
        {
            _noti = noti;
            _users = users;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return View(new NotificationBellModel());

            var user = await _users.GetUserAsync(HttpContext.User);
            if (user == null) return View(new NotificationBellModel());
            var items = await _noti.GetRecentAsync(user.Id);
            var count = await _noti.CountUnreadAsync(user.Id);
            return View(new NotificationBellModel { Items = items, Unread = count });
        }
    }

    public class NotificationBellModel
    {
        public List<Notification> Items { get; set; } = new();
        public int Unread { get; set; }
    }
}
