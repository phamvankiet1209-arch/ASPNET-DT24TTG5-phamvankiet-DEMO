using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HotelBookingManagementSystem.Helpers
{
    public static class SlugHelper
    {
        public static string Generate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            var slug = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            slug = slug.Replace("đ", "d").Replace("Đ", "d");
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            slug = Regex.Replace(slug, @"-+", "-");
            return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
        }
    }

    public static class CurrencyHelper
    {
        public static string ToVnd(decimal amount)
        {
            return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", amount);
        }
    }

    public static class DateHelper
    {
        public static string ToVnDate(DateTime date) => date.ToString("dd/MM/yyyy");
        public static string ToVnDateTime(DateTime date) => date.ToString("dd/MM/yyyy HH:mm");
    }

    public static class BookingCodeGenerator
    {
        public static string Generate(int sequence)
        {
            return $"BK{DateTime.Now:yyyyMMdd}{sequence:D4}";
        }

        public static string GenerateFromDate(DateTime date, int sequence)
        {
            return $"BK{date:yyyyMMdd}{sequence:D4}";
        }
    }

    public static class EnumDisplayHelper
    {
        public static string BookingStatus(Models.Enums.BookingStatus status) => status switch
        {
            Models.Enums.BookingStatus.Pending => "Chờ xác nhận",
            Models.Enums.BookingStatus.Confirmed => "Đã xác nhận",
            Models.Enums.BookingStatus.CheckedIn => "Đang lưu trú",
            Models.Enums.BookingStatus.CheckedOut => "Đã trả phòng",
            Models.Enums.BookingStatus.Cancelled => "Đã hủy",
            Models.Enums.BookingStatus.NoShow => "Không đến",
            _ => status.ToString()
        };

        public static string PaymentStatus(Models.Enums.PaymentStatus status) => status switch
        {
            Models.Enums.PaymentStatus.Unpaid => "Chưa thanh toán",
            Models.Enums.PaymentStatus.Pending => "Đang xử lý",
            Models.Enums.PaymentStatus.Paid => "Đã thanh toán",
            Models.Enums.PaymentStatus.Failed => "Thất bại",
            Models.Enums.PaymentStatus.Refunded => "Đã hoàn tiền",
            _ => status.ToString()
        };

        public static string PaymentMethod(Models.Enums.PaymentMethod method) => method switch
        {
            Models.Enums.PaymentMethod.PayAtHotel => "Thanh toán tại khách sạn",
            Models.Enums.PaymentMethod.BankTransfer => "Chuyển khoản",
            Models.Enums.PaymentMethod.VNPay => "VNPay",
            Models.Enums.PaymentMethod.MoMo => "MoMo",
            Models.Enums.PaymentMethod.PayOS => "PayOS",
            _ => method.ToString()
        };

        public static string RoomStatus(Models.Enums.RoomStatus status) => status switch
        {
            Models.Enums.RoomStatus.Available => "Còn trống",
            Models.Enums.RoomStatus.Occupied => "Đang sử dụng",
            Models.Enums.RoomStatus.Cleaning => "Đang dọn",
            Models.Enums.RoomStatus.Maintenance => "Bảo trì",
            Models.Enums.RoomStatus.Inactive => "Ngừng hoạt động",
            _ => status.ToString()
        };

        public static string Gender(Models.Enums.Gender gender) => gender switch
        {
            Models.Enums.Gender.Male => "Nam",
            Models.Enums.Gender.Female => "Nữ",
            Models.Enums.Gender.Other => "Khác",
            _ => ""
        };

        public static string ContactStatus(Models.Enums.ContactStatus status) => status switch
        {
            Models.Enums.ContactStatus.New => "Mới",
            Models.Enums.ContactStatus.Processing => "Đang xử lý",
            Models.Enums.ContactStatus.Resolved => "Đã giải quyết",
            _ => status.ToString()
        };

        public static string BookingStatusClass(Models.Enums.BookingStatus status) => status switch
        {
            Models.Enums.BookingStatus.Pending => "warning",
            Models.Enums.BookingStatus.Confirmed => "info",
            Models.Enums.BookingStatus.CheckedIn => "primary",
            Models.Enums.BookingStatus.CheckedOut => "success",
            Models.Enums.BookingStatus.Cancelled => "danger",
            Models.Enums.BookingStatus.NoShow => "secondary",
            _ => "secondary"
        };

        public static string PaymentStatusClass(Models.Enums.PaymentStatus status) => status switch
        {
            Models.Enums.PaymentStatus.Unpaid => "secondary",
            Models.Enums.PaymentStatus.Pending => "warning",
            Models.Enums.PaymentStatus.Paid => "success",
            Models.Enums.PaymentStatus.Failed => "danger",
            Models.Enums.PaymentStatus.Refunded => "info",
            _ => "secondary"
        };
    }

    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string HotelStaff = "HotelStaff";
        public const string Customer = "Customer";
    }
}
