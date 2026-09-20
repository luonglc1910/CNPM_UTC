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
                MustChangePassword = true
            },
            new Employee
            {
                Code = "NV002",
                FullName = "Nguyễn Thị Lễ Tân",
                PhoneNumber = "0900000002",
                UserName = "letan",
                Role = EmployeeRole.Receptionist,
                PasswordHash = PasswordHasher.Hash("123456"),
                MustChangePassword = true
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

        db.HotelServices.AddRange(services);
        await db.SaveChangesAsync();
    }
}
