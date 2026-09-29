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
    [Authorize(Roles = "Customer,Admin,Manager,HotelStaff")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _booking;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IRoomQueryService _rooms;

        public BookingController(ApplicationDbContext db, IBookingService booking, UserManager<ApplicationUser> users, IRoomQueryService rooms)
        {
            _db = db;
            _booking = booking;
            _users = users;
            _rooms = rooms;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int roomId, DateTime? checkIn, DateTime? checkOut, int adults = 1, int children = 0)
        {
            var room = await _rooms.GetDetailAsync(roomId);
            if (room == null) return RedirectToAction("Error", "Home", new { code = 404 });

            var user = await _users.GetUserAsync(User);
            var ci = checkIn?.Date ?? DateTime.Today.AddDays(1);
            var co = checkOut?.Date ?? ci.AddDays(1);
            if (co <= ci) co = ci.AddDays(1);

            var available = await _booking.CheckRoomAvailabilityAsync(roomId, ci, co);
            if (!available)
            {
                TempData["Error"] = "Phòng không còn trống trong khoảng thời gian đã chọn.";
                return RedirectToAction("Details", "Room", new { id = roomId, checkIn = ci.ToString("yyyy-MM-dd"), checkOut = co.ToString("yyyy-MM-dd") });
            }

            var price = await _booking.CalculateTotalAmountAsync(room, ci, co, 1, null, null);
            ViewBag.Room = room;
            ViewBag.Price = price;
            ViewBag.Services = await _db.HotelServices.Where(s => s.Status).ToListAsync();

            return View(new BookingFormViewModel
            {
                RoomId = roomId,
                GuestName = user?.FullName ?? "",
                GuestEmail = user?.Email ?? "",
                GuestPhone = user?.PhoneNumber ?? "",
                GuestIdentityNumber = user?.IdentityNumber,
                CheckInDate = ci,
                CheckOutDate = co,
                Adults = adults,
                Children = children
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(BookingFormViewModel model)
        {
            var room = await _rooms.GetDetailAsync(model.RoomId);
            if (room == null) return RedirectToAction("Error", "Home", new { code = 404 });
            if (model.CheckInDate.Date < DateTime.Today)
                ModelState.AddModelError("CheckInDate", "Ngày nhận phòng không được ở quá khứ.");
            if (model.CheckOutDate.Date <= model.CheckInDate.Date)
                ModelState.AddModelError("CheckOutDate", "Ngày trả phòng phải sau ngày nhận phòng.");
            if (model.Adults < 1) ModelState.AddModelError("Adults", "Số người lớn tối thiểu là 1.");

            var price = await _booking.CalculateTotalAmountAsync(room, model.CheckInDate, model.CheckOutDate, model.NumberOfRooms, model.ServiceIds, model.VoucherCode);
            ViewBag.Room = room;
            ViewBag.Price = price;
            ViewBag.Services = await _db.HotelServices.Where(s => s.Status).ToListAsync();
            ViewBag.SelectedServices = await _db.HotelServices.Where(s => model.ServiceIds.Contains(s.ServiceId)).ToListAsync();

            if (!ModelState.IsValid) return View("Create", model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(BookingFormViewModel model)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _booking.CreateBookingAsync(new CreateBookingRequest
            {
                UserId = user.Id,
                RoomId = model.RoomId,
                CheckIn = model.CheckInDate,
                CheckOut = model.CheckOutDate,
                Adults = model.Adults,
                Children = model.Children,
                NumberOfRooms = model.NumberOfRooms,
                GuestName = model.GuestName,
                GuestEmail = model.GuestEmail,
                GuestPhone = model.GuestPhone,
                GuestIdentityNumber = model.GuestIdentityNumber,
                SpecialRequest = model.SpecialRequest,
                Note = model.Note,
                VoucherCode = model.VoucherCode,
                ServiceIds = model.ServiceIds
            });

            if (!result.Ok)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Create), new { roomId = model.RoomId, checkIn = model.CheckInDate.ToString("yyyy-MM-dd"), checkOut = model.CheckOutDate.ToString("yyyy-MM-dd") });
            }

            TempData["Success"] = "Đặt phòng thành công. Vui lòng chọn phương thức thanh toán.";
            return RedirectToAction("Checkout", "Payment", new { bookingId = result.Booking!.BookingId });
        }

        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(int roomId, DateTime checkIn, DateTime checkOut, int rooms, string? voucherCode, string? serviceIds)
        {
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null) return Json(new { success = false, message = "Không tìm thấy phòng." });
            var ids = string.IsNullOrWhiteSpace(serviceIds) ? new List<int>() : serviceIds.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();
            var price = await _booking.CalculateTotalAmountAsync(room, checkIn, checkOut, rooms, ids, voucherCode);
            return Json(new
            {
                success = price.VoucherId.HasValue,
                message = price.VoucherMessage ?? "Không áp dụng được mã.",
                discount = price.DiscountAmount,
                discountText = CurrencyHelper.ToVnd(price.DiscountAmount),
                total = price.TotalAmount,
                totalText = CurrencyHelper.ToVnd(price.TotalAmount),
                taxText = CurrencyHelper.ToVnd(price.TaxAmount)
            });
        }

        public async Task<IActionResult> MyBookings(string tab = "upcoming")
        {
            var user = await _users.GetUserAsync(User);
            var query = _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel)
                .Where(b => b.UserId == user!.Id);

            query = tab switch
            {
                "staying" => query.Where(b => b.BookingStatus == BookingStatus.CheckedIn),
                "completed" => query.Where(b => b.BookingStatus == BookingStatus.CheckedOut),
                "cancelled" => query.Where(b => b.BookingStatus == BookingStatus.Cancelled || b.BookingStatus == BookingStatus.NoShow),
                _ => query.Where(b => b.BookingStatus == BookingStatus.Pending || b.BookingStatus == BookingStatus.Confirmed)
            };

            ViewBag.Tab = tab;
            var items = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _users.GetUserAsync(User);
            var booking = await _db.Bookings
                .Include(b => b.Room).ThenInclude(r => r.Hotel)
                .Include(b => b.Room).ThenInclude(r => r.Images)
                .Include(b => b.BookingServices).ThenInclude(s => s.Service)
                .Include(b => b.Payments)
                .Include(b => b.Voucher)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return RedirectToAction("Error", "Home", new { code = 404 });
            var isStaff = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.HotelStaff);
            if (!isStaff && booking.UserId != user!.Id) return RedirectToAction("Error", "Home", new { code = 403 });
            return View(booking);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var user = await _users.GetUserAsync(User);
            var result = await _booking.CancelBookingAsync(id, user!.Id, reason ?? "Khách hàng yêu cầu hủy");
            TempData[result.Ok ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Print(int id)
        {
            var booking = await LoadBooking(id);
            if (booking == null) return RedirectToAction("Error", "Home", new { code = 404 });
            return View(booking);
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var booking = await LoadBooking(id);
            if (booking == null) return RedirectToAction("Error", "Home", new { code = 404 });
            return View(booking);
        }

        public async Task<IActionResult> Rebook(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return RedirectToAction("Error", "Home", new { code = 404 });
            return RedirectToAction(nameof(Create), new { roomId = booking.RoomId });
        }

        private async Task<Booking?> LoadBooking(int id)
        {
            var user = await _users.GetUserAsync(User);
            var booking = await _db.Bookings
                .Include(b => b.Room).ThenInclude(r => r.Hotel)
                .Include(b => b.BookingServices).ThenInclude(s => s.Service)
                .Include(b => b.Payments)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null) return null;
            var isStaff = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.HotelStaff);
            if (!isStaff && booking.UserId != user!.Id) return null;
            return booking;
        }
    }

    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _payments;
        private readonly UserManager<ApplicationUser> _users;

        public PaymentController(ApplicationDbContext db, IPaymentService payments, UserManager<ApplicationUser> users)
        {
            _db = db;
            _payments = payments;
            _users = users;
        }

        public async Task<IActionResult> Checkout(int bookingId)
        {
            var booking = await _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
            var user = await _users.GetUserAsync(User);
            if (booking == null || booking.UserId != user!.Id) return RedirectToAction("Error", "Home", new { code = 404 });
            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                TempData["Success"] = "Đặt phòng đã được thanh toán.";
                return RedirectToAction("Details", "Booking", new { id = bookingId });
            }
            return View(booking);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(PaymentCheckoutViewModel model)
        {
            var user = await _users.GetUserAsync(User);
            var result = await _payments.CreatePaymentAsync(model.BookingId, model.PaymentMethod, model.Note, user!.Id);
            if (!result.Ok)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Checkout), new { bookingId = model.BookingId });
            }

            if (model.PaymentMethod is PaymentMethod.VNPay or PaymentMethod.MoMo or PaymentMethod.PayOS)
                return RedirectToAction(nameof(Gateway), new { paymentId = result.Payment!.PaymentId });

            if (model.PaymentMethod == PaymentMethod.BankTransfer)
                return RedirectToAction(nameof(BankInfo), new { paymentId = result.Payment!.PaymentId });

            return RedirectToAction(nameof(Result), new { paymentId = result.Payment!.PaymentId, success = true });
        }

        public async Task<IActionResult> Gateway(int paymentId)
        {
            var payment = await _db.Payments.Include(p => p.Booking).ThenInclude(b => b.Room).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null) return RedirectToAction("Error", "Home", new { code = 404 });
            return View(payment);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Simulate(int paymentId, bool success)
        {
            var user = await _users.GetUserAsync(User);
            var result = await _payments.SimulateGatewayResultAsync(paymentId, success, user!.Id);
            TempData[result.Ok ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Result), new { paymentId, success = result.Ok });
        }

        public async Task<IActionResult> BankInfo(int paymentId)
        {
            var payment = await _db.Payments.Include(p => p.Booking).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null) return RedirectToAction("Error", "Home", new { code = 404 });
            return View(payment);
        }

        public async Task<IActionResult> Result(int paymentId, bool success = true)
        {
            var payment = await _db.Payments.Include(p => p.Booking).ThenInclude(b => b.Room).ThenInclude(r => r.Hotel)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null) return RedirectToAction("Error", "Home", new { code = 404 });
            ViewBag.Success = success && payment.PaymentStatus != PaymentStatus.Failed;
            return View(payment);
        }
    }
}
