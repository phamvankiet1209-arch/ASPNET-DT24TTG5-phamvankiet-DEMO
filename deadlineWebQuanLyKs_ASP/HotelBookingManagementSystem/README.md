# HotelBookingManagementSystem

Website đặt phòng khách sạn hoàn chỉnh viết bằng **ASP.NET Core 8 MVC**, Entity Framework Core, Identity và SQL Server.

## Công nghệ

- ASP.NET Core 8 MVC, C#, EF Core, Identity
- SQL Server / SQL Server Express / LocalDB
- Razor + Bootstrap 5 + jQuery + Chart.js + SweetAlert2 + Font Awesome
- Repository/Service, Role-based Authorization, FluentValidation

## Yêu cầu

- .NET 8 SDK (hoặc .NET 8+ SDK có targeting pack net8.0)
- SQL Server, SQL Server Express hoặc LocalDB
- Visual Studio 2022 hoặc Visual Studio Code + C# extension

## Cấu hình Connection String

Mở `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=HotelBookingDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
}
```

SQL Server Express:

```text
Server=.\\SQLEXPRESS;Database=HotelBookingDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

LocalDB:

```text
Server=(localdb)\\mssqllocaldb;Database=HotelBookingDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

## Chạy dự án

```bash
cd HotelBookingManagementSystem
dotnet restore
dotnet ef database update
dotnet run
```

Nếu chưa cài công cụ EF:

```bash
dotnet tool install --global dotnet-ef
```

Ứng dụng tự chạy migration (nếu database chưa có) và seed dữ liệu demo khi khởi động.

Mở trình duyệt: `https://localhost:7xxx` (xem cổng trong `Properties/launchSettings.json`) hoặc URL `dotnet run` in ra.

## Tài khoản demo

| Role | Email | Mật khẩu |
|------|-------|----------|
| Admin | admin@hotel.com | Admin@123 |
| Manager | manager@hotel.com | Manager@123 |
| HotelStaff | staff@hotel.com | Staff@123 |
| Customer | customer@hotel.com | Customer@123 |

Các khách hàng khác: `lan.pham@gmail.com` ... `yen.ngo@gmail.com` / `Customer@123`.

## Chức năng chính

- Tìm kiếm / lọc / sắp xếp phòng, kiểm tra phòng trống theo khoảng ngày
- Đặt phòng, voucher, thuế 8%, dịch vụ thêm
- Thanh toán demo: tại khách sạn, chuyển khoản, VNPay, MoMo, PayOS
- Booking của tôi, hủy, in, hóa đơn, đánh giá sau check-out
- Yêu thích, thông báo, hồ sơ, liên hệ
- Staff: xác nhận, check-in, check-out
- Admin/Manager: CRUD khách sạn, phòng, voucher, báo cáo Chart.js, log hệ thống

## Dữ liệu seed

5 khách sạn, 7 loại phòng, ~45 phòng, 20+ tiện nghi, 10 dịch vụ, 8 voucher, 40+ booking, 30+ review, thanh toán, liên hệ, thông báo.

## Ghi chú bảo vệ đồ án

Logic chống đặt trùng phòng:

```text
existing.CheckIn < new.CheckOut && existing.CheckOut > new.CheckIn
```

Check-out ngày N và check-in ngày N của khách khác **được phép**. Booking `Cancelled` / `NoShow` không chặn phòng.
