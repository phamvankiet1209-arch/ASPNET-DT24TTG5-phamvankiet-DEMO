using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingManagementSystem.Models
{
    public class RoomType
    {
        [Key]
        public int RoomTypeId { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int MaxAdults { get; set; } = 2;
        public int MaxChildren { get; set; } = 1;

        [StringLength(80)]
        public string BedType { get; set; } = "King";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        public bool Status { get; set; } = true;

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
