using System.ComponentModel.DataAnnotations;

namespace HotelBookingManagementSystem.Models
{
    public class SystemLog
    {
        [Key]
        public int LogId { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required, StringLength(80)]
        public string Action { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Entity { get; set; }

        [StringLength(50)]
        public string? EntityId { get; set; }

        public string? Description { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
