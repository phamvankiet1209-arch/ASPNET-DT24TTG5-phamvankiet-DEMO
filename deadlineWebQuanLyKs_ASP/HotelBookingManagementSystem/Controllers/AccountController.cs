using HotelBookingManagementSystem.Helpers;
using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Models.Enums;
using HotelBookingManagementSystem.Services;
using HotelBookingManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly IFileUploadService _upload;
        private readonly ILogService _log;

        public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IFileUploadService upload, ILogService log)
        {
            _users = users;
            _signIn = signIn;
            _upload = upload;
            _log = log;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _users.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
                return View(model);
            }

            var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                await _log.LogAsync("Login", "User", user.Id, "Đăng nhập thành công", user.Id);
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);
                if (await _users.IsInRoleAsync(user, RoleNames.Admin))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                if (await _users.IsInRoleAsync(user, RoleNames.Manager))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                if (await _users.IsInRoleAsync(user, RoleNames.HotelStaff))
                    return RedirectToAction("Index", "Dashboard", new { area = "Staff" });
                return RedirectToAction("Index", "Customer");
            }

            ModelState.AddModelError("", result.IsLockedOut ? "Tài khoản tạm thời bị khóa do đăng nhập sai nhiều lần." : "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.Phone,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.Now,
                Avatar = "/images/users/avatar-default.jpg"
            };
            var result = await _users.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _users.AddToRoleAsync(user, RoleNames.Customer);
                await _signIn.SignInAsync(user, false);
                await _log.LogAsync("Register", "User", user.Id, "Đăng ký tài khoản", user.Id);
                TempData["Success"] = "Đăng ký thành công. Chào mừng bạn đến với StayLux!";
                return RedirectToAction("Index", "Home");
            }
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _users.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Success"] = "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu đã được gửi (demo).";
                return RedirectToAction(nameof(ForgotPassword));
            }
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            return RedirectToAction(nameof(ResetPassword), new { email = user.Email, token });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _users.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản.");
                return View(model);
            }
            var result = await _users.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            return View(new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                IdentityNumber = user.IdentityNumber,
                Address = user.Address,
                Avatar = user.Avatar
            });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!ModelState.IsValid) { model.Avatar = user.Avatar; return View(model); }

            user.FullName = model.FullName;
            user.PhoneNumber = model.Phone;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.IdentityNumber = model.IdentityNumber;
            user.Address = model.Address;
            user.UpdatedAt = DateTime.Now;
            if (model.AvatarFile != null)
            {
                var path = await _upload.UploadAsync(model.AvatarFile, "avatars");
                if (path != null) user.Avatar = path;
            }
            await _users.UpdateAsync(user);
            TempData["Success"] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signIn.RefreshSignInAsync(user);
                TempData["Success"] = "Đổi mật khẩu thành công.";
                return RedirectToAction(nameof(Profile));
            }
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        public IActionResult AccessDenied() => RedirectToAction("Error", "Home", new { code = 403 });
    }
}
