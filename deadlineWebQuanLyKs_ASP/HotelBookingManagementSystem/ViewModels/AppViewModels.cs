using System.ComponentModel.DataAnnotations;
using HotelBookingManagementSystem.Models.Enums;

namespace HotelBookingManagementSystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;

        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        [StringLength(20)]
        public string? IdentityNumber { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        public string? Avatar { get; set; }
        public IFormFile? AvatarFile { get; set; }
    }

    public class ContactViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chủ đề")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Message { get; set; } = string.Empty;
    }

    public class BookingFormViewModel
    {
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string GuestName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string GuestEmail { get; set; } = string.Empty;

        [Required]
        public string GuestPhone { get; set; } = string.Empty;

        public string? GuestIdentityNumber { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 10, ErrorMessage = "Số người lớn tối thiểu là 1")]
        public int Adults { get; set; } = 1;

        [Range(0, 10)]
        public int Children { get; set; }

        [Range(1, 5)]
        public int NumberOfRooms { get; set; } = 1;

        public string? SpecialRequest { get; set; }
        public string? Note { get; set; }
        public string? VoucherCode { get; set; }
        public List<int> ServiceIds { get; set; } = new();
        public string? ReturnUrl { get; set; }
    }

    public class ReviewFormViewModel
    {
        public int BookingId { get; set; }
        public int RoomId { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Range(1, 5)] public int CleanlinessRating { get; set; } = 5;
        [Range(1, 5)] public int LocationRating { get; set; } = 5;
        [Range(1, 5)] public int ServiceRating { get; set; } = 5;
        [Range(1, 5)] public int StaffRating { get; set; } = 5;
        [Range(1, 5)] public int ValueRating { get; set; } = 5;

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Comment { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }

    public class PaymentCheckoutViewModel
    {
        public int BookingId { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.PayAtHotel;
        public string? Note { get; set; }
    }

    public class UserAdminViewModel
    {
        public string? Id { get; set; }

        [Required] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Password { get; set; }
        [Required] public string Role { get; set; } = "Customer";
        public int? HotelId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class HotelFormViewModel
    {
        public int HotelId { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        [Required] public string Address { get; set; } = string.Empty;
        [Required] public string City { get; set; } = string.Empty;
        public string? District { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [Range(1, 5)] public int StarRating { get; set; } = 4;
        public string CheckInTime { get; set; } = "14:00";
        public string CheckOutTime { get; set; } = "12:00";
        public string? Thumbnail { get; set; }
        public IFormFile? ThumbnailFile { get; set; }
        public bool Status { get; set; } = true;
    }

    public class RoomFormViewModel
    {
        public int RoomId { get; set; }
        [Required] public int HotelId { get; set; }
        [Required] public int RoomTypeId { get; set; }
        [Required] public string RoomNumber { get; set; } = string.Empty;
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        public int Floor { get; set; }
        [Range(0.1, 1000)] public decimal Area { get; set; }
        [Range(1, 100000000)] public decimal PricePerNight { get; set; }
        public decimal? DiscountPrice { get; set; }
        [Range(1, 20)] public int AdultCapacity { get; set; } = 2;
        [Range(0, 20)] public int ChildCapacity { get; set; } = 1;
        public string BedType { get; set; } = "King";
        [Range(1, 10)] public int NumberOfBeds { get; set; } = 1;
        public string? ViewType { get; set; }
        public bool SmokingAllowed { get; set; }
        public bool BreakfastIncluded { get; set; }
        public bool HasBalcony { get; set; }
        public bool HasNiceView { get; set; }
        public bool HasAirConditioner { get; set; } = true;
        public bool HasWifi { get; set; } = true;
        public bool HasBathtub { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Available;
        public string? CancellationPolicy { get; set; }
        public decimal ExtraFee { get; set; }
        public bool IsFeatured { get; set; }
        public List<int> AmenityIds { get; set; } = new();
        public IFormFile? ThumbnailFile { get; set; }
        public List<IFormFile>? GalleryFiles { get; set; }
        public string? Thumbnail { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    }
}
