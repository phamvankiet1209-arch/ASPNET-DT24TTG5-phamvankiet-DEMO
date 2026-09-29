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
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dash;
        public DashboardController(IDashboardService dash) => _dash = dash;

        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = User.IsInRole(RoleNames.Admin);
            return View(await _dash.GetAdminStatsAsync());
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class HotelsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _upload;
        private readonly ILogService _log;
        public HotelsController(ApplicationDbContext db, IFileUploadService upload, ILogService log)
        { _db = db; _upload = upload; _log = log; }

        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.Hotels.Include(h => h.Rooms).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(h => h.Name.Contains(q) || h.City.Contains(q));
            ViewBag.Q = q;
            return View(await query.OrderBy(h => h.Name).ToListAsync());
        }

        public IActionResult Create() => View(new HotelFormViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HotelFormViewModel m)
        {
            if (!ModelState.IsValid) return View(m);
            var hotel = Map(m, new Hotel { CreatedAt = DateTime.Now, Slug = SlugHelper.Generate(m.Name) });
            if (m.ThumbnailFile != null) hotel.Thumbnail = await _upload.UploadAsync(m.ThumbnailFile, "hotels");
            _db.Hotels.Add(hotel);
            await _db.SaveChangesAsync();
            await _log.LogAsync("Create", "Hotel", hotel.HotelId.ToString(), hotel.Name, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            TempData["Success"] = "Thêm khách sạn thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var h = await _db.Hotels.FindAsync(id);
            if (h == null) return NotFound();
            return View("Create", new HotelFormViewModel
            {
                HotelId = h.HotelId, Name = h.Name, Description = h.Description, Address = h.Address, City = h.City,
                District = h.District, Phone = h.Phone, Email = h.Email, Website = h.Website, Latitude = h.Latitude,
                Longitude = h.Longitude, StarRating = h.StarRating, CheckInTime = h.CheckInTime, CheckOutTime = h.CheckOutTime,
                Thumbnail = h.Thumbnail, Status = h.Status
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HotelFormViewModel m)
        {
            if (!ModelState.IsValid) return View("Create", m);
            var h = await _db.Hotels.FindAsync(m.HotelId);
            if (h == null) return NotFound();
            Map(m, h);
            h.Slug = SlugHelper.Generate(m.Name);
            h.UpdatedAt = DateTime.Now;
            if (m.ThumbnailFile != null) h.Thumbnail = await _upload.UploadAsync(m.ThumbnailFile, "hotels");
            await _db.SaveChangesAsync();
            await _log.LogAsync("Update", "Hotel", h.HotelId.ToString(), h.Name, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            TempData["Success"] = "Cập nhật khách sạn thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var h = await _db.Hotels.Include(x => x.Rooms).Include(x => x.Bookings).FirstOrDefaultAsync(x => x.HotelId == id);
            if (h == null) return NotFound();
            if (h.Bookings.Any())
            {
                h.Status = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Khách sạn đã có giao dịch nên được ẩn thay vì xóa.";
            }
            else
            {
                _db.Hotels.Remove(h);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa khách sạn.";
            }
            return RedirectToAction(nameof(Index));
        }

        private static Hotel Map(HotelFormViewModel m, Hotel h)
        {
            h.Name = m.Name; h.Description = m.Description; h.Address = m.Address; h.City = m.City; h.District = m.District;
            h.Phone = m.Phone; h.Email = m.Email; h.Website = m.Website; h.Latitude = m.Latitude; h.Longitude = m.Longitude;
            h.StarRating = m.StarRating; h.CheckInTime = m.CheckInTime; h.CheckOutTime = m.CheckOutTime; h.Status = m.Status;
            return h;
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class RoomTypesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public RoomTypesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index() => View(await _db.RoomTypes.Include(t => t.Rooms).ToListAsync());

        public IActionResult Create() => View(new RoomType());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomType m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.RoomTypes.Add(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Thêm loại phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var t = await _db.RoomTypes.FindAsync(id);
            return t == null ? NotFound() : View("Create", t);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoomType m)
        {
            if (!ModelState.IsValid) return View("Create", m);
            _db.RoomTypes.Update(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật loại phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _db.RoomTypes.Include(x => x.Rooms).FirstOrDefaultAsync(x => x.RoomTypeId == id);
            if (t == null) return NotFound();
            if (t.Rooms.Any()) { TempData["Error"] = "Không thể xóa loại phòng đang được sử dụng."; return RedirectToAction(nameof(Index)); }
            _db.RoomTypes.Remove(t);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa loại phòng.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _upload;
        private readonly ILogService _log;
        public RoomsController(ApplicationDbContext db, IFileUploadService upload, ILogService log)
        { _db = db; _upload = upload; _log = log; }

        public async Task<IActionResult> Index(int? hotelId, string? q)
        {
            var query = _db.Rooms.Include(r => r.Hotel).Include(r => r.RoomType).AsQueryable();
            if (hotelId.HasValue) query = query.Where(r => r.HotelId == hotelId);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(r => r.Name.Contains(q) || r.RoomNumber.Contains(q));
            ViewBag.Hotels = await _db.Hotels.ToListAsync();
            ViewBag.HotelId = hotelId; ViewBag.Q = q;
            return View(await query.OrderBy(r => r.HotelId).ThenBy(r => r.RoomNumber).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await Fill();
            return View(new RoomFormViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomFormViewModel m)
        {
            if (!ModelState.IsValid) { await Fill(); return View(m); }
            var room = Map(m, new Room { CreatedAt = DateTime.Now, Slug = SlugHelper.Generate(m.Name + "-" + m.RoomNumber) });
            if (m.ThumbnailFile != null) room.Thumbnail = await _upload.UploadAsync(m.ThumbnailFile, "rooms");
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();
            await SaveAmenities(room.RoomId, m.AmenityIds);
            await SaveGallery(room, m);
            await _log.LogAsync("Create", "Room", room.RoomId.ToString(), room.Name, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            TempData["Success"] = "Thêm phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var r = await _db.Rooms.Include(x => x.RoomAmenities).FirstOrDefaultAsync(x => x.RoomId == id);
            if (r == null) return NotFound();
            await Fill();
            return View("Create", new RoomFormViewModel
            {
                RoomId = r.RoomId, HotelId = r.HotelId, RoomTypeId = r.RoomTypeId, RoomNumber = r.RoomNumber, Name = r.Name,
                Description = r.Description, Floor = r.Floor, Area = r.Area, PricePerNight = r.PricePerNight, DiscountPrice = r.DiscountPrice,
                AdultCapacity = r.AdultCapacity, ChildCapacity = r.ChildCapacity, BedType = r.BedType, NumberOfBeds = r.NumberOfBeds,
                ViewType = r.ViewType, SmokingAllowed = r.SmokingAllowed, BreakfastIncluded = r.BreakfastIncluded, HasBalcony = r.HasBalcony,
                HasNiceView = r.HasNiceView, HasAirConditioner = r.HasAirConditioner, HasWifi = r.HasWifi, HasBathtub = r.HasBathtub,
                Status = r.Status, CancellationPolicy = r.CancellationPolicy, ExtraFee = r.ExtraFee, IsFeatured = r.IsFeatured,
                Thumbnail = r.Thumbnail, AmenityIds = r.RoomAmenities.Select(a => a.AmenityId).ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoomFormViewModel m)
        {
            if (!ModelState.IsValid) { await Fill(); return View("Create", m); }
            var r = await _db.Rooms.FindAsync(m.RoomId);
            if (r == null) return NotFound();
            Map(m, r);
            r.Slug = SlugHelper.Generate(m.Name + "-" + m.RoomNumber);
            r.UpdatedAt = DateTime.Now;
            if (m.ThumbnailFile != null) r.Thumbnail = await _upload.UploadAsync(m.ThumbnailFile, "rooms");
            await SaveAmenities(r.RoomId, m.AmenityIds);
            await SaveGallery(r, m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.Rooms.Include(x => x.Bookings).FirstOrDefaultAsync(x => x.RoomId == id);
            if (r == null) return NotFound();
            if (r.Bookings.Any()) { r.Status = RoomStatus.Inactive; await _db.SaveChangesAsync(); TempData["Success"] = "Phòng đã có booking nên được ngừng hoạt động."; }
            else { _db.Rooms.Remove(r); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa phòng."; }
            return RedirectToAction(nameof(Index));
        }

        private async Task Fill()
        {
            ViewBag.Hotels = await _db.Hotels.ToListAsync();
            ViewBag.RoomTypes = await _db.RoomTypes.ToListAsync();
            ViewBag.Amenities = await _db.Amenities.ToListAsync();
        }

        private static Room Map(RoomFormViewModel m, Room r)
        {
            r.HotelId = m.HotelId; r.RoomTypeId = m.RoomTypeId; r.RoomNumber = m.RoomNumber; r.Name = m.Name; r.Description = m.Description;
            r.Floor = m.Floor; r.Area = m.Area; r.PricePerNight = m.PricePerNight; r.DiscountPrice = m.DiscountPrice;
            r.AdultCapacity = m.AdultCapacity; r.ChildCapacity = m.ChildCapacity; r.BedType = m.BedType; r.NumberOfBeds = m.NumberOfBeds;
            r.ViewType = m.ViewType; r.SmokingAllowed = m.SmokingAllowed; r.BreakfastIncluded = m.BreakfastIncluded; r.HasBalcony = m.HasBalcony;
            r.HasNiceView = m.HasNiceView; r.HasAirConditioner = m.HasAirConditioner; r.HasWifi = m.HasWifi; r.HasBathtub = m.HasBathtub;
            r.Status = m.Status; r.CancellationPolicy = m.CancellationPolicy; r.ExtraFee = m.ExtraFee; r.IsFeatured = m.IsFeatured;
            return r;
        }

        private async Task SaveAmenities(int roomId, List<int> ids)
        {
            var old = _db.RoomAmenities.Where(x => x.RoomId == roomId);
            _db.RoomAmenities.RemoveRange(old);
            foreach (var id in ids.Distinct())
                _db.RoomAmenities.Add(new RoomAmenity { RoomId = roomId, AmenityId = id });
            await _db.SaveChangesAsync();
        }

        private async Task SaveGallery(Room room, RoomFormViewModel m)
        {
            if (m.GalleryFiles == null) return;
            var order = await _db.RoomImages.CountAsync(i => i.RoomId == room.RoomId);
            foreach (var f in m.GalleryFiles)
            {
                var path = await _upload.UploadAsync(f, "rooms");
                if (path == null) continue;
                _db.RoomImages.Add(new RoomImage { RoomId = room.RoomId, ImageUrl = path, DisplayOrder = ++order, IsThumbnail = false });
            }
            await _db.SaveChangesAsync();
        }
    }
}
