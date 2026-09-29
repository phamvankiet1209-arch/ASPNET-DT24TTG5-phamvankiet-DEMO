using System.ComponentModel.DataAnnotations;

namespace HotelBookingManagementSystem.Models
{
    public class Favorite
    {
        [Key]
        public int FavoriteId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
