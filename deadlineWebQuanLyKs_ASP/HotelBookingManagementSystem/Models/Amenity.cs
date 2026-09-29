using System.ComponentModel.DataAnnotations;

namespace HotelBookingManagementSystem.Models
{
    public class Amenity
    {
        [Key]
        public int AmenityId { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Icon { get; set; }

        [StringLength(300)]
        public string? Description { get; set; }

        public bool Status { get; set; } = true;

        public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
    }
}
