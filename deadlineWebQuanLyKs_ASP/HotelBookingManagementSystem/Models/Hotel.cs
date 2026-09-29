using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingManagementSystem.Models
{
    public class Hotel
    {
        [Key]
        public int HotelId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(250)]
        public string? Website { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Range(1, 5)]
        public int StarRating { get; set; } = 4;

        [StringLength(10)]
        public string CheckInTime { get; set; } = "14:00";

        [StringLength(10)]
        public string CheckOutTime { get; set; } = "12:00";

        [StringLength(300)]
        public string? Thumbnail { get; set; }

        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<ApplicationUser> Staff { get; set; } = new List<ApplicationUser>();
    }
}
