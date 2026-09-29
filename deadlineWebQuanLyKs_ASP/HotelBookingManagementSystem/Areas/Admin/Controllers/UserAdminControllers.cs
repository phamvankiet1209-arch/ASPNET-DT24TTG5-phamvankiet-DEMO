using HotelBookingManagementSystem.Data;
using HotelBookingManagementSystem.Helpers;
using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Services;
using HotelBookingManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly ApplicationDbContext _db;
        private readonly ILogService _log;

        public UsersController(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, ApplicationDbContext db, ILogService log)
        {
            _users = users; _roles = roles; _db = db; _log = log;
        }

        public async Task<IActionResult> Index(string? q, string? role)
        {
            var list = _users.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                list = list.Where(u => u.FullName.Contains(q) || (u.Email != null && u.Email.Contains(q)));
            var users = await list.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var result = new List<(ApplicationUser User, IList<string> Roles)>();
            foreach (var u in users)
            {
                var roles = await _users.GetRolesAsync(u);
                if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role)) continue;
                result.Add((u, roles));
            }
            ViewBag.Q = q; ViewBag.Role = role;
            return View(result);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Hotels = await _db.Hotels.ToListAsync();
            return View(new UserAdminViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserAdminViewModel m)
        {
            ViewBag.Hotels = await _db.Hotels.ToListAsync();
            if (string.IsNullOrWhiteSpace(m.Password)) ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
            if (!ModelState.IsValid) return View(m);
            var user = new ApplicationUser
            {
                UserName = m.Email, Email = m.Email, FullName = m.FullName, PhoneNumber = m.Phone,
                EmailConfirmed = true, IsActive = m.IsActive, HotelId = m.HotelId, CreatedAt = DateTime.Now,
                Avatar = "/images/users/avatar-default.jpg"
            };
            var result = await _users.CreateAsync(user, m.Password!);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(m);
            }
            await _users.AddToRoleAsync(user, m.Role);
            await _log.LogAsync("Create", "User", user.Id, $"Tạo user {user.Email} role {m.Role}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            TempData["Success"] = "Thêm người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.Hotels = await _db.Hotels.ToListAsync();
            var roles = await _users.GetRolesAsync(user);
            return View("Create", new UserAdminViewModel
            {
                Id = user.Id, FullName = user.FullName, Email = user.Email ?? "", Phone = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? RoleNames.Customer, HotelId = user.HotelId, IsActive = user.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserAdminViewModel m)
        {
            var user = await _users.FindByIdAsync(m.Id!);
            if (user == null) return NotFound();
            user.FullName = m.FullName; user.PhoneNumber = m.Phone; user.HotelId = m.HotelId; user.IsActive = m.IsActive; user.UpdatedAt = DateTime.Now;
            await _users.UpdateAsync(user);
            var current = await _users.GetRolesAsync(user);
            await _users.RemoveFromRolesAsync(user, current);
            await _users.AddToRoleAsync(user, m.Role);
            if (!string.IsNullOrWhiteSpace(m.Password))
            {
                var token = await _users.GeneratePasswordResetTokenAsync(user);
                await _users.ResetPasswordAsync(user, token, m.Password);
            }
            TempData["Success"] = "Cập nhật người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (user.Email == "admin@hotel.com") { TempData["Error"] = "Không thể khóa tài khoản Admin gốc."; return RedirectToAction(nameof(Index)); }
            user.IsActive = !user.IsActive;
            if (!user.IsActive) await _users.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
            else { await _users.SetLockoutEndDateAsync(user, null); await _users.ResetAccessFailedCountAsync(user); }
            await _users.UpdateAsync(user);
            TempData["Success"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            await _users.ResetPasswordAsync(user, token, "User@123");
            TempData["Success"] = "Đã reset mật khẩu về User@123.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Bookings(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.User = user;
            var bookings = await _db.Bookings.Include(b => b.Room).ThenInclude(r => r.Hotel)
                .Where(b => b.UserId == id).OrderByDescending(b => b.CreatedAt).ToListAsync();
            return View(bookings);
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class StaffsController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _db;
        public StaffsController(UserManager<ApplicationUser> users, ApplicationDbContext db) { _users = users; _db = db; }

        public async Task<IActionResult> Index()
        {
            var staff = await _users.GetUsersInRoleAsync(RoleNames.HotelStaff);
            var managers = await _users.GetUsersInRoleAsync(RoleNames.Manager);
            ViewBag.Managers = managers;
            ViewBag.Hotels = await _db.Hotels.ToListAsync();
            return View(staff);
        }
    }
}
