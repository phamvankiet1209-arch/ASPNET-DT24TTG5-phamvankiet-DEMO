using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingManagementSystem.Models
{
    public class HotelService
    {
        [Key]
        public int ServiceId { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(50)]
        public string Unit { get; set; } = "lần";

        public bool Status { get; set; } = true;

        public ICollection<BookingServiceItem> BookingServices { get; set; } = new List<BookingServiceItem>();
    }
}
