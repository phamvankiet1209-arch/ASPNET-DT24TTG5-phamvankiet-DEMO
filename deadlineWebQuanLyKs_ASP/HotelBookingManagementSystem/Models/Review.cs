using System.ComponentModel.DataAnnotations;

namespace HotelBookingManagementSystem.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [Range(1, 5)]
        public int CleanlinessRating { get; set; }

        [Range(1, 5)]
        public int LocationRating { get; set; }

        [Range(1, 5)]
        public int ServiceRating { get; set; }

        [Range(1, 5)]
        public int StaffRating { get; set; }

        [Range(1, 5)]
        public int ValueRating { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Comment { get; set; } = string.Empty;

        [StringLength(400)]
        public string? ImageUrl { get; set; }

        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
