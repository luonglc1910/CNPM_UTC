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
            // Cập nhật luôn các trường giao diện (Slug, DetailDescription, ImageUrl, ViewType)
            await db.RoomTypes.Where(t => t.Code == "STD")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.PriceFirstHour, t => t.PriceFirstHour == 0 ? 120_000m : t.PriceFirstHour)
                    .SetProperty(t => t.PriceExtraHour, t => t.PriceExtraHour == 0 ? 20_000m : t.PriceExtraHour)
                    .SetProperty(t => t.PriceOvernight, t => t.PriceOvernight == 0 ? 350_000m : t.PriceOvernight)
                    .SetProperty(t => t.Slug, "standard-room")
                    .SetProperty(t => t.DetailDescription, "<p>Phòng nghỉ tiêu chuẩn với không gian ấm cúng, thiết kế hiện đại, đầy đủ tiện nghi cơ bản mang lại sự thoải mái nhất.</p>")
                    .SetProperty(t => t.ImageUrl, "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=800&auto=format&fit=crop")
                    .SetProperty(t => t.ViewType, "Hướng phố"));
                    
            await db.RoomTypes.Where(t => t.Code == "DLX")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.PriceFirstHour, t => t.PriceFirstHour == 0 ? 200_000m : t.PriceFirstHour)
                    .SetProperty(t => t.PriceExtraHour, t => t.PriceExtraHour == 0 ? 40_000m : t.PriceExtraHour)
                    .SetProperty(t => t.PriceOvernight, t => t.PriceOvernight == 0 ? 600_000m : t.PriceOvernight)
                    .SetProperty(t => t.Slug, "deluxe-room")
                    .SetProperty(t => t.DetailDescription, "<p>Tận hưởng không gian sang trọng và thoáng đãng với diện tích rộng rãi. Phòng tắm được trang bị bồn tắm nằm thư giãn.</p>")
                    .SetProperty(t => t.ImageUrl, "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?q=80&w=800&auto=format&fit=crop")
                    .SetProperty(t => t.ViewType, "Hướng biển"));
                    
            await db.RoomTypes.Where(t => t.Code == "VIP")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.PriceFirstHour, t => t.PriceFirstHour == 0 ? 300_000m : t.PriceFirstHour)
                    .SetProperty(t => t.PriceExtraHour, t => t.PriceExtraHour == 0 ? 50_000m : t.PriceExtraHour)
                    .SetProperty(t => t.PriceOvernight, t => t.PriceOvernight == 0 ? 1_200_000m : t.PriceOvernight)
                    .SetProperty(t => t.Slug, "vip-suite")
                    .SetProperty(t => t.DetailDescription, "<p>Trải nghiệm đẳng cấp thượng lưu với hạng phòng VIP Suite. Thiết kế phong cách tổng thống, có phòng khách và ban công riêng biệt ôm trọn tầm nhìn ra đại dương bao la.</p>")
                    .SetProperty(t => t.ImageUrl, "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=800&auto=format&fit=crop")
                    .SetProperty(t => t.ViewType, "Hướng toàn cảnh Panorama"));
            return;
        }


        var standard = new RoomType
        {
            Code = "STD",
            Name = "Standard",
            Slug = "standard-room",
            StandardCapacity = 2,
            MaxCapacity = 3,
            BasePricePerNight = 500_000m,
            ExtraGuestFeePerNight = 150_000m,
            ExtraBedFeePerNight = 200_000m,
            PriceFirstHour = 120_000m,
            PriceExtraHour = 20_000m,
            PriceOvernight = 350_000m,
            Amenities = "Điều hòa, TV, Nóng lạnh, Wifi",
            Description = "Phòng tiêu chuẩn 2 khách",
            DetailDescription = "<p>Phòng nghỉ tiêu chuẩn với không gian ấm cúng, thiết kế hiện đại, đầy đủ tiện nghi cơ bản mang lại sự thoải mái nhất.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=800&auto=format&fit=crop",
            ViewType = "Hướng phố"
        };

        var deluxe = new RoomType
        {
            Code = "DLX",
            Name = "Deluxe",
            Slug = "deluxe-room",
            StandardCapacity = 2,
            MaxCapacity = 4,
            BasePricePerNight = 900_000m,
            ExtraGuestFeePerNight = 200_000m,
            ExtraBedFeePerNight = 250_000m,
            PriceFirstHour = 200_000m,
            PriceExtraHour = 40_000m,
            PriceOvernight = 600_000m,
            Amenities = "Điều hòa, TV, Nóng lạnh, Wifi, Minibar, Bồn tắm",
            Description = "Phòng rộng, có minibar",
            DetailDescription = "<p>Tận hưởng không gian sang trọng và thoáng đãng với diện tích rộng rãi. Phòng tắm được trang bị bồn tắm nằm thư giãn.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?q=80&w=800&auto=format&fit=crop",
            ViewType = "Hướng biển"
        };

        var vip = new RoomType
        {
            Code = "VIP",
            Name = "VIP Suite",
            Slug = "vip-suite",
            StandardCapacity = 2,
            MaxCapacity = 5,
            BasePricePerNight = 1_800_000m,
            ExtraGuestFeePerNight = 300_000m,
            ExtraBedFeePerNight = 350_000m,
            PriceFirstHour = 300_000m,
            PriceExtraHour = 50_000m,
            PriceOvernight = 1_200_000m,
            Amenities = "Điều hòa, Smart TV, Wifi, Minibar, Bồn tắm, Phòng khách riêng, Ban công",
            Description = "Hạng phòng cao cấp nhất",
            DetailDescription = "<p>Trải nghiệm đẳng cấp thượng lưu với hạng phòng VIP Suite. Thiết kế phong cách tổng thống, có phòng khách và ban công riêng biệt ôm trọn tầm nhìn ra đại dương bao la.</p>",
            ImageUrl = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=800&auto=format&fit=crop",
            ViewType = "Hướng toàn cảnh Panorama"
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
                Title = "Nghỉ Dưỡng Thảnh Thơi",
                Description = "Dành riêng cho kỳ nghỉ từ 3 đêm trở lên tại các hạng phòng hướng biển hoặc biệt thự riêng tư. Bao gồm bữa sáng buffet 5 sao hàng ngày và ưu đãi 20% dịch vụ Spa & Ẩm thực.",
                ImageUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?q=80&w=800&auto=format&fit=crop",
                DisplayPrice = 2500000m,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(2),
                IsHeroOffer = true
            },
            new Promotion
            {
                Title = "Trăng Mật Lãng Mạn",
                Description = "Gói trăng mật ngọt ngào dành cho các cặp đôi. Tặng ngay rượu vang, hoa hồng, bánh kem và 01 bữa tối lãng mạn dưới ánh nến tại nhà hàng The Brass Lantern.",
                ImageUrl = "https://images.unsplash.com/photo-1517400508447-f8dd518b86db?q=80&w=800&auto=format&fit=crop",
                DisplayPrice = 4500000m,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(6),
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
            new Testimonial
            {
                CustomerName = "Trần Minh Hoàng",
                Content = "Một kỳ nghỉ tuyệt vời! Khách sạn có thiết kế sang trọng, nhân viên cực kỳ chuyên nghiệp và thân thiện. Dịch vụ Spa là một điểm sáng không thể bỏ lỡ.",
                Rating = 5,
                Source = "TripAdvisor"
            },
            new Testimonial
            {
                CustomerName = "Nguyễn Ngọc Bích",
                Content = "Bữa sáng buffet rất đa dạng và ngon miệng. Phòng ốc sạch sẽ, view ngắm biển hoàng hôn cực chill. Nhất định gia đình tôi sẽ quay lại.",
                Rating = 5,
                Source = "Google Maps"
            }
        };

        db.Testimonials.AddRange(testimonials);
        await db.SaveChangesAsync();
    }
}
