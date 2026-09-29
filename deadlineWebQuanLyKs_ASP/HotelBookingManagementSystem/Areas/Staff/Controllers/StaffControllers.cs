using HotelBookingManagementSystem.Data;
using HotelBookingManagementSystem.Models.Enums;
using HotelBookingManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "HotelStaff,Admin,Manager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Pending = await _db.Bookings.CountAsync(b => b.BookingStatus == BookingStatus.Pending);
            ViewBag.Confirmed = await _db.Bookings.CountAsync(b => b.BookingStatus == BookingStatus.Confirmed);
            ViewBag.Staying = await _db.Bookings.CountAsync(b => b.BookingStatus == BookingStatus.CheckedIn);
            ViewBag.TodayCheckIn = await _db.Bookings.CountAsync(b => b.CheckInDate.Date == DateTime.Today && (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.Pending));
            ViewBag.TodayCheckOut = await _db.Bookings.CountAsync(b => b.CheckOutDate.Date == DateTime.Today && b.BookingStatus == BookingStatus.CheckedIn);
            ViewBag.Recent = await _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel).Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt).Take(8).ToListAsync();
            return View();
        }
    }

    [Area("Staff")]
    [Authorize(Roles = "HotelStaff,Admin,Manager")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _booking;
        public BookingsController(ApplicationDbContext db, IBookingService booking) { _db = db; _booking = booking; }

        public async Task<IActionResult> Index(string? q, BookingStatus? status)
        {
            var query = _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel).Include(b => b.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(b => b.BookingCode.Contains(q) || b.GuestEmail.Contains(q) || b.GuestPhone.Contains(q) || (b.GuestIdentityNumber != null && b.GuestIdentityNumber.Contains(q)) || b.GuestName.Contains(q));
            if (status.HasValue) query = query.Where(b => b.BookingStatus == status);
            ViewBag.Q = q; ViewBag.Status = status;
            return View(await query.OrderByDescending(b => b.CreatedAt).Take(150).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var b = await _db.Bookings.Include(x => x.Room).ThenInclude(r => r.Hotel)
                .Include(x => x.User).Include(x => x.Payments).Include(x => x.BookingServices).ThenInclude(s => s.Service)
                .FirstOrDefaultAsync(x => x.BookingId == id);
            return b == null ? NotFound() : View(b);
        }

        public IActionResult CheckIn() => View();

        [HttpGet]
        public async Task<IActionResult> SearchGuest(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
            var items = await _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel)
                .Where(b => b.BookingCode.Contains(q) || b.GuestEmail.Contains(q) || b.GuestPhone.Contains(q) || (b.GuestIdentityNumber != null && b.GuestIdentityNumber.Contains(q)))
                .OrderByDescending(b => b.CreatedAt).Take(10)
                .Select(b => new { b.BookingId, b.BookingCode, b.GuestName, b.GuestEmail, b.GuestPhone, room = b.Room.Name, hotel = b.Room.Hotel.Name, status = b.BookingStatus.ToString() })
                .ToListAsync();
            return Json(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoCheckIn(int id, string? note)
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var r = await _booking.CheckInAsync(id, uid, note);
            TempData[r.Ok ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoCheckOut(int id, string? note)
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var r = await _booking.CheckOutAsync(id, uid, note);
            TempData[r.Ok ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var r = await _booking.ConfirmBookingAsync(id, uid);
            TempData[r.Ok ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var r = await _booking.CancelBookingAsync(id, uid, reason ?? "Nhân viên hủy", true);
            TempData[r.Ok ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
