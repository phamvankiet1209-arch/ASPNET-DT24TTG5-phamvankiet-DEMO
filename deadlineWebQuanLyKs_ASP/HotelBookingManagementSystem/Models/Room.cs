using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelBookingManagementSystem.Models.Enums;

namespace HotelBookingManagementSystem.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } = null!;

        [Required]
        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; } = null!;

        [Required, StringLength(20)]
        public string RoomNumber { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public int Floor { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        public int AdultCapacity { get; set; } = 2;
        public int ChildCapacity { get; set; } = 1;

        [StringLength(80)]
        public string BedType { get; set; } = "King";

        public int NumberOfBeds { get; set; } = 1;

        [StringLength(80)]
        public string? ViewType { get; set; }

        public bool SmokingAllowed { get; set; }
        public bool BreakfastIncluded { get; set; }
        public bool HasBalcony { get; set; }
        public bool HasNiceView { get; set; }
        public bool HasAirConditioner { get; set; } = true;
        public bool HasWifi { get; set; } = true;
        public bool HasBathtub { get; set; }

        [StringLength(300)]
        public string? Thumbnail { get; set; }

        public RoomStatus Status { get; set; } = RoomStatus.Available;

        [StringLength(500)]
        public string? CancellationPolicy { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraFee { get; set; }

        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
        public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

        [NotMapped]
        public decimal DisplayPrice => DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < PricePerNight
            ? DiscountPrice.Value
            : PricePerNight;

        [NotMapped]
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < PricePerNight;

        [NotMapped]
        public double AverageRating => Reviews != null && Reviews.Any(r => r.Status)
            ? Reviews.Where(r => r.Status).Average(r => r.Rating)
            : 0;

        [NotMapped]
        public int ReviewCount => Reviews?.Count(r => r.Status) ?? 0;
    }
}
