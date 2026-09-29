using HotelBookingManagementSystem.Data;
using HotelBookingManagementSystem.Helpers;
using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Services
{
    public interface IPaymentService
    {
        Task<(bool Ok, string Message, Payment? Payment)> CreatePaymentAsync(int bookingId, PaymentMethod method, string? note, string userId);
        Task<(bool Ok, string Message)> SimulateGatewayResultAsync(int paymentId, bool success, string userId);
        Task<(bool Ok, string Message)> ConfirmBankTransferAsync(int paymentId, string staffId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notification;
        private readonly ILogService _log;
        public PaymentService(ApplicationDbContext db, INotificationService notification, ILogService log)
        {
            _db = db;
            _notification = notification;
            _log = log;
        }

        public async Task<(bool Ok, string Message, Payment? Payment)> CreatePaymentAsync(int bookingId, PaymentMethod method, string? note, string userId)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng.", null);
            if (booking.UserId != userId) return (false, "Bạn không có quyền thanh toán đặt phòng này.", null);
            if (booking.BookingStatus == BookingStatus.Cancelled)
                return (false, "Đặt phòng đã bị hủy.", null);
            if (booking.PaymentStatus == PaymentStatus.Paid)
                return (false, "Đặt phòng đã được thanh toán.", null);

            var payment = new Payment
            {
                BookingId = booking.BookingId,
                TransactionCode = $"TX{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}",
                PaymentMethod = method,
                Amount = booking.TotalAmount,
                PaymentNote = note,
                CreatedAt = DateTime.Now
            };

            if (method == PaymentMethod.PayAtHotel)
            {
                payment.PaymentStatus = PaymentStatus.Unpaid;
                booking.PaymentStatus = PaymentStatus.Unpaid;
                if (booking.BookingStatus == BookingStatus.Pending)
                    booking.BookingStatus = BookingStatus.Confirmed;
            }
            else if (method == PaymentMethod.BankTransfer)
            {
                payment.PaymentStatus = PaymentStatus.Pending;
                booking.PaymentStatus = PaymentStatus.Pending;
            }
            else
            {
                payment.PaymentStatus = PaymentStatus.Pending;
                booking.PaymentStatus = PaymentStatus.Pending;
            }

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            await _log.LogAsync("CreatePayment", "Payment", payment.PaymentId.ToString(),
                $"Tạo thanh toán {payment.TransactionCode} - {EnumDisplayHelper.PaymentMethod(method)}", userId);

            return (true, "Tạo giao dịch thanh toán thành công.", payment);
        }

        public async Task<(bool Ok, string Message)> SimulateGatewayResultAsync(int paymentId, bool success, string userId)
        {
            var payment = await _db.Payments.Include(p => p.Booking).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null) return (false, "Không tìm thấy giao dịch.");
            if (payment.Booking.UserId != userId) return (false, "Không có quyền.");

            if (success)
            {
                payment.PaymentStatus = PaymentStatus.Paid;
                payment.PaidAt = DateTime.Now;
                payment.Booking.PaymentStatus = PaymentStatus.Paid;
                if (payment.Booking.BookingStatus == BookingStatus.Pending)
                    payment.Booking.BookingStatus = BookingStatus.Confirmed;
                await _db.SaveChangesAsync();
                await _notification.NotifyAsync(userId, "Thanh toán thành công",
                    $"Giao dịch {payment.TransactionCode} đã được thanh toán. Đặt phòng {payment.Booking.BookingCode} đã xác nhận.",
                    NotificationType.PaymentSuccess);
                await _log.LogAsync("PaymentSuccess", "Payment", payment.PaymentId.ToString(), payment.TransactionCode, userId);
                return (true, "Thanh toán thành công.");
            }

            payment.PaymentStatus = PaymentStatus.Failed;
            payment.Booking.PaymentStatus = PaymentStatus.Failed;
            await _db.SaveChangesAsync();
            await _log.LogAsync("PaymentFailed", "Payment", payment.PaymentId.ToString(), payment.TransactionCode, userId);
            return (false, "Thanh toán thất bại. Vui lòng thử lại.");
        }

        public async Task<(bool Ok, string Message)> ConfirmBankTransferAsync(int paymentId, string staffId)
        {
            var payment = await _db.Payments.Include(p => p.Booking).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null) return (false, "Không tìm thấy giao dịch.");

            payment.PaymentStatus = PaymentStatus.Paid;
            payment.PaidAt = DateTime.Now;
            payment.Booking.PaymentStatus = PaymentStatus.Paid;
            if (payment.Booking.BookingStatus == BookingStatus.Pending)
                payment.Booking.BookingStatus = BookingStatus.Confirmed;
            await _db.SaveChangesAsync();
            await _notification.NotifyAsync(payment.Booking.UserId, "Thanh toán thành công",
                $"Chuyển khoản {payment.TransactionCode} đã được xác nhận.", NotificationType.PaymentSuccess);
            await _log.LogAsync("ConfirmTransfer", "Payment", payment.PaymentId.ToString(), "Xác nhận chuyển khoản", staffId);
            return (true, "Xác nhận thanh toán thành công.");
        }
    }

    public class RoomSearchFilter
    {
        public string? Keyword { get; set; }
        public string? City { get; set; }
        public int? HotelId { get; set; }
        public int? RoomTypeId { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int Adults { get; set; } = 1;
        public int Children { get; set; }
        public int NumberOfRooms { get; set; } = 1;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Beds { get; set; }
        public int? MaxGuests { get; set; }
        public decimal? MinArea { get; set; }
        public double? MinRating { get; set; }
        public bool? SmokingAllowed { get; set; }
        public bool BreakfastIncluded { get; set; }
        public bool HasBalcony { get; set; }
        public bool HasNiceView { get; set; }
        public bool HasAirConditioner { get; set; }
        public bool HasWifi { get; set; }
        public bool HasBathtub { get; set; }
        public bool PromotionOnly { get; set; }
        public List<int> AmenityIds { get; set; } = new();
        public string Sort { get; set; } = "popular";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
    }

    public interface IRoomQueryService
    {
        Task<(List<Room> Items, int Total)> SearchAsync(RoomSearchFilter filter);
        Task<Room?> GetDetailAsync(int id);
        Task<Room?> GetBySlugAsync(string slug);
        Task<List<Room>> GetSimilarAsync(Room room, int take = 4);
        IQueryable<Room> BaseQuery();
    }

    public class RoomQueryService : IRoomQueryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _booking;

        public RoomQueryService(ApplicationDbContext db, IBookingService booking)
        {
            _db = db;
            _booking = booking;
        }

        public IQueryable<Room> BaseQuery() =>
            _db.Rooms
                .Include(r => r.Hotel)
                .Include(r => r.RoomType)
                .Include(r => r.Images)
                .Include(r => r.Reviews).ThenInclude(rv => rv.User)
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .Where(r => r.Status != RoomStatus.Inactive);

        public async Task<(List<Room> Items, int Total)> SearchAsync(RoomSearchFilter f)
        {
            var q = BaseQuery();

            if (!string.IsNullOrWhiteSpace(f.Keyword))
            {
                var k = f.Keyword.Trim();
                q = q.Where(r => r.Name.Contains(k) || r.Hotel.Name.Contains(k) || r.Hotel.Address.Contains(k) || r.Description.Contains(k));
            }
            if (!string.IsNullOrWhiteSpace(f.City))
                q = q.Where(r => r.Hotel.City.Contains(f.City));
            if (f.HotelId.HasValue) q = q.Where(r => r.HotelId == f.HotelId);
            if (f.RoomTypeId.HasValue) q = q.Where(r => r.RoomTypeId == f.RoomTypeId);
            if (f.Adults > 0) q = q.Where(r => r.AdultCapacity >= f.Adults);
            if (f.Children > 0) q = q.Where(r => r.ChildCapacity >= f.Children);
            if (f.MinPrice.HasValue) q = q.Where(r => (r.DiscountPrice ?? r.PricePerNight) >= f.MinPrice);
            if (f.MaxPrice.HasValue) q = q.Where(r => (r.DiscountPrice ?? r.PricePerNight) <= f.MaxPrice);
            if (f.Beds.HasValue) q = q.Where(r => r.NumberOfBeds >= f.Beds);
            if (f.MaxGuests.HasValue) q = q.Where(r => r.AdultCapacity + r.ChildCapacity >= f.MaxGuests);
            if (f.MinArea.HasValue) q = q.Where(r => r.Area >= f.MinArea);
            if (f.SmokingAllowed.HasValue) q = q.Where(r => r.SmokingAllowed == f.SmokingAllowed);
            if (f.BreakfastIncluded) q = q.Where(r => r.BreakfastIncluded);
            if (f.HasBalcony) q = q.Where(r => r.HasBalcony);
            if (f.HasNiceView) q = q.Where(r => r.HasNiceView);
            if (f.HasAirConditioner) q = q.Where(r => r.HasAirConditioner);
            if (f.HasWifi) q = q.Where(r => r.HasWifi);
            if (f.HasBathtub) q = q.Where(r => r.HasBathtub);
            if (f.PromotionOnly) q = q.Where(r => r.DiscountPrice != null && r.DiscountPrice > 0 && r.DiscountPrice < r.PricePerNight);
            if (f.AmenityIds.Count > 0)
                q = q.Where(r => f.AmenityIds.All(id => r.RoomAmenities.Any(a => a.AmenityId == id)));

            var rooms = await q.ToListAsync();

            if (f.MinRating.HasValue)
                rooms = rooms.Where(r => r.AverageRating >= f.MinRating.Value).ToList();

            if (f.CheckIn.HasValue && f.CheckOut.HasValue && f.CheckOut > f.CheckIn)
            {
                var available = new List<Room>();
                foreach (var room in rooms)
                {
                    if (await _booking.CheckRoomAvailabilityAsync(room.RoomId, f.CheckIn.Value, f.CheckOut.Value))
                        available.Add(room);
                }
                rooms = available;
            }

            rooms = f.Sort switch
            {
                "price_asc" => rooms.OrderBy(r => r.DisplayPrice).ToList(),
                "price_desc" => rooms.OrderByDescending(r => r.DisplayPrice).ToList(),
                "rating" => rooms.OrderByDescending(r => r.AverageRating).ToList(),
                "newest" => rooms.OrderByDescending(r => r.CreatedAt).ToList(),
                _ => rooms.OrderByDescending(r => r.IsFeatured).ThenByDescending(r => r.ViewCount).ThenByDescending(r => r.AverageRating).ToList()
            };

            var total = rooms.Count;
            var page = Math.Max(1, f.Page);
            var items = rooms.Skip((page - 1) * f.PageSize).Take(f.PageSize).ToList();
            return (items, total);
        }

        public Task<Room?> GetDetailAsync(int id) =>
            BaseQuery().FirstOrDefaultAsync(r => r.RoomId == id);

        public Task<Room?> GetBySlugAsync(string slug) =>
            BaseQuery().FirstOrDefaultAsync(r => r.Slug == slug);

        public async Task<List<Room>> GetSimilarAsync(Room room, int take = 4)
        {
            return await BaseQuery()
                .Where(r => r.RoomId != room.RoomId && (r.HotelId == room.HotelId || r.RoomTypeId == room.RoomTypeId))
                .OrderByDescending(r => r.IsFeatured)
                .Take(take)
                .ToListAsync();
        }
    }
}
