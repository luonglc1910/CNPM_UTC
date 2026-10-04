using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Data;

/// <summary>
/// Nạp dữ liệu khởi tạo — NFR-08. Chạy một lần lúc khởi động ứng dụng.
/// Idempotent: mỗi khối chỉ chạy khi bảng tương ứng còn rỗng, nên gọi lại nhiều lần vẫn an toàn.
/// Không dùng HasData trong migration vì mật khẩu phải băm lúc chạy, không băm sẵn được.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(HotelDbContext db)
    {
        await SeedSettingsAsync(db);
        await SeedEmployeesAsync(db);
        await SeedRoomTypesAndRoomsAsync(db);
        await SeedServicesAsync(db);
        await SeedGalleryAsync(db);
        await SeedAmenitiesAsync(db);
        await SeedPromotionsAsync(db);
        await SeedTestimonialsAsync(db);
        await SeedGuestsAsync(db);
        await SeedReservationsAndStaysAsync(db);
    }

    /// <summary>
    /// Tham số cấu hình mặc định — FR-A07, BR-01 đến BR-05.
    /// Bảng giá trị nằm ở <see cref="SystemSettingDefaults"/> để dùng chung với nút
    /// "Khôi phục mặc định" ở SCR-A12. Chỉ thêm khóa còn thiếu, không đụng khóa đã có,
    /// nên nâng cấp phiên bản có thêm tham số mới vẫn chạy được trên DB cũ.
    /// </summary>
    private static async Task SeedSettingsAsync(HotelDbContext db)
    {
        var existing = await db.SystemSettings.Select(s => s.Key).ToListAsync();

        var missing = SystemSettingDefaults.All
            .Where(d => !existing.Contains(d.Key))
            .Select(d => new SystemSetting
            {
                Key = d.Key,
                Value = d.Value,
                Group = d.Group,
                Description = d.Description
            });

        db.SystemSettings.AddRange(missing);
        await db.SaveChangesAsync();
    }

    /// <summary>Tài khoản mặc định cho 2 vai trò. Mật khẩu ban đầu đều là 123456.</summary>
    private static async Task SeedEmployeesAsync(HotelDbContext db)
    {
        if (await db.Employees.AnyAsync())
        {
            return;
        }

        var employees = new[]
        {
            new Employee
            {
                Code = "NV001",
                FullName = "Quản trị hệ thống",
                PhoneNumber = "0900000001",
                UserName = "admin",
                Role = EmployeeRole.Admin,
                PasswordHash = PasswordHasher.Hash("123456"),
                MustChangePassword = false
            },
            new Employee
            {
                Code = "NV002",
                FullName = "Nguyễn Thị Lễ Tân",
                PhoneNumber = "0900000002",
                UserName = "letan",
                Role = EmployeeRole.Receptionist,
                PasswordHash = PasswordHasher.Hash("123456"),
                MustChangePassword = false
            }
        };

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();
    }

    /// <summary>3 loại phòng và 30 phòng trải trên 3 tầng.</summary>
    private static async Task SeedRoomTypesAndRoomsAsync(HotelDbContext db)
    {
        if (await db.RoomTypes.AnyAsync()) return;

        var standard = new RoomType
        {
            Code = "STD",
            Name = "Standard Room",
            Slug = "standard-room",
            StandardCapacity = 2, MaxCapacity = 3,
            BasePricePerNight = 500_000m, ExtraGuestFeePerNight = 150_000m, ExtraBedFeePerNight = 200_000m,
            PriceFirstHour = 120_000m, PriceExtraHour = 20_000m, PriceOvernight = 350_000m,
            Amenities = "Điều hòa, TV, Nóng lạnh, Wifi",
            Description = "Phòng tiêu chuẩn 2 khách, tiết kiệm",
            DetailDescription = "<p>Phòng nghỉ tiêu chuẩn với không gian ấm cúng, thiết kế hiện đại, đầy đủ tiện nghi cơ bản mang lại sự thoải mái nhất. Phù hợp cho khách công tác hoặc cặp đôi.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=800&auto=format&fit=crop",
            ViewType = "Hướng phố"
        };

        var superior = new RoomType
        {
            Code = "SUP",
            Name = "Superior Room",
            Slug = "superior-room",
            StandardCapacity = 2, MaxCapacity = 3,
            BasePricePerNight = 750_000m, ExtraGuestFeePerNight = 200_000m, ExtraBedFeePerNight = 250_000m,
            PriceFirstHour = 150_000m, PriceExtraHour = 30_000m, PriceOvernight = 500_000m,
            Amenities = "Điều hòa, TV, Nóng lạnh, Wifi, Minibar, Sofa",
            Description = "Phòng rộng rãi, có cửa sổ lớn",
            DetailDescription = "<p>Nâng tầm trải nghiệm với phòng Superior rộng rãi, ánh sáng tự nhiên ngập tràn qua khung cửa sổ lớn. Bố trí nội thất thanh lịch và hiện đại.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1590490360182-c33d57733427?q=80&w=800&auto=format&fit=crop",
            ViewType = "Hướng vườn"
        };

        var deluxe = new RoomType
        {
            Code = "DLX",
            Name = "Deluxe Ocean View",
            Slug = "deluxe-ocean-view",
            StandardCapacity = 2, MaxCapacity = 4,
            BasePricePerNight = 1_200_000m, ExtraGuestFeePerNight = 250_000m, ExtraBedFeePerNight = 300_000m,
            PriceFirstHour = 200_000m, PriceExtraHour = 50_000m, PriceOvernight = 800_000m,
            Amenities = "Điều hòa, Smart TV, Wifi, Minibar, Bồn tắm, Ban công",
            Description = "Phòng view biển cực chill",
            DetailDescription = "<p>Tận hưởng làn gió biển tươi mát từ ban công riêng. Phòng Deluxe Ocean View có bồn tắm nằm thư giãn cùng các tiện nghi cao cấp dành riêng cho bạn.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?q=80&w=800&auto=format&fit=crop",
            ViewType = "Hướng biển trực diện"
        };

        var family = new RoomType
        {
            Code = "FAM",
            Name = "Family Suite",
            Slug = "family-suite",
            StandardCapacity = 4, MaxCapacity = 6,
            BasePricePerNight = 1_800_000m, ExtraGuestFeePerNight = 300_000m, ExtraBedFeePerNight = 350_000m,
            PriceFirstHour = 300_000m, PriceExtraHour = 80_000m, PriceOvernight = 1_500_000m,
            Amenities = "2 giường đôi, Điều hòa, Smart TV, Bếp mini, Bồn tắm",
            Description = "Không gian lý tưởng cho gia đình",
            DetailDescription = "<p>Gắn kết yêu thương với phòng Family Suite. Không gian rộng lớn với 2 giường đôi, khu vực bếp mini tiện lợi để hâm nóng đồ ăn cho bé.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1560067174-c5a3a8f37060?q=80&w=800&auto=format&fit=crop",
            ViewType = "Hướng hồ bơi"
        };

        var villa = new RoomType
        {
            Code = "VIL",
            Name = "Beachfront Pool Villa",
            Slug = "beachfront-pool-villa",
            StandardCapacity = 2, MaxCapacity = 4,
            BasePricePerNight = 3_500_000m, ExtraGuestFeePerNight = 500_000m, ExtraBedFeePerNight = 500_000m,
            PriceFirstHour = 500_000m, PriceExtraHour = 100_000m, PriceOvernight = 2_500_000m,
            Amenities = "Hồ bơi riêng, Sân vườn, Trà/Cà phê cao cấp, Loa Bluetooth",
            Description = "Biệt thự sát biển với hồ bơi riêng",
            DetailDescription = "<p>Thiên đường nghỉ dưỡng thực thụ. Bạn sẽ có một hồ bơi riêng rợp bóng dừa, chỉ vài bước chân là chạm tới bãi cát trắng mịn.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?q=80&w=800&auto=format&fit=crop",
            ViewType = "Mặt biển (Beachfront)"
        };

        var vip = new RoomType
        {
            Code = "VIP",
            Name = "Presidential Suite",
            Slug = "presidential-suite",
            StandardCapacity = 2, MaxCapacity = 4,
            BasePricePerNight = 5_000_000m, ExtraGuestFeePerNight = 800_000m, ExtraBedFeePerNight = 800_000m,
            PriceFirstHour = 800_000m, PriceExtraHour = 200_000m, PriceOvernight = 4_000_000m,
            Amenities = "Đặc quyền Club, Quản gia riêng, Phòng họp, Bồn tắm sục Jacuzzi",
            Description = "Tuyệt tác thiết kế đẳng cấp thượng lưu",
            DetailDescription = "<p>Hạng phòng cao cấp nhất dành cho giới thượng lưu. Nội thất xa xỉ, có đặc quyền sử dụng VIP Lounge và dịch vụ quản gia cá nhân 24/7.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=800&auto=format&fit=crop",
            ViewType = "Hướng toàn cảnh Panorama"
        };

        db.RoomTypes.AddRange(standard, superior, deluxe, family, villa, vip);
        await db.SaveChangesAsync();

        var rooms = new List<Room>();

        // STD: Tầng 1
        for (var i = 1; i <= 10; i++) rooms.Add(new Room { RoomNumber = $"1{i:00}", Floor = 1, RoomTypeId = standard.Id });
        
        // SUP: Tầng 2 (201-208)
        for (var i = 1; i <= 8; i++) rooms.Add(new Room { RoomNumber = $"2{i:00}", Floor = 2, RoomTypeId = superior.Id });
        
        // DLX: Tầng 3 (301-306)
        for (var i = 1; i <= 6; i++) rooms.Add(new Room { RoomNumber = $"3{i:00}", Floor = 3, RoomTypeId = deluxe.Id });
        
        // FAM: Tầng 4 (401-405)
        for (var i = 1; i <= 5; i++) rooms.Add(new Room { RoomNumber = $"4{i:00}", Floor = 4, RoomTypeId = family.Id });
        
        // VIL: Khu vực V (V01-V05)
        for (var i = 1; i <= 5; i++) rooms.Add(new Room { RoomNumber = $"V0{i}", Floor = 1, RoomTypeId = villa.Id });
        
        // VIP: Penthouse Tầng 5 (501, 502)
        rooms.Add(new Room { RoomNumber = "501", Floor = 5, RoomTypeId = vip.Id });
        rooms.Add(new Room { RoomNumber = "502", Floor = 5, RoomTypeId = vip.Id });

        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();
    }

    /// <summary>Danh mục dịch vụ mẫu, gồm cả hàng minibar có quản lý kho.</summary>
    private static async Task SeedServicesAsync(HotelDbContext db)
    {
        if (!await db.HotelServices.AnyAsync())
        {
            var services = new[]
            {
                // F&B
                new HotelService { Code = "FB001", Name = "Bữa sáng buffet Việt Nam", Description = "Thưởng thức phong vị ẩm thực Á - Âu đa dạng với các món ăn tươi ngon chuẩn vị 5 sao mỗi buổi sáng.", ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.FoodAndBeverage, UnitPrice = 250_000m, Unit = "suất" },
                new HotelService { Code = "FB002", Name = "Phở bò truyền thống", Description = "Nước dùng ninh xương thảo mộc đậm đà kết hợp bò Wagyu thái mỏng mềm ngọt tuyệt hảo.", ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.FoodAndBeverage, UnitPrice = 120_000m, Unit = "tô" },
                new HotelService { Code = "FB003", Name = "Cà phê muối Huế", Description = "Hương vị cà phê phin sánh mịn hòa quyện lớp kem muối béo nhẹ nồng nàn quyến rũ.", ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.FoodAndBeverage, UnitPrice = 65_000m, Unit = "ly" },
                new HotelService { Code = "FB004", Name = "Bún chả Hà Nội", Description = "Chả nướng than hoa thơm nức ăn kèm nước chấm chua ngọt chuẩn vị phố cổ.", ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.FoodAndBeverage, UnitPrice = 150_000m, Unit = "phần" },
                new HotelService { Code = "FB005", Name = "Set Hải sản nướng", Description = "Hải sản tươi sống đánh bắt trong ngày nướng sốt bơ tỏi thơm lừng.", ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.FoodAndBeverage, UnitPrice = 850_000m, Unit = "set" },
                new HotelService { Code = "FB006", Name = "Trà chiều Hoàng Cung", Description = "Thưởng thức các loại trà hoa hảo hạng cùng bánh ngọt sang trọng góc nhìn hồ bơi.", ImageUrl = "https://images.unsplash.com/photo-1597318181409-cf64d0b5d8a2?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.FoodAndBeverage, UnitPrice = 450_000m, Unit = "set" },
                
                // Minibar
                new HotelService { Code = "MB001", Name = "Nước suối LaVie 500ml", Description = "Nước khoáng thiên nhiên tinh khiết đóng chai 500ml.", ImageUrl = "https://images.unsplash.com/photo-1527661591475-527312dd65f5?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.Minibar, UnitPrice = 25_000m, Unit = "chai", IsStockManaged = true, StockQuantity = 200, MinStockLevel = 50 },
                new HotelService { Code = "MB002", Name = "Bia Saigon Special", Description = "Bia lon Saigon Special 330ml mát lạnh.", Category = ServiceCategory.Minibar, UnitPrice = 45_000m, Unit = "lon", IsStockManaged = true, StockQuantity = 150, MinStockLevel = 40 },
                new HotelService { Code = "MB003", Name = "Trà Olong Tea+", Description = "Trà ô long đóng chai giải nhiệt thanh mát.", Category = ServiceCategory.Minibar, UnitPrice = 30_000m, Unit = "chai", IsStockManaged = true, StockQuantity = 100, MinStockLevel = 20 },
                new HotelService { Code = "MB004", Name = "Snack khoai tây Oishi", Description = "Snack khoai tây giòn rụm vị tự nhiên.", Category = ServiceCategory.Minibar, UnitPrice = 35_000m, Unit = "gói", IsStockManaged = true, StockQuantity = 80, MinStockLevel = 20 },
                
                // Spa & Laundry & Transport
                new HotelService { Code = "SP001", Name = "Massage cổ vai gáy (60p)", Description = "Liệu pháp ấn huyệt trị liệu thư giãn giảm căng thẳng đau mỏi cơ nhanh chóng.", ImageUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.Other, UnitPrice = 450_000m, Unit = "lượt" },
                new HotelService { Code = "SP002", Name = "Trị liệu toàn thân đá nóng", Description = "Kỹ thuật chườm đá núi lửa ấm kết hợp tinh dầu thảo mộc thiên nhiên phục hồi năng lượng.", ImageUrl = "https://images.unsplash.com/photo-1519823551278-64ac92734fb1?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.Other, UnitPrice = 850_000m, Unit = "lượt" },
                new HotelService { Code = "LD001", Name = "Giặt ủi tiêu chuẩn", Description = "Dịch vụ giặt sấy là ủi quần áo lấy ngay trong ngày sạch sẽ thơm mát.", ImageUrl = "https://images.unsplash.com/photo-1517677208171-0bc6725a3e60?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.Laundry, UnitPrice = 60_000m, Unit = "kg" },
                new HotelService { Code = "TR001", Name = "Đưa đón sân bay (4 chỗ)", Description = "Xe sedan hạng sang đón đưa sân bay đúng giờ, tài xế lịch sự và tận tâm.", ImageUrl = "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.Transport, UnitPrice = 450_000m, Unit = "lượt" },
                new HotelService { Code = "TR002", Name = "Thuê xe máy tay ga", Description = "Xe máy tay ga đời mới được bảo dưỡng kỹ càng cho chuyến du ngoạn tự do.", ImageUrl = "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?q=80&w=800&auto=format&fit=crop", Category = ServiceCategory.Transport, UnitPrice = 180_000m, Unit = "ngày" }
            };

            await db.HotelServices.AddRangeAsync(services);
            await db.SaveChangesAsync();
        }
        else
        {
            var existingServices = await db.HotelServices.ToListAsync();
            bool hasChanges = false;
            foreach (var s in existingServices)
            {
                if (string.IsNullOrEmpty(s.ImageUrl))
                {
                    s.ImageUrl = s.Category switch {
                        ServiceCategory.FoodAndBeverage => "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=800&auto=format&fit=crop",
                        ServiceCategory.Transport => "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?q=80&w=800&auto=format&fit=crop",
                        ServiceCategory.Minibar => "https://images.unsplash.com/photo-1527661591475-527312dd65f5?q=80&w=800&auto=format&fit=crop",
                        _ => "https://images.unsplash.com/photo-1540555700478-4be289fbecef?q=80&w=800&auto=format&fit=crop"
                    };
                    hasChanges = true;
                }
                if (string.IsNullOrEmpty(s.Description))
                {
                    s.Description = $"Trải nghiệm dịch vụ {s.Name} cao cấp tại Aura Boutique Hotel & Resort với chuẩn mực phục vụ tận tâm.";
                    hasChanges = true;
                }
            }
            if (hasChanges)
            {
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedGalleryAsync(HotelDbContext db)
    {
        if (!await db.HotelGalleryImages.AnyAsync())
        {
            var images = new[]
            {
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?q=80&w=800&auto=format&fit=crop", Title = "Toàn cảnh khu nghỉ dưỡng từ trên cao", SortOrder = 1 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?q=80&w=800&auto=format&fit=crop", Title = "Tiền sảnh mang đậm nét văn hóa", SortOrder = 2 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1502208327471-d5dde4d78995?q=80&w=800&auto=format&fit=crop", Title = "Nét đẹp phụ nữ Việt tại Resort", SortOrder = 3 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?q=80&w=800&auto=format&fit=crop", Title = "Thực đơn tươi mát", SortOrder = 4 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1549488344-1f9b8d2bd1f3?q=80&w=800&auto=format&fit=crop", Title = "Không gian phòng nghỉ thiên nhiên", SortOrder = 5 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?q=80&w=800&auto=format&fit=crop", Title = "Khu nghỉ dưỡng ven biển", SortOrder = 6 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1563911302283-d2bc129e7570?q=80&w=800&auto=format&fit=crop", Title = "Khu vực sảnh đón khách", SortOrder = 7 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1590523277543-a94d2e4eb00b?q=80&w=800&auto=format&fit=crop", Title = "Dịch vụ phòng chuyên nghiệp", SortOrder = 8 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1565557623262-b51c2513a641?q=80&w=800&auto=format&fit=crop", Title = "Tinh hoa ẩm thực ba miền", SortOrder = 9 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?q=80&w=800&auto=format&fit=crop", Title = "Spa trị liệu truyền thống", SortOrder = 10 },
                new HotelGalleryImage { ImageUrl = "https://images.unsplash.com/photo-1533759413974-9e15f3b745ac?q=80&w=800&auto=format&fit=crop", Title = "Bể bơi vô cực ngắm hoàng hôn", SortOrder = 11 }
            };

            await db.HotelGalleryImages.AddRangeAsync(images);
            await db.SaveChangesAsync();
        }
        else
        {
            var galleryImages = await db.HotelGalleryImages.ToListAsync();
            bool updated = false;
            foreach (var img in galleryImages)
            {
                if (img.ImageUrl != null && img.ImageUrl.Contains("photo-1542314831-c6a420325142"))
                {
                    img.ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?q=80&w=800&auto=format&fit=crop";
                    updated = true;
                }
            }
            if (updated)
            {
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedAmenitiesAsync(HotelDbContext db)
    {
        if (await db.Amenities.AnyAsync()) return;
        var amenities = new[]
        {
            new Amenity { Name = "Wifi tốc độ cao", IconSvg = "<i class=\"bi bi-wifi\"></i>" },
            new Amenity { Name = "Smart TV", IconSvg = "<i class=\"bi bi-tv\"></i>" },
            new Amenity { Name = "Bồn tắm nằm", IconSvg = "<i class=\"bi bi-droplet\"></i>" },
            new Amenity { Name = "Ban công riêng", IconSvg = "<i class=\"bi bi-box\"></i>" },
            new Amenity { Name = "Dịch vụ phòng 24/7", IconSvg = "<i class=\"bi bi-bell\"></i>" },
            new Amenity { Name = "Minibar", IconSvg = "<i class=\"bi bi-cup-straw\"></i>" }
        };
        db.Amenities.AddRange(amenities);
        await db.SaveChangesAsync();
    }

    private static async Task SeedPromotionsAsync(HotelDbContext db)
    {
        if (await db.Promotions.AnyAsync()) return;

        var promotions = new[]
        {
            new Promotion
            {
                Title = "Nghỉ Lễ 30/4 - 1/5 Rực Rỡ",
                Description = "Tận hưởng kỳ nghỉ lễ dài ngày bên gia đình. Giảm 20% khi đặt từ 3 đêm trở lên, miễn phí ăn sáng và 1 bữa tối BBQ hải sản.",
                ImageUrl = "https://images.unsplash.com/photo-1552566626-52f8b828add9?q=80&w=800&auto=format&fit=crop",
                PromoCode = "LE304",
                DiscountDailyPercent = 20,
                DiscountHourlyPercent = 10,
                DiscountOvernightPercent = 15,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),
                IsHeroOffer = true
            },
            new Promotion
            {
                Title = "Hè Sôi Động - Trọn Gói Gia Đình",
                Description = "Gói nghỉ dưỡng gia đình 2 ngày 1 đêm bao gồm 2 vé vui chơi công viên nước, miễn phí cho 2 trẻ em dưới 6 tuổi.",
                ImageUrl = "https://images.unsplash.com/photo-1502086223501-7ea6ecd79368?q=80&w=800&auto=format&fit=crop",
                PromoCode = "SUMMER26",
                DiscountDailyPercent = 15,
                DiscountHourlyPercent = 5,
                DiscountOvernightPercent = 10,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3),
                IsHeroOffer = false
            },
            new Promotion
            {
                Title = "Trăng Mật Ngọt Ngào",
                Description = "Gói trăng mật lãng mạn tặng rượu vang, hoa hồng, bánh kem và 01 bữa tối lãng mạn dưới ánh nến. Ưu đãi 10% giá phòng.",
                ImageUrl = "https://images.unsplash.com/photo-1517400508447-f8dd518b86db?q=80&w=800&auto=format&fit=crop",
                PromoCode = "HONEYMOON",
                DiscountDailyPercent = 10,
                DiscountHourlyPercent = 0,
                DiscountOvernightPercent = 10,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(6),
                IsHeroOffer = false
            },
            new Promotion
            {
                Title = "Staycation Cuối Tuần",
                Description = "Trốn khói bụi thành phố, nạp lại năng lượng cuối tuần với gói Staycation siêu ưu đãi giảm ngay 25%.",
                ImageUrl = "https://images.unsplash.com/photo-1522798514397-e0e62ec87c69?q=80&w=800&auto=format&fit=crop",
                PromoCode = "WEEKEND25",
                DiscountDailyPercent = 25,
                DiscountHourlyPercent = 15,
                DiscountOvernightPercent = 20,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30),
                IsHeroOffer = false
            },
            new Promotion
            {
                Title = "Ưu Đãi Đặt Sớm (Early Bird)",
                Description = "Lên kế hoạch sớm, tiết kiệm lớn! Giảm tới 30% khi đặt phòng trước 45 ngày so với ngày nhận phòng.",
                ImageUrl = "https://images.unsplash.com/photo-1506059612708-99d6c258160e?q=80&w=800&auto=format&fit=crop",
                PromoCode = "EARLY30",
                DiscountDailyPercent = 30,
                DiscountHourlyPercent = 15,
                DiscountOvernightPercent = 20,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1),
                IsHeroOffer = false
            }
        };

        db.Promotions.AddRange(promotions);
        await db.SaveChangesAsync();
    }

    private static async Task SeedTestimonialsAsync(HotelDbContext db)
    {
        if (await db.Testimonials.AnyAsync()) return;

        var testimonials = new[]
        {
            new Testimonial { CustomerName = "Trần Minh Hoàng", Content = "Khách sạn có thiết kế đậm chất Việt, nhân viên cực kỳ chuyên nghiệp và thân thiện. Dịch vụ Spa là một điểm sáng không thể bỏ lỡ.", Rating = 5, Source = "TripAdvisor" },
            new Testimonial { CustomerName = "Nguyễn Ngọc Bích", Content = "Bữa sáng buffet có phở bò rất chuẩn vị. Phòng ốc sạch sẽ, view ngắm biển cực chill. Nhất định gia đình tôi sẽ quay lại.", Rating = 5, Source = "Google Maps" },
            new Testimonial { CustomerName = "Lê Thanh Hùng", Content = "Vợ chồng tôi vừa có kỳ nghỉ trăng mật tuyệt vời ở đây. Dịch vụ chu đáo, decor phòng lãng mạn. Rất đáng tiền!", Rating = 5, Source = "Agoda" },
            new Testimonial { CustomerName = "Phạm Thị Mai", Content = "Hồ bơi đẹp nhưng cuối tuần hơi đông. Bù lại thái độ nhân viên rất ân cần, giúp đỡ chúng tôi nhiệt tình khi cần thuê xe máy.", Rating = 4, Source = "Booking.com" },
            new Testimonial { CustomerName = "Hoàng Sơn", Content = "Đã đi nhiều resort nhưng ở đây mang lại cảm giác rất 'nhà'. Đồ ăn Việt Nam nêm nếm vừa miệng, không gian xanh mát.", Rating = 5, Source = "Facebook" },
            new Testimonial { CustomerName = "Đỗ Kim Oanh", Content = "Mình rất ấn tượng với món cà phê muối ở sảnh chờ. Thủ tục check-in nhanh, phòng sạch và không có mùi ẩm.", Rating = 5, Source = "Google Maps" },
            new Testimonial { CustomerName = "Vũ Quang Đại", Content = "Khung cảnh bình yên, cách ly khỏi sự ồn ào của thành phố. Dịch vụ đưa đón sân bay rất đúng giờ và xe xịn.", Rating = 5, Source = "TripAdvisor" },
            new Testimonial { CustomerName = "Bùi Hương Giang", Content = "Phòng gia đình rộng rãi, tiện cho người già và trẻ nhỏ. Sẽ quay lại dịp hè năm sau.", Rating = 5, Source = "Booking.com" },
            new Testimonial { CustomerName = "Ngô Tuấn Anh", Content = "Mọi thứ hoàn hảo từ lúc bước xuống xe đến lúc về. Cảm ơn đội ngũ lễ tân đã hỗ trợ gia đình.", Rating = 5, Source = "Agoda" },
            new Testimonial { CustomerName = "Đinh Thúy Quỳnh", Content = "Resort có nhiều góc check-in sống ảo cực chất. Ánh sáng trong phòng chụp ảnh lên siêu đẹp.", Rating = 4, Source = "Instagram" },
            new Testimonial { CustomerName = "Lý Minh Triết", Content = "Chất lượng hoàn toàn xứng đáng với 5 sao. Không có điểm gì chê được.", Rating = 5, Source = "Google Maps" }
        };

        db.Testimonials.AddRange(testimonials);
        await db.SaveChangesAsync();
    }

    private static async Task SeedGuestsAsync(HotelDbContext db)
    {
        if (await db.Guests.AnyAsync()) return;

        var defaultPasswordHash = PasswordHasher.Hash("123456");

        var guests = new[]
        {
            new Guest
            {
                FullName = "Nguyễn Văn An",
                IdType = GuestIdType.CitizenId,
                IdNumber = "001090001234",
                DateOfBirth = new DateTime(1990, 5, 15),
                Gender = Gender.Male,
                Nationality = "Việt Nam",
                PhoneNumber = "0912345678",
                Email = "an.nguyen@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "12 Phố Hàng Bạc, Hoàn Kiếm, Hà Nội",
                Tier = MemberTier.Diamond,
                RewardPoints = 1500,
                Notes = "Khách hàng VIP, thích phòng hướng biển tầng cao."
            },
            new Guest
            {
                FullName = "Trần Thị Bích",
                IdType = GuestIdType.CitizenId,
                IdNumber = "025192005678",
                DateOfBirth = new DateTime(1992, 10, 20),
                Gender = Gender.Female,
                Nationality = "Việt Nam",
                PhoneNumber = "0987654321",
                Email = "bich.tran@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "45 Nguyễn Văn Linh, Hải Châu, Đà Nẵng",
                Tier = MemberTier.Gold,
                RewardPoints = 850,
                Notes = "Yêu cầu gối lông vũ và trà thảo mộc đón tiếp."
            },
            new Guest
            {
                FullName = "Lê Hoàng Nam",
                IdType = GuestIdType.CitizenId,
                IdNumber = "079088009876",
                DateOfBirth = new DateTime(1988, 3, 12),
                Gender = Gender.Male,
                Nationality = "Việt Nam",
                PhoneNumber = "0905123456",
                Email = "nam.le@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "78 Lê Duẩn, Quận 1, TP. Hồ Chí Minh",
                Tier = MemberTier.Silver,
                RewardPoints = 400,
                Notes = "Thường công tác Đà Nẵng, yêu cầu xuất hóa đơn công ty."
            },
            new Guest
            {
                FullName = "Phạm Minh Đức",
                IdType = GuestIdType.CitizenId,
                IdNumber = "031093004321",
                DateOfBirth = new DateTime(1993, 8, 25),
                Gender = Gender.Male,
                Nationality = "Việt Nam",
                PhoneNumber = "0934567890",
                Email = "duc.pham@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "23 Lương Khánh Thiện, Ngô Quyền, Hải Phòng",
                Tier = MemberTier.Standard,
                RewardPoints = 120
            },
            new Guest
            {
                FullName = "Võ Thị Mai",
                IdType = GuestIdType.CitizenId,
                IdNumber = "048196008765",
                DateOfBirth = new DateTime(1996, 12, 5),
                Gender = Gender.Female,
                Nationality = "Việt Nam",
                PhoneNumber = "0978123456",
                Email = "mai.vo@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "102 Đại lộ Hòa Bình, Ninh Kiều, Cần Thơ",
                Tier = MemberTier.Diamond,
                RewardPoints = 2100,
                Notes = "Hội viên Kim Cương, hay đi du lịch gia đình."
            },
            new Guest
            {
                FullName = "Hoàng Anh Tuấn",
                IdType = GuestIdType.CitizenId,
                IdNumber = "038085002468",
                DateOfBirth = new DateTime(1985, 7, 18),
                Gender = Gender.Male,
                Nationality = "Việt Nam",
                PhoneNumber = "0918273645",
                Email = "tuan.hoang@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "56 Nguyễn Trãi, Thanh Xuân, Hà Nội",
                Tier = MemberTier.Gold,
                RewardPoints = 950
            },
            new Guest
            {
                FullName = "Đặng Thu Thảo",
                IdType = GuestIdType.CitizenId,
                IdNumber = "001198001357",
                DateOfBirth = new DateTime(1998, 1, 30),
                Gender = Gender.Female,
                Nationality = "Việt Nam",
                PhoneNumber = "0965432109",
                Email = "thao.dang@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "89 Kim Mã, Ba Đình, Hà Nội",
                Tier = MemberTier.Silver,
                RewardPoints = 350
            },
            new Guest
            {
                FullName = "Bùi Quốc Bảo",
                IdType = GuestIdType.CitizenId,
                IdNumber = "075091009753",
                DateOfBirth = new DateTime(1991, 11, 14),
                Gender = Gender.Male,
                Nationality = "Việt Nam",
                PhoneNumber = "0943210987",
                Email = "bao.bui@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "15 Phạm Văn Thuận, Biên Hòa, Đồng Nai",
                Tier = MemberTier.Standard,
                RewardPoints = 180
            },
            new Guest
            {
                FullName = "Đoàn Ngọc Linh",
                IdType = GuestIdType.CitizenId,
                IdNumber = "049194008642",
                DateOfBirth = new DateTime(1994, 9, 9),
                Gender = Gender.Female,
                Nationality = "Việt Nam",
                PhoneNumber = "0921098765",
                Email = "linh.doan@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "67 Võ Nguyên Giáp, Ngũ Hành Sơn, Đà Nẵng",
                Tier = MemberTier.Diamond,
                RewardPoints = 1800,
                Notes = "Hay đặt dịch vụ Spa đá nóng."
            },
            new Guest
            {
                FullName = "Ngô Gia Huy",
                IdType = GuestIdType.CitizenId,
                IdNumber = "001097003579",
                DateOfBirth = new DateTime(1997, 4, 22),
                Gender = Gender.Male,
                Nationality = "Việt Nam",
                PhoneNumber = "0989012345",
                Email = "huy.ngo@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "34 Hoàng Quốc Việt, Cầu Giấy, Hà Nội",
                Tier = MemberTier.Gold,
                RewardPoints = 720
            },
            new Guest
            {
                FullName = "Trịnh Khánh Vy",
                IdType = GuestIdType.CitizenId,
                IdNumber = "079199002468",
                DateOfBirth = new DateTime(1999, 6, 11),
                Gender = Gender.Female,
                Nationality = "Việt Nam",
                PhoneNumber = "0938123456",
                Email = "vy.trinh@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "12 Nguyễn Thị Minh Khai, Quận 3, TP. Hồ Chí Minh",
                Tier = MemberTier.Silver,
                RewardPoints = 500
            },
            new Guest
            {
                FullName = "Dương Văn Khánh",
                IdType = GuestIdType.CitizenId,
                IdNumber = "036087001234",
                DateOfBirth = new DateTime(1987, 2, 28),
                Gender = Gender.Male,
                Nationality = "Việt Nam",
                PhoneNumber = "0915987654",
                Email = "khanh.duong@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "88 Trần Hưng Đạo, TP. Nam Định",
                Tier = MemberTier.Standard,
                RewardPoints = 90
            },
            new Guest
            {
                FullName = "Lý Gia Tuệ",
                IdType = GuestIdType.CitizenId,
                IdNumber = "001195004567",
                DateOfBirth = new DateTime(1995, 8, 16),
                Gender = Gender.Female,
                Nationality = "Việt Nam",
                PhoneNumber = "0976543210",
                Email = "tue.ly@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "19 Xuân Diệu, Tây Hồ, Hà Nội",
                Tier = MemberTier.Diamond,
                RewardPoints = 2400,
                Notes = "Khách hàng ưu tiên, đặt xe đón tận sân bay."
            },
            new Guest
            {
                FullName = "Phan Văn Hải",
                IdType = GuestIdType.CitizenId,
                IdNumber = "052089006789",
                DateOfBirth = new DateTime(1989, 10, 4),
                Gender = Gender.Male,
                Nationality = "Việt Nam",
                PhoneNumber = "0903456789",
                Email = "hai.phan@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "54 Trần Phú, Nha Trang, Khánh Hòa",
                Tier = MemberTier.Gold,
                RewardPoints = 680
            },
            new Guest
            {
                FullName = "Vũ Kim Anh",
                IdType = GuestIdType.CitizenId,
                IdNumber = "001197007890",
                DateOfBirth = new DateTime(1997, 3, 8),
                Gender = Gender.Female,
                Nationality = "Việt Nam",
                PhoneNumber = "0961234567",
                Email = "kimanh.vu@gmail.com",
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                Address = "91 Xã Đàn, Đống Đa, Hà Nội",
                Tier = MemberTier.Silver,
                RewardPoints = 420
            }
        };

        await db.Guests.AddRangeAsync(guests);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Nạp dữ liệu giả lập cho Đơn đặt phòng, Lưu trú (tình trạng phòng), Dọn phòng, Hóa đơn (cho báo cáo).
    /// Sinh 15-20 đơn ở các trạng thái khác nhau.
    /// </summary>
    private static async Task SeedReservationsAndStaysAsync(HotelDbContext db)
    {
        if (await db.Reservations.AnyAsync()) return;

        var guests = await db.Guests.Take(15).ToListAsync();
        var rooms = await db.Rooms.Include(r => r.RoomType).ToListAsync();
        var admin = await db.Employees.FirstOrDefaultAsync(e => e.UserName == "admin");
        var receptionist = await db.Employees.FirstOrDefaultAsync(e => e.UserName == "letan");

        if (guests.Count == 0 || rooms.Count == 0 || admin == null || receptionist == null) return;

        var rng = new Random(1234);
        var now = DateTime.Now;

        // Mở 1 ca làm việc cho lễ tân
        var openShift = new CashierShift
        {
            EmployeeId = receptionist.Id,
            OpenedAt = now.Date.AddHours(8),
            Slot = ShiftSlot.Ca1,
            Status = ShiftStatus.Open,
            OpeningCash = 2000000m
        };
        db.CashierShifts.Add(openShift);
        await db.SaveChangesAsync();

        var reservations = new List<Reservation>();

        // 5 đơn trong quá khứ (đã CheckOut) -> Sinh Hóa đơn (Invoices) để có dữ liệu Báo cáo
        for (int i = 0; i < 5; i++)
        {
            var guest = guests[i];
            var room = rooms[i];
            var checkIn = now.Date.AddDays(-10 + i);
            var checkOut = checkIn.AddDays(2);
            
            var res = new Reservation
            {
                Code = $"RSV-{checkIn:yyMMdd}-{1000 + i}",
                PrimaryGuestId = guest.Id,
                RentalType = RentalType.Daily,
                CheckInDate = checkIn.AddHours(14),
                CheckOutDate = checkOut.AddHours(12),
                Nights = 2,
                Status = ReservationStatus.CheckedOut,
                Source = ReservationSource.Phone,
                EstimatedTotal = room.RoomType.BasePricePerNight * 2,
                Rooms = new List<ReservationRoom>
                {
                    new ReservationRoom
                    {
                        RoomTypeId = room.RoomTypeId,
                        RoomId = room.Id,
                        Adults = 2,
                        PricePerNight = room.RoomType.BasePricePerNight,
                        PriceFirstHour = room.RoomType.PriceFirstHour,
                        PriceExtraHour = room.RoomType.PriceExtraHour,
                        PriceOvernight = room.RoomType.PriceOvernight
                    }
                }
            };
            
            var stay = new Stay
            {
                Reservation = res,
                RoomId = room.Id,
                PrimaryGuestId = guest.Id,
                ActualCheckIn = res.CheckInDate.AddMinutes(rng.Next(10, 60)),
                ExpectedCheckOut = res.CheckOutDate,
                ActualCheckOut = res.CheckOutDate.AddMinutes(-rng.Next(10, 120)),
                RentalType = RentalType.Daily,
                PricePerNight = room.RoomType.BasePricePerNight,
                PriceFirstHour = room.RoomType.PriceFirstHour,
                PriceExtraHour = room.RoomType.PriceExtraHour,
                PriceOvernight = room.RoomType.PriceOvernight,
                Nights = 2,
                Status = StayStatus.CheckedOut,
                IsInspected = true,
                InspectedAt = res.CheckOutDate.AddMinutes(-rng.Next(15, 30)),
                InspectedBy = admin.Id,
                Folio = new Folio
                {
                    Code = $"F-{100000 + i}",
                    IsLocked = true,
                    LockedAt = res.CheckOutDate,
                    LockedBy = receptionist.Id,
                    Items = new List<FolioItem>
                    {
                        new FolioItem
                        {
                            ItemType = FolioItemType.Room,
                            Description = "Tiền phòng (2 đêm)",
                            Quantity = 2,
                            UnitPrice = room.RoomType.BasePricePerNight,
                            Amount = room.RoomType.BasePricePerNight * 2,
                            ChargedAt = res.CheckInDate,
                            CreatedBy = receptionist.Id
                        }
                    }
                }
            };
            res.Stays.Add(stay);
            reservations.Add(res);
        }

        // 7 đơn hiện tại (Đang CheckIn) -> Trạng thái phòng Đang ở (Occupied)
        for (int i = 5; i < 12; i++)
        {
            var guest = guests[i];
            var room = rooms[i];
            var checkIn = now.Date.AddDays(-1);
            var checkOut = checkIn.AddDays(3);
            
            var res = new Reservation
            {
                Code = $"RSV-{checkIn:yyMMdd}-{2000 + i}",
                PrimaryGuestId = guest.Id,
                RentalType = RentalType.Daily,
                CheckInDate = checkIn.AddHours(14),
                CheckOutDate = checkOut.AddHours(12),
                Nights = 3,
                Status = ReservationStatus.CheckedIn,
                Source = ReservationSource.WalkIn,
                EstimatedTotal = room.RoomType.BasePricePerNight * 3,
                Rooms = new List<ReservationRoom>
                {
                    new ReservationRoom
                    {
                        RoomTypeId = room.RoomTypeId,
                        RoomId = room.Id,
                        Adults = 2,
                        PricePerNight = room.RoomType.BasePricePerNight,
                        PriceFirstHour = room.RoomType.PriceFirstHour,
                        PriceExtraHour = room.RoomType.PriceExtraHour,
                        PriceOvernight = room.RoomType.PriceOvernight
                    }
                }
            };
            
            var stay = new Stay
            {
                Reservation = res,
                RoomId = room.Id,
                PrimaryGuestId = guest.Id,
                ActualCheckIn = res.CheckInDate.AddMinutes(rng.Next(5, 45)),
                ExpectedCheckOut = res.CheckOutDate,
                RentalType = RentalType.Daily,
                PricePerNight = room.RoomType.BasePricePerNight,
                PriceFirstHour = room.RoomType.PriceFirstHour,
                PriceExtraHour = room.RoomType.PriceExtraHour,
                PriceOvernight = room.RoomType.PriceOvernight,
                Nights = 3,
                Status = StayStatus.CheckedIn,
                Folio = new Folio
                {
                    Code = $"F-{200000 + i}",
                    IsLocked = false,
                    Items = new List<FolioItem>
                    {
                        new FolioItem
                        {
                            ItemType = FolioItemType.Room,
                            Description = "Tiền phòng (tạm tính)",
                            Quantity = 3,
                            UnitPrice = room.RoomType.BasePricePerNight,
                            Amount = room.RoomType.BasePricePerNight * 3,
                            ChargedAt = res.CheckInDate,
                            CreatedBy = receptionist.Id
                        }
                    }
                }
            };
            res.Stays.Add(stay);
            reservations.Add(res);
            
            // Thêm vài tác vụ dọn phòng hàng ngày cho phòng đang có khách (Trạng thái Pending)
            if (i % 2 == 0)
            {
                db.HousekeepingTasks.Add(new HousekeepingTask
                {
                    RoomId = room.Id,
                    Status = HousekeepingTaskStatus.Pending,
                    Notes = "Make up room",
                    Stay = stay
                });
            }
        }

        // 3 đơn tương lai (Đã xác nhận Confirmed) -> Trạng thái phòng Sắp đến (Chờ check-in)
        for (int i = 12; i < 15; i++)
        {
            var guest = guests[i];
            var room = rooms[i];
            var checkIn = now.Date.AddDays(2 + (i - 12));
            var checkOut = checkIn.AddDays(2);
            
            var res = new Reservation
            {
                Code = $"RSV-{checkIn:yyMMdd}-{3000 + i}",
                PrimaryGuestId = guest.Id,
                RentalType = RentalType.Daily,
                CheckInDate = checkIn.AddHours(14),
                CheckOutDate = checkOut.AddHours(12),
                Nights = 2,
                Status = ReservationStatus.Confirmed,
                Source = ReservationSource.Referral,
                EstimatedTotal = room.RoomType.BasePricePerNight * 2,
                Rooms = new List<ReservationRoom>
                {
                    new ReservationRoom
                    {
                        RoomTypeId = room.RoomTypeId,
                        RoomId = room.Id,
                        Adults = 1,
                        PricePerNight = room.RoomType.BasePricePerNight,
                        PriceFirstHour = room.RoomType.PriceFirstHour,
                        PriceExtraHour = room.RoomType.PriceExtraHour,
                        PriceOvernight = room.RoomType.PriceOvernight
                    }
                }
            };
            reservations.Add(res);
        }

        db.Reservations.AddRange(reservations);
        await db.SaveChangesAsync();

        // Tạo Invoices cho các lượt lưu trú đã check-out
        var pastStays = reservations.Where(r => r.Status == ReservationStatus.CheckedOut).SelectMany(r => r.Stays).ToList();
        var invoices = new List<Invoice>();
        int invoiceCounter = 1;
        foreach (var stay in pastStays)
        {
            var roomCharge = stay.Folio!.Items.Where(i => i.ItemType == FolioItemType.Room).Sum(i => i.Amount);
            var subTotal = roomCharge;
            var taxRate = 8m; // 8% VAT
            var taxAmount = subTotal * taxRate / 100m;
            var totalAmount = subTotal + taxAmount;
            
            var invoice = new Invoice
            {
                InvoiceNo = $"HD-{now:yyyyMM}-{invoiceCounter:00000}",
                FolioId = stay.Folio.Id,
                IssuedAt = stay.ActualCheckOut ?? now,
                IssuedBy = receptionist.Id,
                CashierShiftId = openShift.Id,
                RoomCharge = roomCharge,
                ServiceCharge = 0,
                SurchargeAmount = 0,
                DiscountAmount = 0,
                DepositAmount = 0,
                SubTotal = subTotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                AmountPaid = totalAmount,
                DebtAmount = 0,
                Status = InvoiceStatus.Settled,
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        Type = PaymentType.InvoiceSettlement,
                        Method = PaymentMethod.Card,
                        Amount = totalAmount,
                        PaidAt = stay.ActualCheckOut ?? now,
                        CreatedBy = receptionist.Id,
                        TransactionRef = $"VNPay-{now.Ticks}"
                    }
                }
            };
            invoices.Add(invoice);
            invoiceCounter++;
        }
        
        db.Invoices.AddRange(invoices);
        
        // Thêm 5 phòng đang chờ dọn dẹp sau khi khách check-out (Phòng trống, trạng thái InProgress)
        for (int i = 15; i < 20; i++)
        {
             db.HousekeepingTasks.Add(new HousekeepingTask
             {
                 RoomId = rooms[i].Id,
                 Status = HousekeepingTaskStatus.InProgress,
                 Notes = "Dọn phòng trống",
                 StartedAt = now.AddMinutes(-30)
             });
        }

        await db.SaveChangesAsync();
    }
}
