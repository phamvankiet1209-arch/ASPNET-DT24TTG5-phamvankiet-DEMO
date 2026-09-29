using HotelBookingManagementSystem.Data;
using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Services
{
    public class DashboardStats
    {
        public int TotalCustomers { get; set; }
        public int TotalHotels { get; set; }
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int TotalBookings { get; set; }
        public int TodayBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthRevenueData { get; set; } = new();
        public List<int> MonthBookingData { get; set; } = new();
        public List<string> StatusLabels { get; set; } = new();
        public List<int> StatusData { get; set; } = new();
        public List<(string Name, int Count)> TopRooms { get; set; } = new();
        public List<(string Name, int Count)> TopCustomers { get; set; } = new();
    }

    public interface IDashboardService
    {
        Task<DashboardStats> GetAdminStatsAsync();
        Task<DashboardStats> GetManagerStatsAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        public DashboardService(ApplicationDbContext db) => _db = db;

        public async Task<DashboardStats> GetAdminStatsAsync()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var paidStatuses = new[] { PaymentStatus.Paid };
            var revenueQuery = _db.Bookings.Where(b => b.PaymentStatus == PaymentStatus.Paid && b.BookingStatus != BookingStatus.Cancelled);

            var stats = new DashboardStats
            {
                TotalCustomers = await _db.Users.CountAsync(),
                TotalHotels = await _db.Hotels.CountAsync(),
                TotalRooms = await _db.Rooms.CountAsync(),
                AvailableRooms = await _db.Rooms.CountAsync(r => r.Status == RoomStatus.Available),
                OccupiedRooms = await _db.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied),
                TotalBookings = await _db.Bookings.CountAsync(),
                TodayBookings = await _db.Bookings.CountAsync(b => b.CreatedAt.Date == today),
                PendingBookings = await _db.Bookings.CountAsync(b => b.BookingStatus == BookingStatus.Pending),
                ConfirmedBookings = await _db.Bookings.CountAsync(b => b.BookingStatus == BookingStatus.Confirmed),
                CancelledBookings = await _db.Bookings.CountAsync(b => b.BookingStatus == BookingStatus.Cancelled),
                TodayRevenue = await revenueQuery.Where(b => b.CreatedAt.Date == today).SumAsync(b => (decimal?)b.TotalAmount) ?? 0,
                MonthRevenue = await revenueQuery.Where(b => b.CreatedAt >= monthStart).SumAsync(b => (decimal?)b.TotalAmount) ?? 0,
                TotalRevenue = await revenueQuery.SumAsync(b => (decimal?)b.TotalAmount) ?? 0
            };

            for (var i = 11; i >= 0; i--)
            {
                var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var next = d.AddMonths(1);
                stats.MonthLabels.Add($"T{d.Month}/{d.Year}");
                stats.MonthRevenueData.Add(await revenueQuery.Where(b => b.CreatedAt >= d && b.CreatedAt < next).SumAsync(b => (decimal?)b.TotalAmount) ?? 0);
                stats.MonthBookingData.Add(await _db.Bookings.CountAsync(b => b.CreatedAt >= d && b.CreatedAt < next));
            }

            foreach (BookingStatus s in Enum.GetValues(typeof(BookingStatus)))
            {
                stats.StatusLabels.Add(Helpers.EnumDisplayHelper.BookingStatus(s));
                stats.StatusData.Add(await _db.Bookings.CountAsync(b => b.BookingStatus == s));
            }

            stats.TopRooms = (await _db.Bookings.Include(b => b.Room)
                    .Where(b => b.BookingStatus != BookingStatus.Cancelled)
                    .GroupBy(b => b.Room.Name)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).Take(5).ToListAsync())
                .Select(x => (x.Name, x.Count)).ToList();

            stats.TopCustomers = (await _db.Bookings.Include(b => b.User)
                    .Where(b => b.BookingStatus != BookingStatus.Cancelled)
                    .GroupBy(b => b.User.FullName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).Take(5).ToListAsync())
                .Select(x => (x.Name, x.Count)).ToList();

            return stats;
        }

        public Task<DashboardStats> GetManagerStatsAsync() => GetAdminStatsAsync();
    }

    public class ReportFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class ReportResult
    {
        public decimal Revenue { get; set; }
        public int TotalBookings { get; set; }
        public int SuccessBookings { get; set; }
        public int CancelledBookings { get; set; }
        public List<string> Labels { get; set; } = new();
        public List<decimal> RevenueSeries { get; set; } = new();
        public List<(string Name, int Count, decimal Revenue)> TopRooms { get; set; } = new();
        public List<(string Name, int Count)> LowRooms { get; set; } = new();
        public List<(string Name, int Count, decimal Spent)> TopCustomers { get; set; } = new();
        public int NewCustomers { get; set; }
    }

    public interface IReportService
    {
        Task<ReportResult> GetAsync(ReportFilter filter);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;
        public ReportService(ApplicationDbContext db) => _db = db;

        public async Task<ReportResult> GetAsync(ReportFilter filter)
        {
            var from = filter.From ?? DateTime.Today.AddMonths(-11);
            var to = (filter.To ?? DateTime.Today).Date.AddDays(1);

            var bookings = _db.Bookings.Include(b => b.Room).Include(b => b.User)
                .Where(b => b.CreatedAt >= from && b.CreatedAt < to);

            var paid = bookings.Where(b => b.PaymentStatus == PaymentStatus.Paid && b.BookingStatus != BookingStatus.Cancelled);

            var result = new ReportResult
            {
                Revenue = await paid.SumAsync(b => (decimal?)b.TotalAmount) ?? 0,
                TotalBookings = await bookings.CountAsync(),
                SuccessBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.CheckedOut || b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.CheckedIn),
                CancelledBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.Cancelled),
                NewCustomers = await _db.Users.CountAsync(u => u.CreatedAt >= from && u.CreatedAt < to)
            };

            var cursor = new DateTime(from.Year, from.Month, 1);
            var end = to;
            while (cursor < end)
            {
                var next = cursor.AddMonths(1);
                result.Labels.Add($"T{cursor.Month}/{cursor.Year}");
                result.RevenueSeries.Add(await paid.Where(b => b.CreatedAt >= cursor && b.CreatedAt < next).SumAsync(b => (decimal?)b.TotalAmount) ?? 0);
                cursor = next;
            }

            result.TopRooms = (await paid.GroupBy(b => b.Room.Name)
                .Select(g => new { Name = g.Key, Count = g.Count(), Revenue = g.Sum(x => x.TotalAmount) })
                .OrderByDescending(x => x.Count).Take(8).ToListAsync())
                .Select(x => (x.Name, x.Count, x.Revenue)).ToList();

            var allRoomBookings = await _db.Rooms.Select(r => new
            {
                r.Name,
                Count = r.Bookings.Count(b => b.CreatedAt >= from && b.CreatedAt < to && b.BookingStatus != BookingStatus.Cancelled)
            }).OrderBy(x => x.Count).Take(8).ToListAsync();
            result.LowRooms = allRoomBookings.Select(x => (x.Name, x.Count)).ToList();

            result.TopCustomers = (await paid.GroupBy(b => b.User.FullName)
                .Select(g => new { Name = g.Key, Count = g.Count(), Spent = g.Sum(x => x.TotalAmount) })
                .OrderByDescending(x => x.Spent).Take(8).ToListAsync())
                .Select(x => (x.Name, x.Count, x.Spent)).ToList();

            return result;
        }
    }

    public interface IFavoriteService
    {
        Task<(bool Ok, string Message)> ToggleAsync(string userId, int roomId);
        Task<bool> IsFavoriteAsync(string userId, int roomId);
        Task<List<Favorite>> GetByUserAsync(string userId);
    }

    public class FavoriteService : IFavoriteService
    {
        private readonly ApplicationDbContext _db;
        public FavoriteService(ApplicationDbContext db) => _db = db;

        public async Task<(bool Ok, string Message)> ToggleAsync(string userId, int roomId)
        {
            var existing = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.RoomId == roomId);
            if (existing != null)
            {
                _db.Favorites.Remove(existing);
                await _db.SaveChangesAsync();
                return (true, "Đã xóa khỏi danh sách yêu thích.");
            }

            _db.Favorites.Add(new Favorite { UserId = userId, RoomId = roomId, CreatedAt = DateTime.Now });
            await _db.SaveChangesAsync();
            return (true, "Đã thêm vào danh sách yêu thích.");
        }

        public Task<bool> IsFavoriteAsync(string userId, int roomId) =>
            _db.Favorites.AnyAsync(f => f.UserId == userId && f.RoomId == roomId);

        public Task<List<Favorite>> GetByUserAsync(string userId) =>
            _db.Favorites.Include(f => f.Room).ThenInclude(r => r.Hotel)
                .Include(f => f.Room).ThenInclude(r => r.Reviews)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
    }
}
