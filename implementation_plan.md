# Kế hoạch Khởi tạo Dự án Quản lý Khách sạn

Kế hoạch này phác thảo các bước để khởi tạo cấu trúc dự án cơ bản cho Hệ thống Quản lý Khách sạn, dựa trên 5 nhóm quy trình cốt lõi mà bạn đã cung cấp.

## Cần Người dùng Phê duyệt

Vui lòng xem xét các công nghệ được đề xuất cho frontend và backend để đảm bảo chúng phù hợp với mong muốn của bạn.

## Câu hỏi Mở

- **Framework Backend:** Tôi đề xuất sử dụng **.NET 8 Web API** cho backend C#. Bạn thấy có hợp lý không?
- **Framework Frontend:** Tôi đề xuất sử dụng **React với Vite**. Bạn muốn sử dụng JavaScript hay TypeScript cho dự án React?
- **Cơ sở dữ liệu (Database):** Bạn có muốn tôi thiết lập Entity Framework Core cho cơ sở dữ liệu trong lần khởi tạo này luôn không?

## Các Thay đổi Đề xuất

Chúng ta sẽ tạo hai thư mục chính `FE` và `BE` ở thư mục gốc của không gian làm việc.

### Backend (BE)
Tôi sẽ chạy lệnh `dotnet new webapi -n HotelManagement -o BE` để tạo dự án C# Web API mới.
Tôi cũng sẽ tạo cấu trúc thư mục cơ bản phản ánh 5 nhóm nghiệp vụ cốt lõi:
- `Controllers/` (Các API Endpoints)
- `Models/` (Các thực thể dữ liệu)
- `Services/` (Logic nghiệp vụ)
  - `Configuration/` (Quản lý Danh mục & Cấu hình Hệ thống)
    - `ConfigurationsController` (GET/POST /api/configurations/...)
  - `Reservation/` (Nghiệp vụ Đặt phòng)
    - `ReservationsController` (GET/POST/PUT /api/reservations/...)
  - `FrontDesk/` (Nghiệp vụ Lễ tân)
    - `FrontDeskController` (GET/POST/PUT /api/frontdesk/...)
  - `Housekeeping/` (Nghiệp vụ Dịch vụ & Buồng phòng)
    - `HousekeepingController` (GET/POST/PUT /api/housekeeping/...)
  - `Billing/` (Nghiệp vụ Thu ngân & Báo cáo Thống kê)
    - `BillingController` (GET/POST /api/billing/...)

### Frontend (FE)
Tôi sẽ chạy lệnh `npx -y create-vite@latest FE --template react` (hoặc `react-ts` nếu bạn dùng TypeScript) để tạo dự án React.
Tôi sẽ thiết lập cấu trúc thư mục cơ bản cho các nhóm nghiệp vụ:
- `src/components/` (Các thành phần dùng chung)
- `src/pages/` (Các trang giao diện)
  - `Configuration/`
  - `Reservation/`
  - `FrontDesk/`
  - `Housekeeping/`
  - `Billing/`

## Kế hoạch Kiểm tra

1. Xác minh rằng dự án `BE` build thành công (sử dụng lệnh `dotnet build`).
2. Xác minh rằng dự án `FE` cài đặt thành công các thư viện (dependencies) (sử dụng lệnh `npm install`).
