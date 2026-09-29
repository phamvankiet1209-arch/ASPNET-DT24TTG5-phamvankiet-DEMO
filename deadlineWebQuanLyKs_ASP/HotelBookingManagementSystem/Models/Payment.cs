using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelBookingManagementSystem.Models.Enums;

namespace HotelBookingManagementSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        [Required, StringLength(40)]
        public string TransactionCode { get; set; } = string.Empty;

        public PaymentMethod PaymentMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public DateTime? PaidAt { get; set; }

        [StringLength(400)]
        public string? PaymentNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
