using System.ComponentModel.DataAnnotations;

namespace HotelBookingManagementSystem.Models
{
    public class RoomAmenity
    {
        [Key]
        public int RoomAmenityId { get; set; }

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public int AmenityId { get; set; }
        public Amenity Amenity { get; set; } = null!;
    }
}
