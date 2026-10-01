# Quản Lý Khách Sạn (Hotel Management) - ASP.NET Core MVC

Đây là hệ thống quản lý khách sạn được xây dựng bằng kiến trúc ASP.NET Core MVC (Tích hợp cả giao diện và xử lý Backend).

## Yêu cầu hệ thống (Prerequisites)

1. **.NET SDK**: Phiên bản 10.0 trở lên.
2. **Hệ quản trị CSDL**: SQL Server.
3. **IDE**: Visual Studio 2022, JetBrains Rider hoặc Visual Studio Code.
4. **Công cụ dòng lệnh**: Cài đặt .NET EF CLI (nếu cần quản lý database bằng dòng lệnh):
   ```bash
   dotnet tool install --global dotnet-ef
   ```

## Cấu hình (Configuration)

Cấu hình chuỗi kết nối cơ sở dữ liệu nằm trong file `appsettings.json`:

```json
"ConnectionStrings": {
    "HotelDb": "Server=127.0.0.1,1433;Database=AppDb;User Id=sa;Password=123456;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

- Mặc định, dự án sẽ kết nối tới SQL Server tại Localhost (`127.0.0.1,1433`).
- Bạn có thể đổi `Server`, `Database`, `User Id` và `Password` theo SQL Server cài đặt trên máy của bạn.

## Hướng dẫn các bước chạy dự án

### Bước 1: Khôi phục các gói thư viện (Restore Packages)

Mở terminal tại thư mục gốc của dự án (`d:\UTC\CôngNghệPhầnMềm\CNPM_UTC`) và chạy lệnh:

```bash
dotnet restore
```

### Bước 2: Cập nhật Cơ Sở Dữ Liệu (Database Migration)

Dự án sử dụng Entity Framework Core. Trước khi chạy, bạn cần tạo CSDL từ file Migrations đã có sẵn. Chạy câu lệnh sau:

```bash
dotnet ef database update
```

*(Lưu ý: Đảm bảo SQL Server của bạn đang hoạt động và cấu hình connection string ở file `appsettings.json` là chính xác trước khi chạy lệnh này).*

### Bước 3: Build và Chạy Dự Án

Có 2 cách để chạy:

**Cách 1: Chạy bình thường (dành cho môi trường production hoặc test)**
```bash
dotnet run
```

**Cách 2: Chạy với Hot Reload (dành cho lúc dev, code tự động cập nhật lại khi bạn save file)**
```bash
dotnet watch run
```

### Bước 4: Truy cập ứng dụng

Sau khi khởi chạy thành công, Terminal sẽ hiển thị đường link của ứng dụng (Thường là `http://localhost:5000` hoặc `https://localhost:5001`). 

Mở đường dẫn này trên trình duyệt (Chrome/Edge/Firefox) để sử dụng hệ thống.
