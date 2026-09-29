using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelBookingManagementSystem.Models.Enums;

namespace HotelBookingManagementSystem.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required, StringLength(30)]
        public string BookingCode { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } = null!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }

        public int Adults { get; set; } = 1;
        public int Children { get; set; }
        public int NumberOfRooms { get; set; } = 1;
        public int NumberOfNights { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RoomPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int? VoucherId { get; set; }
        public Voucher? Voucher { get; set; }

        public BookingStatus BookingStatus { get; set; } = BookingStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        [StringLength(150)]
        public string GuestName { get; set; } = string.Empty;

        [StringLength(150)]
        public string GuestEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string GuestPhone { get; set; } = string.Empty;

        [StringLength(20)]
        public string? GuestIdentityNumber { get; set; }

        [StringLength(500)]
        public string? SpecialRequest { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        [StringLength(400)]
        public string? CancelReason { get; set; }

        public ICollection<BookingServiceItem> BookingServices { get; set; } = new List<BookingServiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
