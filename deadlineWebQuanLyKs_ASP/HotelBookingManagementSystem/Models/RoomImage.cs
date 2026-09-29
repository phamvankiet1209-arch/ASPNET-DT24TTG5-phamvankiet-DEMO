using System.ComponentModel.DataAnnotations;

namespace HotelBookingManagementSystem.Models
{
    public class RoomImage
    {
        [Key]
        public int RoomImageId { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        [Required, StringLength(400)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsThumbnail { get; set; }
        public int DisplayOrder { get; set; }
    }
}
