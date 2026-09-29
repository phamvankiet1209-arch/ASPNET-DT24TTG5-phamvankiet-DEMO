using System.ComponentModel.DataAnnotations;
using HotelBookingManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace HotelBookingManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        [StringLength(20)]
        public string? IdentityNumber { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(300)]
        public string? Avatar { get; set; }

        public int? HotelId { get; set; }
        public Hotel? Hotel { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public ICollection<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();
    }
}
