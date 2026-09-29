using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelBookingManagementSystem.Models.Enums;

namespace HotelBookingManagementSystem.Models
{
    public class Voucher
    {
        [Key]
        public int VoucherId { get; set; }

        [Required, StringLength(40)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(400)]
        public string? Description { get; set; }

        public DiscountType DiscountType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumOrder { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int Quantity { get; set; }
        public int UsedCount { get; set; }
        public bool Status { get; set; } = true;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        [NotMapped]
        public bool IsValid => Status
            && DateTime.Now >= StartDate
            && DateTime.Now <= EndDate
            && UsedCount < Quantity;
    }
}
