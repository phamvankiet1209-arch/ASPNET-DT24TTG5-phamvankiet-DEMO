namespace HotelBookingManagementSystem.Models.Enums
{
    public enum Gender
    {
        Male = 1,
        Female = 2,
        Other = 3
    }

    public enum BookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        CheckedIn = 3,
        CheckedOut = 4,
        Cancelled = 5,
        NoShow = 6
    }

    public enum PaymentStatus
    {
        Unpaid = 1,
        Pending = 2,
        Paid = 3,
        Failed = 4,
        Refunded = 5
    }

    public enum PaymentMethod
    {
        PayAtHotel = 1,
        BankTransfer = 2,
        VNPay = 3,
        MoMo = 4,
        PayOS = 5
    }

    public enum RoomStatus
    {
        Available = 1,
        Occupied = 2,
        Cleaning = 3,
        Maintenance = 4,
        Inactive = 5
    }

    public enum DiscountType
    {
        Percentage = 1,
        FixedAmount = 2
    }

    public enum ContactStatus
    {
        New = 1,
        Processing = 2,
        Resolved = 3
    }

    public enum NotificationType
    {
        BookingSuccess = 1,
        BookingConfirmed = 2,
        BookingCancelled = 3,
        PaymentSuccess = 4,
        CheckInReminder = 5,
        NewVoucher = 6,
        System = 7
    }
}
