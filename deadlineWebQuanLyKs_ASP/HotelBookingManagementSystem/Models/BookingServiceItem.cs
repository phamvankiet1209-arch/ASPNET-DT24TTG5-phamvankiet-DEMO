using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingManagementSystem.Models
{
    public class BookingServiceItem
    {
        [Key]
        public int BookingServiceId { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public int ServiceId { get; set; }
        public HotelService Service { get; set; } = null!;

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
    }
}
