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

namespace HotelBookingManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class AmenitiesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AmenitiesController(ApplicationDbContext db) => _db = db;
        public async Task<IActionResult> Index() => View(await _db.Amenities.ToListAsync());
        public IActionResult Create() => View(new Amenity());
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Amenity m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Amenities.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Thêm tiện nghi thành công.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id) { var a = await _db.Amenities.FindAsync(id); return a == null ? NotFound() : View("Create", a); }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Amenity m) { _db.Amenities.Update(m); await _db.SaveChangesAsync(); TempData["Success"] = "Cập nhật thành công."; return RedirectToAction(nameof(Index)); }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _db.Amenities.FindAsync(id);
            if (a == null) return NotFound();
            _db.Amenities.Remove(a); await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa tiện nghi.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ServicesController(ApplicationDbContext db) => _db = db;
        public async Task<IActionResult> Index() => View(await _db.HotelServices.ToListAsync());
        public IActionResult Create() => View(new HotelService());
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HotelService m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.HotelServices.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Thêm dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id) { var s = await _db.HotelServices.FindAsync(id); return s == null ? NotFound() : View("Create", s); }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HotelService m) { _db.HotelServices.Update(m); await _db.SaveChangesAsync(); TempData["Success"] = "Cập nhật thành công."; return RedirectToAction(nameof(Index)); }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.HotelServices.FindAsync(id);
            if (s == null) return NotFound();
            _db.HotelServices.Remove(s); await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa dịch vụ.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager,HotelStaff")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _booking;
        public BookingsController(ApplicationDbContext db, IBookingService booking) { _db = db; _booking = booking; }

        public async Task<IActionResult> Index(string? q, BookingStatus? status, PaymentStatus? payment)
        {
            var query = _db.Bookings.Include(b => b.User).Include(b => b.Room).ThenInclude(r => r.Hotel).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(b => b.BookingCode.Contains(q) || b.GuestName.Contains(q) || b.GuestEmail.Contains(q) || b.GuestPhone.Contains(q) || (b.GuestIdentityNumber != null && b.GuestIdentityNumber.Contains(q)));
            if (status.HasValue) query = query.Where(b => b.BookingStatus == status);
            if (payment.HasValue) query = query.Where(b => b.PaymentStatus == payment);
            ViewBag.Q = q; ViewBag.Status = status; ViewBag.Payment = payment;
            return View(await query.OrderByDescending(b => b.CreatedAt).Take(200).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var b = await _db.Bookings.Include(x => x.User).Include(x => x.Room).ThenInclude(r => r.Hotel)
                .Include(x => x.BookingServices).ThenInclude(s => s.Service)
                .Include(x => x.Payments).Include(x => x.Voucher)
                .FirstOrDefaultAsync(x => x.BookingId == id);
            return b == null ? NotFound() : View(b);
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
        public async Task<IActionResult> CheckIn(int id, string? note)
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var r = await _booking.CheckInAsync(id, uid, note);
            TempData[r.Ok ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id, string? note)
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var r = await _booking.CheckOutAsync(id, uid, note);
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

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int id, string note)
        {
            var b = await _db.Bookings.FindAsync(id);
            if (b == null) return NotFound();
            b.Note = string.IsNullOrWhiteSpace(b.Note) ? note : b.Note + "\n" + note;
            b.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã lưu ghi chú.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager,HotelStaff")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _pay;
        public PaymentsController(ApplicationDbContext db, IPaymentService pay) { _db = db; _pay = pay; }

        public async Task<IActionResult> Index(string? q, PaymentStatus? status)
        {
            var query = _db.Payments.Include(p => p.Booking).ThenInclude(b => b.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.TransactionCode.Contains(q) || p.Booking.BookingCode.Contains(q));
            if (status.HasValue) query = query.Where(p => p.PaymentStatus == status);
            ViewBag.Q = q; ViewBag.Status = status;
            return View(await query.OrderByDescending(p => p.CreatedAt).Take(200).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var r = await _pay.ConfirmBankTransferAsync(id, uid);
            TempData[r.Ok ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class VouchersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public VouchersController(ApplicationDbContext db) => _db = db;
        public async Task<IActionResult> Index() => View(await _db.Vouchers.OrderByDescending(v => v.StartDate).ToListAsync());
        public IActionResult Create() => View(new Voucher { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(3), Quantity = 100, Status = true });
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher m)
        {
            m.Code = m.Code.ToUpperInvariant();
            if (await _db.Vouchers.AnyAsync(v => v.Code == m.Code)) { ModelState.AddModelError("Code", "Mã đã tồn tại."); return View(m); }
            _db.Vouchers.Add(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Thêm voucher thành công.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id) { var v = await _db.Vouchers.FindAsync(id); return v == null ? NotFound() : View("Create", v); }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Voucher m)
        {
            m.Code = m.Code.ToUpperInvariant();
            _db.Vouchers.Update(m); await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật voucher thành công.";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _db.Vouchers.FindAsync(id);
            if (v == null) return NotFound();
            v.Status = false; await _db.SaveChangesAsync();
            TempData["Success"] = "Đã ngừng voucher.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ReviewsController(ApplicationDbContext db) => _db = db;
        public async Task<IActionResult> Index() =>
            View(await _db.Reviews.Include(r => r.User).Include(r => r.Room).OrderByDescending(r => r.CreatedAt).ToListAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var r = await _db.Reviews.FindAsync(id);
            if (r == null) return NotFound();
            r.Status = !r.Status;
            await _db.SaveChangesAsync();
            TempData["Success"] = r.Status ? "Đã hiện review." : "Đã ẩn review không phù hợp.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ContactsController(ApplicationDbContext db) => _db = db;
        public async Task<IActionResult> Index() => View(await _db.Contacts.OrderByDescending(c => c.CreatedAt).ToListAsync());
        public async Task<IActionResult> Details(int id)
        {
            var c = await _db.Contacts.FindAsync(id);
            return c == null ? NotFound() : View(c);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, ContactStatus status, string? adminNote)
        {
            var c = await _db.Contacts.FindAsync(id);
            if (c == null) return NotFound();
            c.Status = status; c.AdminNote = adminNote; c.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật liên hệ thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ReportsController : Controller
    {
        private readonly IReportService _report;
        public ReportsController(IReportService report) => _report = report;
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var result = await _report.GetAsync(new ReportFilter { From = from, To = to });
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            return View(result);
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class SystemLogsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SystemLogsController(ApplicationDbContext db) => _db = db;
        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.SystemLogs.Include(l => l.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(l => l.Action.Contains(q) || (l.Description != null && l.Description.Contains(q)));
            return View(await query.OrderByDescending(l => l.CreatedAt).Take(300).ToListAsync());
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        public IActionResult Index() => View();
    }
}
