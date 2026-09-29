using HotelBookingManagementSystem.Data;
using HotelBookingManagementSystem.Helpers;
using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Services
{
    public class PriceBreakdown
    {
        public int Nights { get; set; }
        public decimal RoomPrice { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ExtraFee { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int? VoucherId { get; set; }
        public string? VoucherCode { get; set; }
        public string? VoucherMessage { get; set; }
    }

    public class CreateBookingRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public int NumberOfRooms { get; set; } = 1;
        public string GuestName { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? GuestIdentityNumber { get; set; }
        public string? SpecialRequest { get; set; }
        public string? Note { get; set; }
        public string? VoucherCode { get; set; }
        public List<int> ServiceIds { get; set; } = new();
        public decimal TaxRate { get; set; } = 0.08m;
    }

    public interface IBookingService
    {
        Task<bool> CheckRoomAvailabilityAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
        int CalculateNumberOfNights(DateTime checkIn, DateTime checkOut);
        decimal CalculateRoomPrice(Room room, int nights, int numberOfRooms);
        Task<PriceBreakdown> CalculateTotalAmountAsync(Room room, DateTime checkIn, DateTime checkOut, int numberOfRooms, List<int>? serviceIds, string? voucherCode);
        Task<(bool Ok, string Message, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request);
        Task<(bool Ok, string Message)> CancelBookingAsync(int bookingId, string userId, string reason, bool isStaff = false);
        Task<(bool Ok, string Message)> ConfirmBookingAsync(int bookingId, string staffId);
        Task<(bool Ok, string Message)> CheckInAsync(int bookingId, string staffId, string? note = null);
        Task<(bool Ok, string Message)> CheckOutAsync(int bookingId, string staffId, string? note = null);
        Task<(bool Ok, string Message)> MarkNoShowAsync(int bookingId, string staffId);
    }

    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IVoucherService _voucherService;
        private readonly INotificationService _notification;
        private readonly ILogService _log;

        private static readonly BookingStatus[] BlockingStatuses =
        {
            BookingStatus.Pending,
            BookingStatus.Confirmed,
            BookingStatus.CheckedIn
        };

        public BookingService(ApplicationDbContext db, IVoucherService voucherService, INotificationService notification, ILogService log)
        {
            _db = db;
            _voucherService = voucherService;
            _notification = notification;
            _log = log;
        }

        public async Task<bool> CheckRoomAvailabilityAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
        {
            checkIn = checkIn.Date;
            checkOut = checkOut.Date;
            if (checkOut <= checkIn) return false;

            var query = _db.Bookings.Where(b =>
                b.RoomId == roomId &&
                BlockingStatuses.Contains(b.BookingStatus) &&
                b.CheckInDate < checkOut &&
                b.CheckOutDate > checkIn);

            if (excludeBookingId.HasValue)
                query = query.Where(b => b.BookingId != excludeBookingId.Value);

            return !await query.AnyAsync();
        }

        public int CalculateNumberOfNights(DateTime checkIn, DateTime checkOut)
        {
            var nights = (checkOut.Date - checkIn.Date).Days;
            return nights < 1 ? 1 : nights;
        }

        public decimal CalculateRoomPrice(Room room, int nights, int numberOfRooms)
        {
            return room.DisplayPrice * nights * Math.Max(1, numberOfRooms);
        }

        public async Task<PriceBreakdown> CalculateTotalAmountAsync(Room room, DateTime checkIn, DateTime checkOut, int numberOfRooms, List<int>? serviceIds, string? voucherCode)
        {
            var nights = CalculateNumberOfNights(checkIn, checkOut);
            var subtotal = CalculateRoomPrice(room, nights, numberOfRooms);
            var extra = room.ExtraFee * nights;

            decimal serviceAmount = 0;
            if (serviceIds != null && serviceIds.Count > 0)
            {
                var services = await _db.HotelServices.Where(s => serviceIds.Contains(s.ServiceId) && s.Status).ToListAsync();
                serviceAmount = services.Sum(s => s.Price);
            }

            var taxable = subtotal + extra + serviceAmount;
            decimal discount = 0;
            int? voucherId = null;
            string? voucherMsg = null;
            string? code = null;

            if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                var apply = await _voucherService.ApplyAsync(voucherCode, taxable);
                voucherMsg = apply.Message;
                if (apply.Success)
                {
                    discount = apply.DiscountAmount;
                    voucherId = apply.Voucher!.VoucherId;
                    code = apply.Voucher.Code;
                }
            }

            var afterDiscount = Math.Max(0, taxable - discount);
            var tax = Math.Round(afterDiscount * 0.08m, 0);
            var total = afterDiscount + tax;

            return new PriceBreakdown
            {
                Nights = nights,
                RoomPrice = room.DisplayPrice,
                Subtotal = subtotal,
                ExtraFee = extra,
                ServiceAmount = serviceAmount,
                DiscountAmount = discount,
                TaxAmount = tax,
                TotalAmount = total,
                VoucherId = voucherId,
                VoucherCode = code,
                VoucherMessage = voucherMsg
            };
        }

        public async Task<(bool Ok, string Message, Booking? Booking)> CreateBookingAsync(CreateBookingRequest request)
        {
            if (request.CheckIn.Date < DateTime.Today)
                return (false, "Ngày nhận phòng không được ở quá khứ.", null);
            if (request.CheckOut.Date <= request.CheckIn.Date)
                return (false, "Ngày trả phòng phải sau ngày nhận phòng.", null);
            if (request.Adults < 1)
                return (false, "Số người lớn tối thiểu là 1.", null);

            var room = await _db.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.RoomId == request.RoomId);

            if (room == null) return (false, "Không tìm thấy phòng.", null);
            if (room.Status == RoomStatus.Inactive || room.Status == RoomStatus.Maintenance)
                return (false, "Phòng hiện không thể đặt.", null);
            if (request.Adults > room.AdultCapacity || request.Children > room.ChildCapacity)
                return (false, "Số khách vượt quá sức chứa của phòng.", null);

            var available = await CheckRoomAvailabilityAsync(request.RoomId, request.CheckIn, request.CheckOut);
            if (!available)
                return (false, "Phòng đã được đặt trong khoảng thời gian này. Vui lòng chọn ngày khác.", null);

            var breakdown = await CalculateTotalAmountAsync(room, request.CheckIn, request.CheckOut, request.NumberOfRooms, request.ServiceIds, request.VoucherCode);

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Double-check inside transaction to reduce concurrent booking risk
                var stillAvailable = await CheckRoomAvailabilityAsync(request.RoomId, request.CheckIn, request.CheckOut);
                if (!stillAvailable)
                {
                    await tx.RollbackAsync();
                    return (false, "Phòng vừa được đặt bởi khách khác. Vui lòng chọn ngày khác.", null);
                }

                var today = DateTime.Now.Date;
                var seq = await _db.Bookings.CountAsync(b => b.CreatedAt.Date == today) + 1;

                var booking = new Booking
                {
                    BookingCode = BookingCodeGenerator.Generate(seq),
                    UserId = request.UserId,
                    HotelId = room.HotelId,
                    RoomId = room.RoomId,
                    CheckInDate = request.CheckIn.Date,
                    CheckOutDate = request.CheckOut.Date,
                    Adults = request.Adults,
                    Children = request.Children,
                    NumberOfRooms = Math.Max(1, request.NumberOfRooms),
                    NumberOfNights = breakdown.Nights,
                    RoomPrice = breakdown.RoomPrice,
                    Subtotal = breakdown.Subtotal,
                    ExtraFee = breakdown.ExtraFee,
                    DiscountAmount = breakdown.DiscountAmount,
                    ServiceAmount = breakdown.ServiceAmount,
                    TaxAmount = breakdown.TaxAmount,
                    TotalAmount = breakdown.TotalAmount,
                    VoucherId = breakdown.VoucherId,
                    BookingStatus = BookingStatus.Pending,
                    PaymentStatus = PaymentStatus.Unpaid,
                    GuestName = request.GuestName,
                    GuestEmail = request.GuestEmail,
                    GuestPhone = request.GuestPhone,
                    GuestIdentityNumber = request.GuestIdentityNumber,
                    SpecialRequest = request.SpecialRequest,
                    Note = request.Note,
                    CreatedAt = DateTime.Now
                };

                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();

                if (request.ServiceIds.Count > 0)
                {
                    var services = await _db.HotelServices.Where(s => request.ServiceIds.Contains(s.ServiceId) && s.Status).ToListAsync();
                    foreach (var s in services)
                    {
                        _db.BookingServices.Add(new BookingServiceItem
                        {
                            BookingId = booking.BookingId,
                            ServiceId = s.ServiceId,
                            Quantity = 1,
                            UnitPrice = s.Price,
                            TotalPrice = s.Price
                        });
                    }
                    await _db.SaveChangesAsync();
                }

                if (breakdown.VoucherId.HasValue)
                    await _voucherService.IncrementUsedAsync(breakdown.VoucherId.Value);

                await _notification.NotifyAsync(request.UserId,
                    "Đặt phòng thành công",
                    $"Mã đặt phòng {booking.BookingCode} đã được tạo. Vui lòng thanh toán để hoàn tất.",
                    NotificationType.BookingSuccess);

                await _log.LogAsync("CreateBooking", "Booking", booking.BookingId.ToString(),
                    $"Tạo booking {booking.BookingCode}", request.UserId);

                await tx.CommitAsync();
                return (true, "Đặt phòng thành công.", booking);
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                return (false, "Có lỗi xảy ra khi tạo đặt phòng. Vui lòng thử lại.", null);
            }
        }

        public async Task<(bool Ok, string Message)> CancelBookingAsync(int bookingId, string userId, string reason, bool isStaff = false)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng.");
            if (!isStaff && booking.UserId != userId) return (false, "Bạn không có quyền hủy đặt phòng này.");
            if (booking.BookingStatus is BookingStatus.CheckedOut or BookingStatus.Cancelled)
                return (false, "Không thể hủy đặt phòng ở trạng thái hiện tại.");
            if (!isStaff && booking.BookingStatus == BookingStatus.CheckedIn)
                return (false, "Phòng đang lưu trú, vui lòng liên hệ lễ tân để hủy.");
            if (!isStaff && booking.CheckInDate.Date <= DateTime.Today)
                return (false, "Không thể hủy trong ngày nhận phòng. Vui lòng liên hệ khách sạn.");

            booking.BookingStatus = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.Now;
            booking.CancelReason = reason;
            booking.UpdatedAt = DateTime.Now;

            if (booking.PaymentStatus == PaymentStatus.Paid)
                booking.PaymentStatus = PaymentStatus.Refunded;

            if (booking.VoucherId.HasValue)
                await _voucherService.DecrementUsedAsync(booking.VoucherId.Value);

            await _db.SaveChangesAsync();
            await _notification.NotifyAsync(booking.UserId, "Đặt phòng đã bị hủy",
                $"Mã {booking.BookingCode} đã được hủy. Lý do: {reason}", NotificationType.BookingCancelled);
            await _log.LogAsync("CancelBooking", "Booking", booking.BookingId.ToString(), reason, userId);
            return (true, "Hủy đặt phòng thành công.");
        }

        public async Task<(bool Ok, string Message)> ConfirmBookingAsync(int bookingId, string staffId)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng.");
            if (booking.BookingStatus != BookingStatus.Pending)
                return (false, "Chỉ xác nhận được đặt phòng đang chờ.");

            booking.BookingStatus = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await _notification.NotifyAsync(booking.UserId, "Đặt phòng đã được xác nhận",
                $"Mã {booking.BookingCode} đã được xác nhận. Hẹn gặp bạn vào ngày nhận phòng.", NotificationType.BookingConfirmed);
            await _log.LogAsync("ConfirmBooking", "Booking", booking.BookingId.ToString(), "Xác nhận booking", staffId);
            return (true, "Xác nhận đặt phòng thành công.");
        }

        public async Task<(bool Ok, string Message)> CheckInAsync(int bookingId, string staffId, string? note = null)
        {
            var booking = await _db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng.");
            if (booking.BookingStatus != BookingStatus.Confirmed && booking.BookingStatus != BookingStatus.Pending)
                return (false, "Chỉ check-in được đặt phòng đã xác nhận hoặc đang chờ.");

            booking.BookingStatus = BookingStatus.CheckedIn;
            booking.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(note)) booking.Note = string.IsNullOrWhiteSpace(booking.Note) ? note : $"{booking.Note}\n{note}";
            booking.Room.Status = RoomStatus.Occupied;
            await _db.SaveChangesAsync();
            await _log.LogAsync("CheckIn", "Booking", booking.BookingId.ToString(), $"Check-in {booking.BookingCode}", staffId);
            return (true, "Check-in thành công.");
        }

        public async Task<(bool Ok, string Message)> CheckOutAsync(int bookingId, string staffId, string? note = null)
        {
            var booking = await _db.Bookings.Include(b => b.Room).Include(b => b.Payments).FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng.");
            if (booking.BookingStatus != BookingStatus.CheckedIn)
                return (false, "Chỉ check-out được khách đang lưu trú.");

            booking.BookingStatus = BookingStatus.CheckedOut;
            booking.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(note)) booking.Note = string.IsNullOrWhiteSpace(booking.Note) ? note : $"{booking.Note}\n{note}";
            booking.Room.Status = RoomStatus.Cleaning;
            await _db.SaveChangesAsync();
            await _log.LogAsync("CheckOut", "Booking", booking.BookingId.ToString(), $"Check-out {booking.BookingCode}", staffId);
            return (true, "Check-out thành công.");
        }

        public async Task<(bool Ok, string Message)> MarkNoShowAsync(int bookingId, string staffId)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return (false, "Không tìm thấy đặt phòng.");
            booking.BookingStatus = BookingStatus.NoShow;
            booking.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await _log.LogAsync("NoShow", "Booking", booking.BookingId.ToString(), "Đánh dấu không đến", staffId);
            return (true, "Đã đánh dấu khách không đến.");
        }
    }
}
