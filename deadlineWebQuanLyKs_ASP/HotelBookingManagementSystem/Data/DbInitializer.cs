using HotelBookingManagementSystem.Helpers;
using HotelBookingManagementSystem.Models;
using HotelBookingManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingManagementSystem.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await db.Database.MigrateAsync();
            await UpgradeMediaPaths(db);

            foreach (var role in new[] { RoleNames.Admin, RoleNames.Manager, RoleNames.HotelStaff, RoleNames.Customer })
            {
                if (!await roles.RoleExistsAsync(role))
                    await roles.CreateAsync(new IdentityRole(role));
            }

            if (!await db.Hotels.AnyAsync())
                await SeedCatalog(db);

            await SeedUsers(users, db);

            if (!await db.Bookings.AnyAsync())
                await SeedTransactions(db, users);
        }

        private static async Task UpgradeMediaPaths(ApplicationDbContext db)
        {
            var hotels = await db.Hotels.Where(h => h.Thumbnail != null && h.Thumbnail.EndsWith(".svg")).ToListAsync();
            foreach (var h in hotels) h.Thumbnail = h.Thumbnail!.Replace(".svg", ".jpg");

            var rooms = await db.Rooms.Where(r => r.Thumbnail != null && r.Thumbnail.EndsWith(".svg")).ToListAsync();
            foreach (var r in rooms) r.Thumbnail = r.Thumbnail!.Replace(".svg", ".jpg");

            var images = await db.RoomImages.Where(i => i.ImageUrl.EndsWith(".svg")).ToListAsync();
            foreach (var i in images) i.ImageUrl = i.ImageUrl.Replace(".svg", ".jpg");

            var people = await db.Users.Where(u => u.Avatar != null && u.Avatar.EndsWith(".svg")).ToListAsync();
            foreach (var u in people) u.Avatar = u.Avatar!.Replace(".svg", ".jpg");

            if (hotels.Count + rooms.Count + images.Count + people.Count > 0)
                await db.SaveChangesAsync();
        }

        private static async Task SeedCatalog(ApplicationDbContext db)
        {
            var hotels = new List<Hotel>
            {
                H("Grand Hanoi Hotel", "Khách sạn 5 sao giữa lòng Cầu Giấy, kiến trúc hiện đại, dịch vụ chuẩn quốc tế và tầm nhìn thành phố ấn tượng.", "Số 18 Trần Duy Hưng, Cầu Giấy, Hà Nội", "Hà Nội", "Cầu Giấy", "02435551201", "hello@grandhanoi.vn", 21.0312, 105.8015, 5, "/images/hotels/hotel-1.jpg"),
                H("Sunrise Hotel", "Khách sạn boutique ngay Hồ Hoàn Kiếm, thuận tiện dạo phố cổ, thưởng thức ẩm thực và khám phá Hà Nội.", "42 Hàng Bài, Hoàn Kiếm, Hà Nội", "Hà Nội", "Hoàn Kiếm", "02439381202", "hi@sunrisehotel.vn", 21.0245, 105.8520, 4, "/images/hotels/hotel-2.jpg"),
                H("Ocean Pearl Resort", "Resort sát biển Mỹ Khê, hồ bơi vô cực, spa cao cấp và không gian nghỉ dưỡng đúng chất Đà Nẵng.", "88 Võ Nguyên Giáp, Sơn Trà, Đà Nẵng", "Đà Nẵng", "Sơn Trà", "02363951203", "stay@oceanpearl.vn", 16.0598, 108.2475, 5, "/images/hotels/hotel-3.jpg"),
                H("Green City Hotel", "Khách sạn giữa lòng Đà Lạt mát mẻ, hướng đồi thông, phù hợp cặp đôi và gia đình muốn nghỉ dưỡng yên tĩnh.", "15 Trần Phú, Phường 3, Đà Lạt", "Đà Lạt", "Phường 3", "02633551204", "info@greencity.vn", 11.9404, 108.4583, 4, "/images/hotels/hotel-4.jpg"),
                H("Royal Palace Hotel", "Khách sạn sang trọng trung tâm Quận 1, gần nhà thờ Đức Bà, phù hợp công tác và du lịch cao cấp.", "27 Đồng Khởi, Quận 1, TP. Hồ Chí Minh", "TP. Hồ Chí Minh", "Quận 1", "02838221205", "booking@royalpalace.vn", 10.7769, 106.7009, 5, "/images/hotels/hotel-5.jpg")
            };
            db.Hotels.AddRange(hotels);

            var types = new List<RoomType>
            {
                new() { Name = "Standard Room", Description = "Phòng tiêu chuẩn ấm cúng, đầy đủ tiện nghi cơ bản.", MaxAdults = 2, MaxChildren = 1, BedType = "Queen", Area = 22, BasePrice = 650000, Status = true },
                new() { Name = "Superior Room", Description = "Rộng hơn Standard, view đẹp hơn và trang thiết bị nâng cấp.", MaxAdults = 2, MaxChildren = 1, BedType = "King", Area = 28, BasePrice = 950000, Status = true },
                new() { Name = "Deluxe Room", Description = "Phòng cao cấp, nội thất tinh tế, phù hợp kỳ nghỉ lãng mạn.", MaxAdults = 2, MaxChildren = 1, BedType = "King", Area = 35, BasePrice = 1450000, Status = true },
                new() { Name = "Executive Room", Description = "Dành cho khách công tác, có bàn làm việc lớn và lounge.", MaxAdults = 2, MaxChildren = 1, BedType = "King", Area = 38, BasePrice = 1850000, Status = true },
                new() { Name = "Suite Room", Description = "Suite với phòng khách riêng, không gian sang trọng.", MaxAdults = 3, MaxChildren = 2, BedType = "King", Area = 55, BasePrice = 2800000, Status = true },
                new() { Name = "Family Room", Description = "Phòng gia đình rộng, 2 giường lớn, phù hợp 4-5 khách.", MaxAdults = 4, MaxChildren = 2, BedType = "Twin", Area = 48, BasePrice = 2200000, Status = true },
                new() { Name = "Presidential Suite", Description = "Hạng phòng đỉnh cao, tầm nhìn panorama và dịch vụ butler.", MaxAdults = 4, MaxChildren = 2, BedType = "King", Area = 90, BasePrice = 5500000, Status = true }
            };
            db.RoomTypes.AddRange(types);

            var amenities = new (string Name, string Icon)[]
            {
                ("Wi-Fi miễn phí","fa-wifi"),("Điều hòa","fa-snowflake"),("TV","fa-tv"),("Smart TV","fa-display"),
                ("Mini Bar","fa-martini-glass"),("Tủ lạnh","fa-cube"),("Máy sấy tóc","fa-wind"),("Ấm đun nước","fa-mug-hot"),
                ("Bàn làm việc","fa-laptop"),("Két sắt","fa-vault"),("Ban công","fa-door-open"),("Bồn tắm","fa-bath"),
                ("Vòi sen","fa-shower"),("Dép","fa-shoe-prints"),("Áo choàng tắm","fa-shirt"),("Khăn tắm","fa-hand-sparkles"),
                ("Nước uống miễn phí","fa-bottle-water"),("Phòng cách âm","fa-volume-xmark"),("Điện thoại","fa-phone"),
                ("Dịch vụ phòng","fa-bell-concierge"),("Tầm nhìn đẹp","fa-mountain-sun"),("Bể bơi","fa-water-ladder")
            }.Select(a => new Amenity { Name = a.Name, Icon = a.Icon, Status = true }).ToList();
            db.Amenities.AddRange(amenities);

            db.HotelServices.AddRange(
                S("Ăn sáng buffet", "Buffet sáng Á - Âu đa dạng.", 250000, "người"),
                S("Đưa đón sân bay", "Xe riêng đưa đón 1 chiều.", 350000, "lượt"),
                S("Thuê xe máy", "Xe tay ga theo ngày.", 150000, "ngày"),
                S("Thuê ô tô", "Xe 4-7 chỗ có tài xế.", 1200000, "ngày"),
                S("Giặt ủi", "Giặt ủi trong ngày.", 80000, "bộ"),
                S("Spa", "Liệu trình thư giãn 60 phút.", 450000, "lần"),
                S("Massage", "Massage body 60 phút.", 400000, "lần"),
                S("Mini Bar", "Gói đồ uống trong tủ lạnh.", 200000, "phòng"),
                S("Room Service", "Phục vụ đồ ăn tại phòng.", 120000, "lần"),
                S("Extra Bed", "Giường phụ cho 1 người.", 300000, "đêm")
            );

            db.Vouchers.AddRange(
                V("WELCOME10", "Chào mừng thành viên mới", "Giảm 10%, tối đa 300.000₫", DiscountType.Percentage, 10, 500000, 300000, 200),
                V("HOTEL50K", "Ưu đãi 50K", "Giảm thẳng 50.000₫", DiscountType.FixedAmount, 50000, 800000, null, 300),
                V("SUMMER20", "Hè rực rỡ", "Giảm 20%, tối đa 500.000₫", DiscountType.Percentage, 20, 1500000, 500000, 150),
                V("WEEKEND15", "Cuối tuần vui vẻ", "Giảm 15%, tối đa 400.000₫", DiscountType.Percentage, 15, 1000000, 400000, 120),
                V("FAMILY100K", "Gia đình thân yêu", "Giảm 100.000₫ cho phòng Family", DiscountType.FixedAmount, 100000, 2000000, null, 80),
                V("VIP25", "Hạng VIP", "Giảm 25%, tối đa 800.000₫", DiscountType.Percentage, 25, 3000000, 800000, 50),
                V("SPRING8", "Xuân sang", "Giảm 8% không giới hạn", DiscountType.Percentage, 8, 0, null, 250),
                V("NIGHT200K", "Đêm vàng", "Giảm 200.000₫", DiscountType.FixedAmount, 200000, 2500000, null, 60)
            );

            await db.SaveChangesAsync();

            var rooms = BuildRooms(hotels, types);
            db.Rooms.AddRange(rooms);
            await db.SaveChangesAsync();

            var amenityIds = amenities.Select(a => a.AmenityId).ToList();
            var images = new List<RoomImage>();
            var links = new List<RoomAmenity>();
            var rng = new Random(21);
            foreach (var room in rooms)
            {
                images.Add(new RoomImage { RoomId = room.RoomId, ImageUrl = room.Thumbnail!, IsThumbnail = true, DisplayOrder = 1 });
                images.Add(new RoomImage { RoomId = room.RoomId, ImageUrl = $"/images/rooms/room-{((room.RoomId + 2) % 12) + 1:00}.jpg", DisplayOrder = 2 });
                images.Add(new RoomImage { RoomId = room.RoomId, ImageUrl = $"/images/rooms/room-{((room.RoomId + 5) % 12) + 1:00}.jpg", DisplayOrder = 3 });
                var take = rng.Next(8, 14);
                foreach (var aid in amenityIds.OrderBy(_ => rng.Next()).Take(take))
                    links.Add(new RoomAmenity { RoomId = room.RoomId, AmenityId = aid });
            }
            db.RoomImages.AddRange(images);
            db.RoomAmenities.AddRange(links);
            await db.SaveChangesAsync();
        }

        private static List<Room> BuildRooms(List<Hotel> hotels, List<RoomType> types)
        {
            var list = new List<Room>();
            var specs = new (int TypeIdx, string Num, string Name, int Floor, decimal Area, decimal Price, decimal? Discount, int Adults, int Child, string Bed, int Beds, string View, bool Feat)[]
            {
                (0,"101","Standard City View",1,22,650000,590000,2,1,"Queen",1,"Thành phố",false),
                (0,"102","Standard Garden",1,22,680000,null,2,1,"Queen",1,"Vườn",false),
                (1,"103","Superior Twin",1,28,980000,890000,2,1,"Twin",2,"Thành phố",true),
                (2,"201","Deluxe King",2,35,1550000,1350000,2,1,"King",1,"Hồ bơi",true),
                (2,"202","Deluxe Twin",2,35,1500000,null,2,1,"Twin",2,"Thành phố",false),
                (5,"203","Family Connecting",2,48,2300000,1990000,4,2,"Twin",2,"Vườn",true),
                (3,"301","Executive Lounge",3,38,1950000,1750000,2,1,"King",1,"Thành phố",true),
                (4,"302","Junior Suite",3,55,2950000,2590000,3,2,"King",1,"Panorama",true),
                (6,"401","Presidential Suite",4,92,5800000,null,4,2,"King",1,"Panorama",true)
            };

            int img = 1;
            foreach (var hotel in hotels)
            {
                foreach (var s in specs)
                {
                    var t = types[s.TypeIdx];
                    var name = $"{s.Name} - {hotel.Name}";
                    var priceAdj = hotel.StarRating == 5 ? 1.15m : 1m;
                    if (hotel.City.Contains("Đà Nẵng") || hotel.City.Contains("Hồ Chí Minh")) priceAdj += 0.08m;
                    var price = Math.Round(s.Price * priceAdj / 1000) * 1000;
                    var disc = s.Discount.HasValue ? Math.Round(s.Discount.Value * priceAdj / 1000) * 1000 : (decimal?)null;
                    var roomImg = $"/images/rooms/room-{((img++ - 1) % 12) + 1:00}.jpg";
                    list.Add(new Room
                    {
                        HotelId = hotel.HotelId,
                        RoomTypeId = t.RoomTypeId,
                        RoomNumber = s.Num,
                        Name = name,
                        Slug = SlugHelper.Generate(name + "-" + hotel.HotelId),
                        Description = $"{s.Name} tại {hotel.Name}. Không gian {s.Area}m², {s.Bed.ToLower()} bed, tầm nhìn {s.View.ToLower()}. Nội thất hiện đại, ánh sáng tự nhiên và tiện nghi đầy đủ cho kỳ nghỉ thoải mái.",
                        Floor = s.Floor,
                        Area = s.Area,
                        PricePerNight = price,
                        DiscountPrice = disc,
                        AdultCapacity = s.Adults,
                        ChildCapacity = s.Child,
                        BedType = s.Bed,
                        NumberOfBeds = s.Beds,
                        ViewType = s.View,
                        SmokingAllowed = s.Num.EndsWith("2"),
                        BreakfastIncluded = s.TypeIdx >= 1,
                        HasBalcony = s.Floor >= 2,
                        HasNiceView = s.View is "Panorama" or "Hồ bơi" or "Vườn",
                        HasAirConditioner = true,
                        HasWifi = true,
                        HasBathtub = s.TypeIdx >= 3,
                        Thumbnail = roomImg,
                        Status = RoomStatus.Available,
                        CancellationPolicy = "Hủy miễn phí trước 24 giờ. Sau thời điểm này tính 50% đêm đầu.",
                        ExtraFee = s.TypeIdx >= 4 ? 150000 : 0,
                        IsFeatured = s.Feat,
                        CreatedAt = new DateTime(2025, 11, 1).AddDays(img)
                    });
                }
            }
            return list;
        }

        private static async Task SeedUsers(UserManager<ApplicationUser> users, ApplicationDbContext db)
        {
            async Task Ensure(string email, string password, string name, string role, string phone, int? hotelId, Gender gender, string avatar)
            {
                if (await users.FindByEmailAsync(email) != null) return;
                var u = new ApplicationUser
                {
                    UserName = email, Email = email, EmailConfirmed = true, FullName = name, PhoneNumber = phone,
                    Gender = gender, IsActive = true, HotelId = hotelId, CreatedAt = new DateTime(2025, 10, 1),
                    Avatar = avatar, Address = "Việt Nam", IdentityNumber = "0" + Random.Shared.Next(100000000, 999999999).ToString()
                };
                await users.CreateAsync(u, password);
                await users.AddToRoleAsync(u, role);
            }

            var h1 = db.Hotels.OrderBy(h => h.HotelId).First().HotelId;
            await Ensure("admin@hotel.com", "Admin@123", "Quản trị hệ thống", RoleNames.Admin, "0901000001", null, Gender.Male, "/images/users/avatar-admin.jpg");
            await Ensure("manager@hotel.com", "Manager@123", "Nguyễn Thị Mai", RoleNames.Manager, "0901000002", h1, Gender.Female, "/images/users/avatar-manager.jpg");
            await Ensure("staff@hotel.com", "Staff@123", "Trần Văn Hùng", RoleNames.HotelStaff, "0901000003", h1, Gender.Male, "/images/users/avatar-staff.jpg");
            await Ensure("staff2@hotel.com", "Staff@123", "Lê Thị Hoa", RoleNames.HotelStaff, "0901000004", db.Hotels.Skip(1).First().HotelId, Gender.Female, "/images/users/avatar-staff.jpg");
            await Ensure("staff3@hotel.com", "Staff@123", "Phạm Quốc Bảo", RoleNames.HotelStaff, "0901000005", db.Hotels.Skip(2).First().HotelId, Gender.Male, "/images/users/avatar-staff.jpg");
            await Ensure("customer@hotel.com", "Customer@123", "Nguyễn Văn An", RoleNames.Customer, "0912000001", null, Gender.Male, "/images/users/avatar-1.jpg");

            var extra = new (string Email, string Name, string Phone, Gender G)[]
            {
                ("lan.pham@gmail.com","Phạm Thị Lan","0912000002",Gender.Female),
                ("minh.tran@gmail.com","Trần Đức Minh","0912000003",Gender.Male),
                ("huong.le@gmail.com","Lê Ngọc Hương","0912000004",Gender.Female),
                ("nam.nguyen@gmail.com","Nguyễn Hoàng Nam","0912000005",Gender.Male),
                ("thao.vo@gmail.com","Võ Thanh Thảo","0912000006",Gender.Female),
                ("duc.bui@gmail.com","Bùi Anh Đức","0912000007",Gender.Male),
                ("my.dang@gmail.com","Đặng Hà My","0912000008",Gender.Female),
                ("khoa.ho@gmail.com","Hồ Minh Khoa","0912000009",Gender.Male),
                ("trang.do@gmail.com","Đỗ Thu Trang","0912000010",Gender.Female),
                ("long.pham@gmail.com","Phạm Tuấn Long","0912000011",Gender.Male),
                ("yen.ngo@gmail.com","Ngô Hải Yến","0912000012",Gender.Female)
            };
            foreach (var c in extra)
                await Ensure(c.Email, "Customer@123", c.Name, RoleNames.Customer, c.Phone, null, c.G, "/images/users/avatar-1.jpg");
        }

        private static async Task SeedTransactions(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            var customers = (await users.GetUsersInRoleAsync(RoleNames.Customer)).ToList();
            var rooms = await db.Rooms.Include(r => r.Hotel).ToListAsync();
            var services = await db.HotelServices.ToListAsync();
            var vouchers = await db.Vouchers.ToListAsync();
            var rng = new Random(2026);

            var bookings = new List<Booking>();
            int seq = 1;
            var occupied = new Dictionary<int, List<(DateTime In, DateTime Out, BookingStatus St)>>();

            void AddBooking(Room room, ApplicationUser user, DateTime cin, DateTime cout, BookingStatus st, PaymentStatus pay, bool useVoucher)
            {
                if (!occupied.ContainsKey(room.RoomId)) occupied[room.RoomId] = new();
                var block = st is BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.CheckedIn;
                if (block && occupied[room.RoomId].Any(x => x.In < cout && x.Out > cin && x.St is BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.CheckedIn))
                    return;

                var nights = Math.Max(1, (cout - cin).Days);
                var sub = room.DisplayPrice * nights;
                var extra = room.ExtraFee * nights;
                var svc = rng.Next(0, 3) == 0 ? services[rng.Next(services.Count)].Price : 0;
                var taxable = sub + extra + svc;
                decimal discount = 0;
                int? voucherId = null;
                if (useVoucher && vouchers.Count > 0)
                {
                    var v = vouchers[rng.Next(vouchers.Count)];
                    voucherId = v.VoucherId;
                    discount = v.DiscountType == DiscountType.Percentage
                        ? Math.Min(taxable * v.DiscountValue / 100, v.MaximumDiscount ?? taxable)
                        : v.DiscountValue;
                    if (discount > taxable) discount = taxable;
                    v.UsedCount++;
                }
                var after = Math.Max(0, taxable - discount);
                var tax = Math.Round(after * 0.08m, 0);
                var created = cin.AddDays(-rng.Next(5, 25));
                if (created > DateTime.Now) created = DateTime.Now.AddDays(-1);

                bookings.Add(new Booking
                {
                    BookingCode = BookingCodeGenerator.GenerateFromDate(created, seq++),
                    UserId = user.Id,
                    HotelId = room.HotelId,
                    RoomId = room.RoomId,
                    CheckInDate = cin,
                    CheckOutDate = cout,
                    Adults = Math.Min(room.AdultCapacity, rng.Next(1, room.AdultCapacity + 1)),
                    Children = rng.Next(0, room.ChildCapacity + 1),
                    NumberOfRooms = 1,
                    NumberOfNights = nights,
                    RoomPrice = room.DisplayPrice,
                    Subtotal = sub,
                    ExtraFee = extra,
                    ServiceAmount = svc,
                    DiscountAmount = Math.Round(discount, 0),
                    TaxAmount = tax,
                    TotalAmount = after + tax,
                    VoucherId = voucherId,
                    BookingStatus = st,
                    PaymentStatus = pay,
                    GuestName = user.FullName,
                    GuestEmail = user.Email ?? "",
                    GuestPhone = user.PhoneNumber ?? "",
                    GuestIdentityNumber = user.IdentityNumber,
                    SpecialRequest = rng.Next(0, 4) == 0 ? "Phòng tầng cao, giường đôi." : null,
                    CreatedAt = created,
                    CancelledAt = st == BookingStatus.Cancelled ? created.AddDays(1) : null,
                    CancelReason = st == BookingStatus.Cancelled ? "Đổi lịch trình cá nhân" : null
                });
                occupied[room.RoomId].Add((cin, cout, st));
            }

            var today = DateTime.Today;
            // Past checked-out bookings across months for charts
            for (var month = 1; month <= 9; month++)
            {
                for (var k = 0; k < 4; k++)
                {
                    var room = rooms[rng.Next(rooms.Count)];
                    var user = customers[rng.Next(customers.Count)];
                    var cin = new DateTime(2026, month, Math.Min(20, 3 + k * 5));
                    AddBooking(room, user, cin, cin.AddDays(rng.Next(1, 4)), BookingStatus.CheckedOut, PaymentStatus.Paid, k % 2 == 0);
                }
            }

            // Current staying
            for (var i = 0; i < 4; i++)
            {
                var room = rooms[i];
                room.Status = RoomStatus.Occupied;
                AddBooking(room, customers[i % customers.Count], today.AddDays(-1), today.AddDays(2), BookingStatus.CheckedIn, PaymentStatus.Paid, false);
            }

            // Upcoming confirmed / pending
            for (var i = 0; i < 10; i++)
            {
                var room = rooms[(i + 8) % rooms.Count];
                var cin = today.AddDays(5 + i * 2);
                AddBooking(room, customers[i % customers.Count], cin, cin.AddDays(2), i % 3 == 0 ? BookingStatus.Pending : BookingStatus.Confirmed,
                    i % 3 == 0 ? PaymentStatus.Unpaid : PaymentStatus.Paid, i % 2 == 0);
            }

            // Cancelled
            for (var i = 0; i < 6; i++)
            {
                var room = rooms[(i + 15) % rooms.Count];
                var cin = new DateTime(2026, 4 + (i % 5), 8);
                AddBooking(room, customers[i % customers.Count], cin, cin.AddDays(2), BookingStatus.Cancelled, PaymentStatus.Refunded, false);
            }

            db.Bookings.AddRange(bookings);
            await db.SaveChangesAsync();

            var payments = new List<Payment>();
            foreach (var b in bookings.Where(x => x.PaymentStatus is PaymentStatus.Paid or PaymentStatus.Refunded or PaymentStatus.Pending))
            {
                payments.Add(new Payment
                {
                    BookingId = b.BookingId,
                    TransactionCode = $"TX{b.CreatedAt:yyyyMMdd}{b.BookingId:D4}",
                    PaymentMethod = (PaymentMethod)rng.Next(1, 6),
                    Amount = b.TotalAmount,
                    PaymentStatus = b.PaymentStatus,
                    PaidAt = b.PaymentStatus == PaymentStatus.Paid ? b.CreatedAt.AddHours(1) : null,
                    CreatedAt = b.CreatedAt.AddMinutes(20)
                });
            }
            db.Payments.AddRange(payments);

            var reviewTexts = new (string Title, string Comment, int Rating)[]
            {
                ("Phòng sạch sẽ, nhân viên nhiệt tình", "Phòng sạch sẽ, nhân viên nhiệt tình và vị trí thuận tiện. Sẽ quay lại.", 5),
                ("Buffet sáng đa dạng", "Khách sạn đẹp, phòng rộng, buffet sáng khá đa dạng. Rất hài lòng.", 5),
                ("Trải nghiệm tốt", "Trải nghiệm tốt, sẽ quay lại vào lần sau. View buổi tối rất đẹp.", 5),
                ("Vị trí trung tâm", "Gần nhiều điểm tham quan, di chuyển dễ dàng. Phòng hơi nhỏ nhưng gọn gàng.", 4),
                ("Đáng giá tiền", "Giá hợp lý so với chất lượng. Giường êm, nước nóng ổn định.", 4),
                ("Yên tĩnh, dễ ngủ", "Phòng cách âm tốt, ban đêm rất yên. Nhân viên lễ tân thân thiện.", 5),
                ("Hướng biển tuyệt", "Ban công nhìn ra biển, hoàng hôn đẹp. Hồ bơi sạch.", 5),
                ("Phù hợp gia đình", "Phòng family rộng, có 2 giường lớn, trẻ em rất thích.", 4),
                ("Check-in nhanh", "Làm thủ tục nhanh, được nâng hạng phòng. Ấn tượng tốt.", 5),
                ("Sẽ giới thiệu bạn bè", "Dịch vụ spa dễ chịu, room service giao nhanh. Đáng để thử.", 4)
            };

            var checkedOut = bookings.Where(b => b.BookingStatus == BookingStatus.CheckedOut).ToList();
            var reviews = new List<Review>();
            for (var i = 0; i < Math.Min(32, checkedOut.Count); i++)
            {
                var b = checkedOut[i];
                var t = reviewTexts[i % reviewTexts.Length];
                var rating = Math.Clamp(t.Rating + (i % 3 == 0 ? -1 : 0), 3, 5);
                reviews.Add(new Review
                {
                    UserId = b.UserId,
                    RoomId = b.RoomId,
                    BookingId = b.BookingId,
                    Rating = rating,
                    CleanlinessRating = Math.Clamp(rating, 3, 5),
                    LocationRating = Math.Clamp(rating + (i % 2), 3, 5),
                    ServiceRating = rating,
                    StaffRating = Math.Clamp(rating, 4, 5),
                    ValueRating = Math.Clamp(rating - 1, 3, 5),
                    Title = t.Title,
                    Comment = t.Comment,
                    Status = true,
                    CreatedAt = b.CheckOutDate.AddDays(1)
                });
            }
            db.Reviews.AddRange(reviews);

            var favs = new List<Favorite>();
            foreach (var c in customers.Take(8))
            {
                foreach (var room in rooms.OrderBy(_ => rng.Next()).Take(3))
                {
                    if (favs.Any(f => f.UserId == c.Id && f.RoomId == room.RoomId)) continue;
                    favs.Add(new Favorite { UserId = c.Id, RoomId = room.RoomId, CreatedAt = DateTime.Now.AddDays(-rng.Next(1, 40)) });
                }
            }
            db.Favorites.AddRange(favs);

            var notes = new List<Notification>();
            foreach (var b in bookings.Take(25))
            {
                notes.Add(new Notification
                {
                    UserId = b.UserId,
                    Title = "Đặt phòng thành công",
                    Message = $"Mã đặt phòng {b.BookingCode} đã được tạo.",
                    Type = NotificationType.BookingSuccess,
                    IsRead = rng.Next(0, 2) == 0,
                    CreatedAt = b.CreatedAt
                });
            }
            foreach (var c in customers.Take(10))
            {
                notes.Add(new Notification
                {
                    UserId = c.Id,
                    Title = "Voucher mới dành cho bạn",
                    Message = "Mã SUMMER20 giảm 20% tối đa 500.000₫ đang chờ bạn sử dụng.",
                    Type = NotificationType.NewVoucher,
                    CreatedAt = DateTime.Now.AddDays(-2)
                });
            }
            db.Notifications.AddRange(notes);

            db.Contacts.AddRange(
                new Contact { FullName = "Nguyễn Văn An", Email = "customer@hotel.com", Phone = "0912000001", Subject = "Hỏi chính sách hủy phòng", Message = "Tôi muốn hỏi điều kiện hủy phòng trước 48 giờ.", Status = ContactStatus.Resolved, CreatedAt = DateTime.Now.AddDays(-12), AdminNote = "Đã phản hồi qua email." },
                new Contact { FullName = "Phạm Thị Lan", Email = "lan.pham@gmail.com", Phone = "0912000002", Subject = "Xuất hóa đơn VAT", Message = "Xin hỗ trợ xuất hóa đơn cho công ty.", Status = ContactStatus.Processing, CreatedAt = DateTime.Now.AddDays(-3) },
                new Contact { FullName = "Trần Đức Minh", Email = "minh.tran@gmail.com", Phone = "0912000003", Subject = "Đặt đoàn 15 khách", Message = "Cần báo giá phòng cho đoàn công tác tháng 10.", Status = ContactStatus.New, CreatedAt = DateTime.Now.AddDays(-1) },
                new Contact { FullName = "Lê Ngọc Hương", Email = "huong.le@gmail.com", Phone = "0912000004", Subject = "Góp ý dịch vụ spa", Message = "Spa rất tốt nhưng nên tăng khung giờ tối.", Status = ContactStatus.Resolved, CreatedAt = DateTime.Now.AddDays(-20) },
                new Contact { FullName = "Khách vãng lai", Email = "guest@example.com", Phone = "0987654321", Subject = "Liên hệ hợp tác", Message = "Muốn tìm hiểu chương trình affiliate.", Status = ContactStatus.New, CreatedAt = DateTime.Now }
            );

            db.SystemLogs.Add(new SystemLog { Action = "Seed", Entity = "System", Description = "Khởi tạo dữ liệu mẫu", CreatedAt = DateTime.Now });
            await db.SaveChangesAsync();
        }

        private static Hotel H(string name, string desc, string address, string city, string district, string phone, string email, double lat, double lng, int star, string thumb) => new()
        {
            Name = name, Slug = SlugHelper.Generate(name), Description = desc, Address = address, City = city, District = district,
            Phone = phone, Email = email, Website = "https://www." + email.Split('@')[1], Latitude = lat, Longitude = lng,
            StarRating = star, CheckInTime = "14:00", CheckOutTime = "12:00", Thumbnail = thumb, Status = true, CreatedAt = new DateTime(2025, 9, 1)
        };

        private static HotelService S(string name, string desc, decimal price, string unit) => new()
        { Name = name, Description = desc, Price = price, Unit = unit, Status = true };

        private static Voucher V(string code, string name, string desc, DiscountType type, decimal value, decimal min, decimal? max, int qty) => new()
        {
            Code = code, Name = name, Description = desc, DiscountType = type, DiscountValue = value, MinimumOrder = min,
            MaximumDiscount = max, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31),
            Quantity = qty, UsedCount = 0, Status = true
        };
    }
}
