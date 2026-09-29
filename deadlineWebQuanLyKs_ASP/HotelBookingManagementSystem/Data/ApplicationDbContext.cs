using HotelBookingManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hotel> Hotels => Set<Hotel>();
        public DbSet<RoomType> RoomTypes => Set<RoomType>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<RoomImage> RoomImages => Set<RoomImage>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<RoomAmenity> RoomAmenities => Set<RoomAmenity>();
        public DbSet<HotelService> HotelServices => Set<HotelService>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingServiceItem> BookingServices => Set<BookingServiceItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Contact> Contacts => Set<Contact>();
        public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Hotel>(e =>
            {
                e.HasIndex(x => x.Slug).IsUnique();
                e.HasIndex(x => x.City);
            });

            builder.Entity<Room>(e =>
            {
                e.HasIndex(x => x.Slug);
                e.HasIndex(x => new { x.HotelId, x.RoomNumber }).IsUnique();
                e.HasOne(x => x.Hotel).WithMany(h => h.Rooms).HasForeignKey(x => x.HotelId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.RoomType).WithMany(t => t.Rooms).HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<RoomImage>(e =>
            {
                e.HasOne(x => x.Room).WithMany(r => r.Images).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<RoomAmenity>(e =>
            {
                e.HasIndex(x => new { x.RoomId, x.AmenityId }).IsUnique();
                e.HasOne(x => x.Room).WithMany(r => r.RoomAmenities).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Amenity).WithMany(a => a.RoomAmenities).HasForeignKey(x => x.AmenityId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Voucher>(e =>
            {
                e.HasIndex(x => x.Code).IsUnique();
            });

            builder.Entity<Booking>(e =>
            {
                e.HasIndex(x => x.BookingCode).IsUnique();
                e.HasIndex(x => new { x.RoomId, x.CheckInDate, x.CheckOutDate });
                e.HasOne(x => x.User).WithMany(u => u.Bookings).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Hotel).WithMany(h => h.Bookings).HasForeignKey(x => x.HotelId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Room).WithMany(r => r.Bookings).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Voucher).WithMany(v => v.Bookings).HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<BookingServiceItem>(e =>
            {
                e.HasOne(x => x.Booking).WithMany(b => b.BookingServices).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Service).WithMany(s => s.BookingServices).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Payment>(e =>
            {
                e.HasIndex(x => x.TransactionCode).IsUnique();
                e.HasOne(x => x.Booking).WithMany(b => b.Payments).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Review>(e =>
            {
                e.HasIndex(x => x.BookingId).IsUnique();
                e.HasOne(x => x.User).WithMany(u => u.Reviews).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Room).WithMany(r => r.Reviews).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Booking).WithMany(b => b.Reviews).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Favorite>(e =>
            {
                e.HasIndex(x => new { x.UserId, x.RoomId }).IsUnique();
                e.HasOne(x => x.User).WithMany(u => u.Favorites).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Room).WithMany(r => r.Favorites).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Notification>(e =>
            {
                e.HasOne(x => x.User).WithMany(u => u.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Contact>(e =>
            {
                e.HasOne(x => x.User).WithMany(u => u.Contacts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<SystemLog>(e =>
            {
                e.HasOne(x => x.User).WithMany(u => u.SystemLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ApplicationUser>(e =>
            {
                e.HasOne(x => x.Hotel).WithMany(h => h.Staff).HasForeignKey(x => x.HotelId).OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
