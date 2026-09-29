using System.ComponentModel.DataAnnotations;
using HotelBookingManagementSystem.Models.Enums;

namespace HotelBookingManagementSystem.Models
{
    public class Contact
    {
        [Key]
        public int ContactId { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required, StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public ContactStatus Status { get; set; } = ContactStatus.New;

        [StringLength(500)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
