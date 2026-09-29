using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using HotelBookingManagementSystem.Data;
using HotelBookingManagementSystem.Helpers;

namespace HotelBookingManagementSystem.Services
{
    public interface ILogService
    {
        Task LogAsync(string action, string? entity, string? entityId, string? description, string? userId = null);
    }

    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;

        public LogService(ApplicationDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task LogAsync(string action, string? entity, string? entityId, string? description, string? userId = null)
        {
            var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
            _db.SystemLogs.Add(new SystemLog
            {
                UserId = userId,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Description = description,
                IpAddress = ip,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
    }

    public interface INotificationService
    {
        Task NotifyAsync(string userId, string title, string message, NotificationType type);
        Task<List<Notification>> GetRecentAsync(string userId, int take = 8);
        Task<int> CountUnreadAsync(string userId);
        Task MarkReadAsync(int id, string userId);
        Task MarkAllReadAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        public NotificationService(ApplicationDbContext db) => _db = db;

        public async Task NotifyAsync(string userId, string title, string message, NotificationType type)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        public Task<List<Notification>> GetRecentAsync(string userId, int take = 8) =>
            _db.Notifications.Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync();

        public Task<int> CountUnreadAsync(string userId) =>
            _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task MarkReadAsync(int id, string userId)
        {
            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.NotificationId == id && x.UserId == userId);
            if (n == null) return;
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }

        public async Task MarkAllReadAsync(string userId)
        {
            var list = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var n in list) n.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public interface IFileUploadService
    {
        Task<string?> UploadAsync(IFormFile file, string folder);
        bool Delete(string relativePath);
    }

    public class FileUploadService : IFileUploadService
    {
        private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
        private const long MaxSize = 5 * 1024 * 1024;
        private readonly IWebHostEnvironment _env;

        public FileUploadService(IWebHostEnvironment env) => _env = env;

        public async Task<string?> UploadAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Allowed.Contains(ext) || file.Length > MaxSize) return null;

            var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(dir, name);
            await using var stream = new FileStream(full, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{name}";
        }

        public bool Delete(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return false;
            var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full)) return false;
            File.Delete(full);
            return true;
        }
    }

    public class VoucherApplyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public Voucher? Voucher { get; set; }
    }

    public interface IVoucherService
    {
        Task<VoucherApplyResult> ApplyAsync(string code, decimal orderAmount);
        Task IncrementUsedAsync(int voucherId);
        Task DecrementUsedAsync(int voucherId);
    }

    public class VoucherService : IVoucherService
    {
        private readonly ApplicationDbContext _db;
        public VoucherService(ApplicationDbContext db) => _db = db;

        public async Task<VoucherApplyResult> ApplyAsync(string code, decimal orderAmount)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new VoucherApplyResult { Success = false, Message = "Vui lòng nhập mã giảm giá." };

            var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == code.Trim().ToUpper());
            if (voucher == null)
                return new VoucherApplyResult { Success = false, Message = "Mã giảm giá không tồn tại." };
            if (!voucher.Status)
                return new VoucherApplyResult { Success = false, Message = "Mã giảm giá đã ngừng hoạt động." };
            if (DateTime.Now < voucher.StartDate || DateTime.Now > voucher.EndDate)
                return new VoucherApplyResult { Success = false, Message = "Mã giảm giá đã hết hạn hoặc chưa đến thời gian sử dụng." };
            if (voucher.UsedCount >= voucher.Quantity)
                return new VoucherApplyResult { Success = false, Message = "Mã giảm giá đã hết lượt sử dụng." };
            if (orderAmount < voucher.MinimumOrder)
                return new VoucherApplyResult { Success = false, Message = $"Đơn hàng tối thiểu {CurrencyHelper.ToVnd(voucher.MinimumOrder)} để áp dụng mã này." };

            decimal discount;
            if (voucher.DiscountType == DiscountType.Percentage)
            {
                discount = orderAmount * voucher.DiscountValue / 100;
                if (voucher.MaximumDiscount.HasValue && discount > voucher.MaximumDiscount.Value)
                    discount = voucher.MaximumDiscount.Value;
            }
            else
            {
                discount = voucher.DiscountValue;
            }

            if (discount > orderAmount) discount = orderAmount;

            return new VoucherApplyResult
            {
                Success = true,
                Message = "Áp dụng mã giảm giá thành công.",
                DiscountAmount = Math.Round(discount, 0),
                Voucher = voucher
            };
        }

        public async Task IncrementUsedAsync(int voucherId)
        {
            var v = await _db.Vouchers.FindAsync(voucherId);
            if (v == null) return;
            v.UsedCount++;
            await _db.SaveChangesAsync();
        }

        public async Task DecrementUsedAsync(int voucherId)
        {
            var v = await _db.Vouchers.FindAsync(voucherId);
            if (v == null || v.UsedCount <= 0) return;
            v.UsedCount--;
            await _db.SaveChangesAsync();
        }
    }
}
