using HotelBookingManagementSystem.Data;
using HotelBookingManagementSystem.Helpers;
using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Models.Enums;
using HotelBookingManagementSystem.Services;
using HotelBookingManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IFileUploadService _upload;

        public ReviewController(ApplicationDbContext db, UserManager<ApplicationUser> users, IFileUploadService upload)
        {
            _db = db;
            _users = users;
            _upload = upload;
        }

        public async Task<IActionResult> Create(int bookingId)
        {
            var user = await _users.GetUserAsync(User);
            var booking = await _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == user!.Id);
            if (booking == null) return RedirectToAction("Error", "Home", new { code = 404 });
            if (booking.BookingStatus != BookingStatus.CheckedOut)
            {
                TempData["Error"] = "Bạn chỉ có thể đánh giá sau khi đã trả phòng.";
                return RedirectToAction("Details", "Booking", new { id = bookingId });
            }
            if (await _db.Reviews.AnyAsync(r => r.BookingId == bookingId))
            {
                TempData["Error"] = "Bạn đã đánh giá đặt phòng này.";
                return RedirectToAction("Details", "Booking", new { id = bookingId });
            }
            ViewBag.Booking = booking;
            return View(new ReviewFormViewModel { BookingId = bookingId, RoomId = booking.RoomId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewFormViewModel model)
        {
            var user = await _users.GetUserAsync(User);
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingId == model.BookingId && b.UserId == user!.Id);
            if (booking == null || booking.BookingStatus != BookingStatus.CheckedOut)
            {
                TempData["Error"] = "Không thể gửi đánh giá.";
                return RedirectToAction("MyBookings", "Booking");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Booking = await _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel).FirstAsync(b => b.BookingId == model.BookingId);
                return View(model);
            }

            string? img = null;
            if (model.Image != null) img = await _upload.UploadAsync(model.Image, "reviews");

            _db.Reviews.Add(new Review
            {
                UserId = user!.Id,
                RoomId = booking.RoomId,
                BookingId = booking.BookingId,
                Rating = model.Rating,
                CleanlinessRating = model.CleanlinessRating,
                LocationRating = model.LocationRating,
                ServiceRating = model.ServiceRating,
                StaffRating = model.StaffRating,
                ValueRating = model.ValueRating,
                Title = model.Title,
                Comment = model.Comment,
                ImageUrl = img,
                Status = true,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cảm ơn bạn đã đánh giá.";
            return RedirectToAction("Details", "Room", new { id = booking.RoomId });
        }
    }

    [Authorize]
    public class FavoriteController : Controller
    {
        private readonly IFavoriteService _fav;
        private readonly UserManager<ApplicationUser> _users;

        public FavoriteController(IFavoriteService fav, UserManager<ApplicationUser> users)
        {
            _fav = fav;
            _users = users;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _users.GetUserAsync(User);
            return View(await _fav.GetByUserAsync(user!.Id));
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int roomId)
        {
            var user = await _users.GetUserAsync(User);
            var result = await _fav.ToggleAsync(user!.Id, roomId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = result.Ok, message = result.Message, isFavorite = await _fav.IsFavoriteAsync(user.Id, roomId) });
            TempData["Success"] = result.Message;
            return RedirectToAction("Details", "Room", new { id = roomId });
        }
    }

    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _noti;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _db;

        public NotificationController(INotificationService noti, UserManager<ApplicationUser> users, ApplicationDbContext db)
        {
            _noti = noti;
            _users = users;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _users.GetUserAsync(User);
            var items = await _db.Notifications.Where(n => n.UserId == user!.Id).OrderByDescending(n => n.CreatedAt).ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var user = await _users.GetUserAsync(User);
            await _noti.MarkReadAsync(id, user!.Id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await _users.GetUserAsync(User);
            await _noti.MarkAllReadAsync(user!.Id);
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Customer,Admin,Manager,HotelStaff")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IRoomQueryService _rooms;

        public CustomerController(ApplicationDbContext db, UserManager<ApplicationUser> users, IRoomQueryService rooms)
        {
            _db = db;
            _users = users;
            _rooms = rooms;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _users.GetUserAsync(User);
            var bookings = _db.Bookings.Where(b => b.UserId == user!.Id);
            ViewBag.Upcoming = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.Pending || b.BookingStatus == BookingStatus.Confirmed);
            ViewBag.Completed = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.CheckedOut);
            ViewBag.Cancelled = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.Cancelled);
            ViewBag.Favorites = await _db.Favorites.CountAsync(f => f.UserId == user!.Id);
            ViewBag.Recent = await _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel)
                .Where(b => b.UserId == user!.Id).OrderByDescending(b => b.CreatedAt).Take(5).ToListAsync();
            ViewBag.Vouchers = await _db.Vouchers.Where(v => v.Status && v.EndDate >= DateTime.Now && v.UsedCount < v.Quantity).Take(6).ToListAsync();
            ViewBag.Suggest = await _rooms.BaseQuery().OrderByDescending(r => r.IsFeatured).Take(4).ToListAsync();
            ViewBag.User = user;
            return View();
        }
    }
}
