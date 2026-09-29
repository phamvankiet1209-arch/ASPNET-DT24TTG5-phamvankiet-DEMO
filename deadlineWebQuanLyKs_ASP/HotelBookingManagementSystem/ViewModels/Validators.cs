using FluentValidation;
using HotelBookingManagementSystem.ViewModels;

namespace HotelBookingManagementSystem.ViewModels
{
    public class BookingFormValidator : AbstractValidator<BookingFormViewModel>
    {
        public BookingFormValidator()
        {
            RuleFor(x => x.GuestName).NotEmpty().WithMessage("Vui lòng nhập họ tên người đặt.");
            RuleFor(x => x.GuestEmail).NotEmpty().EmailAddress().WithMessage("Email không hợp lệ.");
            RuleFor(x => x.GuestPhone).NotEmpty().WithMessage("Vui lòng nhập số điện thoại.");
            RuleFor(x => x.CheckInDate).GreaterThanOrEqualTo(DateTime.Today).WithMessage("Ngày nhận phòng không được ở quá khứ.");
            RuleFor(x => x.CheckOutDate).GreaterThan(x => x.CheckInDate).WithMessage("Ngày trả phòng phải sau ngày nhận phòng.");
            RuleFor(x => x.Adults).GreaterThanOrEqualTo(1).WithMessage("Số người lớn tối thiểu là 1.");
            RuleFor(x => x.NumberOfRooms).GreaterThanOrEqualTo(1);
        }
    }

    public class RegisterValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Phone).NotEmpty().Matches(@"^(0|\+84)[0-9]{9,10}$").WithMessage("Số điện thoại không hợp lệ.");
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Mật khẩu xác nhận không khớp.");
        }
    }
}
