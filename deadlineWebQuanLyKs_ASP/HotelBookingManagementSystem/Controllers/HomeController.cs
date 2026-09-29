using System.Diagnostics;
using HotelBookingManagementSystem.Data;
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
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IRoomQueryService _rooms;

        public HomeController(ApplicationDbContext db, IRoomQueryService rooms)
        {
            _db = db;
            _rooms = rooms;
        }

        public async Task<IActionResult> Index()
        {
            var hotels = await _db.Hotels.Where(h => h.Status).Include(h => h.Rooms).ThenInclude(r => r.Reviews).OrderByDescending(h => h.StarRating).Take(5).ToListAsync();
            var featured = await _rooms.BaseQuery().Where(r => r.IsFeatured).OrderByDescending(r => r.ViewCount).Take(8).ToListAsync();
            var promo = await _rooms.BaseQuery().Where(r => r.DiscountPrice != null && r.DiscountPrice > 0 && r.DiscountPrice < r.PricePerNight).Take(6).ToListAsync();
            var reviews = await _db.Reviews.Include(r => r.User).Include(r => r.Room).ThenInclude(x => x.Hotel)
                .Where(r => r.Status).OrderByDescending(r => r.CreatedAt).Take(6).ToListAsync();
            var cities = await _db.Hotels.Where(h => h.Status).GroupBy(h => h.City).Select(g => new { City = g.Key, Count = g.Count() }).ToListAsync();

            ViewBag.Hotels = hotels;
            ViewBag.FeaturedRooms = featured;
            ViewBag.PromoRooms = promo;
            ViewBag.Reviews = reviews;
            ViewBag.Cities = cities;
            ViewBag.CheckIn = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            ViewBag.CheckOut = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");
            return View();
        }

        public IActionResult About() => View();
        public IActionResult Privacy() => View();
        public IActionResult Terms() => View();
        public IActionResult BookingPolicy() => View();

        [HttpGet]
        public IActionResult Contact()
        {
            var model = new ContactViewModel();
            if (User.Identity?.IsAuthenticated == true)
            {
                model.FullName = User.FindFirst("FullName")?.Value ?? User.Identity.Name ?? "";
                model.Email = User.Identity.Name ?? "";
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _db.Contacts.Add(new Contact
            {
                UserId = userId,
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Subject = model.Subject,
                Message = model.Message,
                Status = ContactStatus.New,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi sớm nhất.";
            return RedirectToAction(nameof(Contact));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Newsletter(string email)
        {
            TempData["Success"] = "Đăng ký nhận ưu đãi thành công!";
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int code = 500)
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = code,
                Title = code switch
                {
                    404 => "Không tìm thấy trang",
                    403 => "Không có quyền truy cập",
                    _ => "Đã xảy ra lỗi hệ thống"
                },
                Message = code switch
                {
                    404 => "Trang bạn tìm kiếm không tồn tại hoặc đã được di chuyển.",
                    403 => "Bạn không có quyền truy cập nội dung này.",
                    _ => "Xin lỗi, hệ thống đang gặp sự cố. Vui lòng thử lại sau."
                }
            };
            return View(model);
        }
    }
}
