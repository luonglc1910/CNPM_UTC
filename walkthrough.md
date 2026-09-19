# Hệ thống Quản lý Khách sạn - ASP.NET Core MVC

Dự án đã chuyển từ mô hình **Web API (BE) + React (FE)** sang **ASP.NET Core MVC (.NET 10)**.
Hệ thống chỉ dành cho **Admin / nhân viên khách sạn**, không có cổng cho khách hàng tự đặt phòng:
đơn đặt phòng do Admin/lễ tân tạo thay cho khách.

## Cấu trúc

Project nằm trực tiếp ở thư mục gốc của repo:

```
HotelManagement.slnx
HotelManagement.Web.csproj
Program.cs
appsettings.json
Controllers/
├── AdminControllerBase.cs      # Lớp cơ sở, Pending() dùng tạm cho action chưa làm
├── HomeController.cs           # Tổng quan (Dashboard)
├── AccountController.cs        # Đăng nhập / đăng xuất Admin
├── RoomTypesController.cs      # Danh mục: loại phòng
├── RoomsController.cs          # Danh mục: phòng
├── HotelServicesController.cs  # Danh mục: dịch vụ
├── EmployeesController.cs      # Danh mục: nhân viên
├── ReservationsController.cs   # Đặt phòng
├── FrontDeskController.cs      # Lễ tân
├── HousekeepingController.cs   # Buồng phòng
├── BillingController.cs        # Thu ngân
└── ReportsController.cs        # Báo cáo
Data/HotelDbContext.cs          # EF Core DbContext (SQL Server), chưa có entity
Models/
Views/                          # Mỗi controller một thư mục view
└── Shared/_Layout, _Sidebar, _Placeholder
wwwroot/
```

## Chức năng theo nhóm nghiệp vụ

| Nhóm | URL | Mô tả |
|---|---|---|
| Danh mục & Cấu hình | `/RoomTypes`, `/Rooms`, `/HotelServices`, `/Employees` | Index / Create / Edit; `Rooms/UpdateStatus` (sẵn sàng / bảo trì) |
| Đặt phòng | `/Reservations`, `/Reservations/Availability` | Danh sách, tra cứu phòng trống, Create / Details / Edit / Cancel |
| Lễ tân | `/FrontDesk` | Khách đang lưu trú, CheckIn, WalkIn, CheckOut, ChangeRoom, AddGuest |
| Buồng phòng | `/Housekeeping`, `/Housekeeping/Requests` | Trạng thái dọn phòng, yêu cầu phục vụ, CompleteRequest, MinibarUsage |
| Thu ngân | `/Billing` | Folio, AddCharge, Payment, GenerateInvoice |
| Báo cáo | `/Reports/Revenue`, `/Reports/Occupancy` | Doanh thu, công suất phòng |

## Trạng thái hiện tại: mới dựng khung

- Tất cả các trang đều mở được, nhưng mới là placeholder: GET trả về view rỗng,
  POST chỉ hiện thông báo "Chức năng này chưa được triển khai" rồi chuyển trang.
- Đã cấu hình EF Core SQL Server (`ConnectionStrings:HotelDb` trong `appsettings.json`,
  mặc định là LocalDB) nhưng chưa có entity và migration.
- Đã cấu hình cookie authentication nhưng **chưa bắt buộc đăng nhập**; trang đăng nhập mới có giao diện.

## Chạy ứng dụng

Tại thư mục gốc của repo:

```bash
dotnet run
```

Mở http://localhost:5265.

## Bước tiếp theo

1. Thiết kế entity trong `Models/` và thêm `DbSet` vào `HotelDbContext`.
2. Cài SQL Server / LocalDB, sửa connection string nếu cần, rồi chạy:
   `dotnet ef migrations add InitialCreate` và `dotnet ef database update`.
3. Làm đăng nhập Admin, sau đó bật `AuthorizeFilter` toàn cục trong `Program.cs`.
4. Triển khai lần lượt nghiệp vụ cho từng nhóm (thay `Pending(...)` bằng logic thật).
