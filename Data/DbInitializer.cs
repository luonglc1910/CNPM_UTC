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
        if (await db.RoomTypes.AnyAsync())
        {
            // Backfill giá giờ/qua đêm nếu = 0 — xảy ra khi migration thêm cột sau khi DB đã có data.
            // ExecuteUpdateAsync chỉ đụng đúng cột cần fix, không xáo trộn data khác.
            await db.RoomTypes.Where(t => t.Code == "STD" && t.PriceFirstHour == 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.PriceFirstHour,  120_000m)
                    .SetProperty(t => t.PriceExtraHour,   20_000m)
                    .SetProperty(t => t.PriceOvernight,  350_000m));
            await db.RoomTypes.Where(t => t.Code == "DLX" && t.PriceFirstHour == 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.PriceFirstHour,  200_000m)
                    .SetProperty(t => t.PriceExtraHour,   40_000m)
                    .SetProperty(t => t.PriceOvernight,  600_000m));
            await db.RoomTypes.Where(t => t.Code == "VIP" && t.PriceFirstHour == 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.PriceFirstHour,  300_000m)
                    .SetProperty(t => t.PriceExtraHour,   50_000m)
                    .SetProperty(t => t.PriceOvernight, 1_200_000m));
            return;
        }


        var standard = new RoomType
        {
            Code = "STD",
            Name = "Standard",
            StandardCapacity = 2,
            MaxCapacity = 3,
            BasePricePerNight = 500_000m,
            ExtraGuestFeePerNight = 150_000m,
            ExtraBedFeePerNight = 200_000m,
            PriceFirstHour = 120_000m,
            PriceExtraHour = 20_000m,
            PriceOvernight = 350_000m,
            Amenities = "Điều hòa, TV, Nóng lạnh, Wifi",
            Description = "Phòng tiêu chuẩn 2 khách"
        };

        var deluxe = new RoomType
        {
            Code = "DLX",
            Name = "Deluxe",
            StandardCapacity = 2,
            MaxCapacity = 4,
            BasePricePerNight = 900_000m,
            ExtraGuestFeePerNight = 200_000m,
            ExtraBedFeePerNight = 250_000m,
            PriceFirstHour = 200_000m,
            PriceExtraHour = 40_000m,
            PriceOvernight = 600_000m,
            Amenities = "Điều hòa, TV, Nóng lạnh, Wifi, Minibar, Bồn tắm",
            Description = "Phòng rộng, có minibar"
        };

        var vip = new RoomType
        {
            Code = "VIP",
            Name = "VIP Suite",
            StandardCapacity = 2,
            MaxCapacity = 5,
            BasePricePerNight = 1_800_000m,
            ExtraGuestFeePerNight = 300_000m,
            ExtraBedFeePerNight = 350_000m,
            PriceFirstHour = 300_000m,
            PriceExtraHour = 50_000m,
            PriceOvernight = 1_200_000m,
            Amenities = "Điều hòa, Smart TV, Wifi, Minibar, Bồn tắm, Phòng khách riêng, Ban công",
            Description = "Hạng phòng cao cấp nhất"
        };

        db.RoomTypes.AddRange(standard, deluxe, vip);
        await db.SaveChangesAsync();

        var rooms = new List<Room>();

        // Tầng 1-2: Standard (101-110, 201-210), tầng 3: Deluxe (301-306) và VIP (307-310).
        for (var i = 1; i <= 10; i++)
        {
            rooms.Add(new Room { RoomNumber = $"1{i:00}", Floor = 1, RoomTypeId = standard.Id });
            rooms.Add(new Room { RoomNumber = $"2{i:00}", Floor = 2, RoomTypeId = standard.Id });
        }

        for (var i = 1; i <= 6; i++)
        {
            rooms.Add(new Room { RoomNumber = $"3{i:00}", Floor = 3, RoomTypeId = deluxe.Id });
        }

        for (var i = 7; i <= 10; i++)
        {
            rooms.Add(new Room { RoomNumber = $"3{i:00}", Floor = 3, RoomTypeId = vip.Id });
        }

        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();
    }

    /// <summary>Danh mục dịch vụ mẫu, gồm cả hàng minibar có quản lý kho.</summary>
    private static async Task SeedServicesAsync(HotelDbContext db)
    {
        if (await db.HotelServices.AnyAsync())
        {
            return;
        }

        var services = new[]
        {
            new HotelService { Code = "MB001", Name = "Nước suối 500ml", Category = ServiceCategory.Minibar, UnitPrice = 15_000m, Unit = "chai", IsStockManaged = true, StockQuantity = 200, MinStockLevel = 30 },
            new HotelService { Code = "MB002", Name = "Bia Heineken", Category = ServiceCategory.Minibar, UnitPrice = 35_000m, Unit = "lon", IsStockManaged = true, StockQuantity = 120, MinStockLevel = 24 },
            new HotelService { Code = "MB003", Name = "Coca-Cola", Category = ServiceCategory.Minibar, UnitPrice = 20_000m, Unit = "lon", IsStockManaged = true, StockQuantity = 150, MinStockLevel = 24 },
            new HotelService { Code = "MB004", Name = "Snack khoai tây", Category = ServiceCategory.Minibar, UnitPrice = 25_000m, Unit = "gói", IsStockManaged = true, StockQuantity = 80, MinStockLevel = 20 },
            new HotelService { Code = "FB001", Name = "Bữa sáng buffet", Category = ServiceCategory.FoodAndBeverage, UnitPrice = 120_000m, Unit = "suất" },
            new HotelService { Code = "FB002", Name = "Cơm phần tại phòng", Category = ServiceCategory.FoodAndBeverage, UnitPrice = 90_000m, Unit = "suất" },
            new HotelService { Code = "LD001", Name = "Giặt ủi thường", Category = ServiceCategory.Laundry, UnitPrice = 40_000m, Unit = "kg" },
            new HotelService { Code = "LD002", Name = "Giặt ủi lấy nhanh", Category = ServiceCategory.Laundry, UnitPrice = 70_000m, Unit = "kg" },
            new HotelService { Code = "TR001", Name = "Đưa đón sân bay", Category = ServiceCategory.Transport, UnitPrice = 350_000m, Unit = "lượt" },
            new HotelService { Code = "TR002", Name = "Thuê xe máy", Category = ServiceCategory.Transport, UnitPrice = 150_000m, Unit = "ngày" }
        };

        await db.HotelServices.AddRangeAsync(services);
        await db.SaveChangesAsync();
    }

    private static async Task SeedGalleryAsync(HotelDbContext db)
    {
        if (await db.HotelGalleryImages.AnyAsync())
        {
            return;
        }

        var images = new[]
        {
            new HotelGalleryImage { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuDPF3028cg_lvvBgtzoThJwfJWRo0qj03pC0pEKWr5p__6o3srO4Wf1sGpFkVUPAcwCffODcrUSWZjYKKAuH0oLwCFTq8tmtEBe7EWyAR_Tv9TENC63mm_nmAIRbnJ4p_CvfBswksbXkf2yHHSo72m-KB-4v6UMjsoBj9NMwJNmRMGzlKzpJb3d1dGpxuqtU-UOIswaZPfkVBXfDsRoHNvplFhqwrqdiWuA5NXxp-73DtNE4ClzIw", Title = "Toàn cảnh khu nghỉ dưỡng", SortOrder = 1 },
            new HotelGalleryImage { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBG8SGMAv_h2X_H8t4D47Axe2w9GdmTe0IPFTvt_7XU0-WGGnbQ4oXPaQyT0KQJeJDlgFvTJinSRkUrozh3MO343guQk-ASWqUSQQIS0EBNmPYancHaBW3VMuRNLkpIQ4Xr7r0fI2iFyk076kuxwRtPVoOQAK6HGsGgFPWGAmlIWEUn733cawgVVwcyVbErnLs9zKXZp9VDtM6R5KvwCT3l8B82_6DPmz4ZBXxfMVg2iNPIxL2GEA", Title = "Skyline Cocktail Lounge", SortOrder = 2 },
            new HotelGalleryImage { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuA1Ue-zaIWw8ItsnwKnOVH4N-N8YUcfPlk5rRVnOyzrze1P6Ocq_c7WRd6m5o_U9esV6MLxOFBjVmDIqD9174VZ2Ca9yb5otUo9W6XsyAIsIO-5m6rzyAoDbjl-VivCItGO93WC_7sXYUVVgpIDjqAHJ0dogBb4zPZG_op6iYwkqXYtVwyMRnibjVSfJea8jy-twfODCmajDm9qX9mwMxaeGGy-SzXtClgw7jWYIgIEEasNYKjcEg", Title = "Phòng trị liệu Lotus Sanctuary", SortOrder = 3 },
            new HotelGalleryImage { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuC5V3C0xNj0g4gBR44A6fn7FwCG3P7uR9Jj5XufnEqC9nmmhNWgVhmwWb4iEeNNbXL6Nvk_4Ak45INhTB0WFQU5ApvlpkVhay5KsijWL1h3vcyDHWHCScEcshC2XbMhftQQKCj4AXQgiLQrDmnJwMXZN1cFW_jUHPGNtercJh4bn-opE1Qi5RHhl9IxBvh9jDOuK-We9XJSAdLtB72RN_3gxBjjnSOjlATtwTHTIKyrQ8Vo8bBO1g", Title = "Chi tiết thủ công tinh xảo", SortOrder = 4 },
            new HotelGalleryImage { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuDydPQk8YZnmkTK4wZ0jcIPz7givIrKPBFraa-1zEXfyZ8oS7QbUZWgZmECMm2bJVd_X3plzBHCAH6BoxX1FDc4310n09qZjbHrnTgjjzHPSPgO0AsdWWHVxzpl6DoqcdwYPFpja7m7qWJQQx6ovcDhY5K94PGI6E7v3vjJ6mJfg-mwRKEvHSUs7btgaMtkAHTX6KHw3dAdVGMB2gyN7hgV5hPPSJkVW5x08AxX3v8hZzbMJPdvbA", Title = "Ẩm thực The Brass Lantern", SortOrder = 5 },
            new HotelGalleryImage { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuB5LTj-561bDrykcmgbSJrnaoYacqL1PDzW5vhF2MAT6xP5vwfCGufjsCkpnrkVrLUBqUn9veffnkns3dRdWWxi1r-SERmlC7K9NUXkZFRSIY1wZpV26asyoW336DGUIT05MtHLUIoHtI0JpUYJuToZJU7o39P44egSrst0a0JchukLaamECFTMrhYAYJtXMMjcsHELXBdZyLA5yeH7UR1zySfmzhtlB4xsDdjDrlh1xNkDpJrsFA", Title = "Sảnh đón tiếp sang trọng", SortOrder = 6 },
            new HotelGalleryImage { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuB2GPkdHunTSWkaEE4tQZalRBlOJHqtIRvSuh2JNrTuWlP7xh_es0ZZIhVJpiHY_0t3sFBxzuKHlfRlRYshjlog_6YXgCJUZchQbUsiWQroIt95iguU0PFabk-8pkp4j4YoIoS9Hczgq0Phez3xFlnest21wzGcClDwnEC9JKU5jn9MRXu1osGJXZ67UxcqTygpC2yDS2Yy1xQ5eqrqzCctYuieITUPbsiIPsgN79yG8ceiC4TZng", Title = "Bể bơi vô cực hoàng hôn", SortOrder = 7 },
            new HotelGalleryImage { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCO9DjhF6ltLD3HaQJCQWwn9kCybwBzVTnvWEFCyLjY4X0jcz5Ra4kZNwd5QVLiNNJ0DT6CxLDk7aOqa7hkyRWYDyfgjUmTgnF8Ugb95vLhfBqCbk-IY83WX8tDn7__OWeFJTPBem5vOPiyMoJEW0YxBmRHxbD6RQd2MQRCPWAg-Mx2XyTVzrQGQDgacJcW8mpLTor9gr8rT1cF545cwZm8IjM-D9CQf4sTN9v12w3W1Bw_dPaCOA", Title = "Không gian phòng nghỉ cao cấp", SortOrder = 8 }
        };

        await db.HotelGalleryImages.AddRangeAsync(images);
        await db.SaveChangesAsync();
    }
}
