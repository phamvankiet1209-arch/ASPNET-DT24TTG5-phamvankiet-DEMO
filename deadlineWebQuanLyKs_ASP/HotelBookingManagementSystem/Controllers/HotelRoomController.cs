using HotelBookingManagementSystem.Data;
using HotelBookingManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Controllers
{
    public class HotelController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IRoomQueryService _rooms;

        public HotelController(ApplicationDbContext db, IRoomQueryService rooms)
        {
            _db = db;
            _rooms = rooms;
        }

        public async Task<IActionResult> Index(string? city, string? q, int page = 1)
        {
            var query = _db.Hotels.Include(h => h.Rooms).ThenInclude(r => r.Reviews).Where(h => h.Status);
            if (!string.IsNullOrWhiteSpace(city)) query = query.Where(h => h.City.Contains(city));
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(h => h.Name.Contains(q) || h.Address.Contains(q));
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(h => h.StarRating).Skip((page - 1) * 9).Take(9).ToListAsync();
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 9.0);
            ViewBag.City = city;
            ViewBag.Q = q;
            ViewBag.Cities = await _db.Hotels.Select(h => h.City).Distinct().ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var hotel = await _db.Hotels
                .Include(h => h.Rooms).ThenInclude(r => r.Reviews).ThenInclude(rv => rv.User)
                .Include(h => h.Rooms).ThenInclude(r => r.RoomType)
                .Include(h => h.Rooms).ThenInclude(r => r.Images)
                .FirstOrDefaultAsync(h => h.HotelId == id && h.Status);
            if (hotel == null) return RedirectToAction("Error", "Home", new { code = 404 });
            ViewBag.Rooms = hotel.Rooms.Where(r => r.Status != Models.Enums.RoomStatus.Inactive).OrderBy(r => r.DisplayPrice).ToList();
            return View(hotel);
        }
    }

    public class RoomController : Controller
    {
        private readonly IRoomQueryService _rooms;
        private readonly IBookingService _booking;
        private readonly ApplicationDbContext _db;
        private readonly IFavoriteService _fav;

        public RoomController(IRoomQueryService rooms, IBookingService booking, ApplicationDbContext db, IFavoriteService fav)
        {
            _rooms = rooms;
            _booking = booking;
            _db = db;
            _fav = fav;
        }

        public async Task<IActionResult> Index(RoomSearchFilter filter)
        {
            if (filter.Page < 1) filter.Page = 1;
            var (items, total) = await _rooms.SearchAsync(filter);
            ViewBag.Filter = filter;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize);
            ViewBag.Hotels = await _db.Hotels.Where(h => h.Status).ToListAsync();
            ViewBag.RoomTypes = await _db.RoomTypes.Where(t => t.Status).ToListAsync();
            ViewBag.Amenities = await _db.Amenities.Where(a => a.Status).ToListAsync();
            ViewBag.Cities = await _db.Hotels.Select(h => h.City).Distinct().ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Promotions()
        {
            var filter = new RoomSearchFilter { PromotionOnly = true, PageSize = 24 };
            var (items, _) = await _rooms.SearchAsync(filter);
            return View(items);
        }

        public async Task<IActionResult> Details(int id, DateTime? checkIn, DateTime? checkOut)
        {
            var room = await _rooms.GetDetailAsync(id);
            if (room == null) return RedirectToAction("Error", "Home", new { code = 404 });
            room.ViewCount++;
            await _db.SaveChangesAsync();

            ViewBag.Similar = await _rooms.GetSimilarAsync(room);
            ViewBag.CheckIn = (checkIn ?? DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd");
            ViewBag.CheckOut = (checkOut ?? DateTime.Today.AddDays(2)).ToString("yyyy-MM-dd");
            ViewBag.Services = await _db.HotelServices.Where(s => s.Status).ToListAsync();
            if (User.Identity?.IsAuthenticated == true)
            {
                var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
                ViewBag.IsFavorite = await _fav.IsFavoriteAsync(uid, id);
            }
            return View(room);
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int roomId, DateTime checkIn, DateTime checkOut)
        {
            if (checkIn.Date < DateTime.Today)
                return Json(new { available = false, message = "Ngày nhận phòng không được ở quá khứ." });
            if (checkOut.Date <= checkIn.Date)
                return Json(new { available = false, message = "Ngày trả phòng phải sau ngày nhận phòng." });

            var available = await _booking.CheckRoomAvailabilityAsync(roomId, checkIn, checkOut);
            var room = await _db.Rooms.FindAsync(roomId);
            PriceBreakdown? price = null;
            if (available && room != null)
                price = await _booking.CalculateTotalAmountAsync(room, checkIn, checkOut, 1, null, null);

            return Json(new
            {
                available,
                message = available ? "Phòng còn trống trong khoảng thời gian này." : "Phòng đã được đặt trong khoảng thời gian này.",
                nights = price?.Nights,
                total = price?.TotalAmount,
                totalText = price == null ? null : Helpers.CurrencyHelper.ToVnd(price.TotalAmount)
            });
        }
    }
}
