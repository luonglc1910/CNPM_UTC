# Tổng kết Khởi tạo Dự án Quản lý Khách sạn

Quá trình dựng base dự án đã hoàn tất! Dưới đây là những gì đã được thiết lập:

## Các Thay đổi Đã thực hiện

### Backend (BE) - C# Web API
- Khởi tạo dự án `HotelManagement.API` sử dụng **.NET 8.0**.
- Cấu trúc thư mục được thiết lập để quản lý logic theo 5 nhóm nghiệp vụ cốt lõi:
  - `BE/Services/Configuration` (Quản lý Danh mục & Cấu hình Hệ thống)
    - `GET /api/configurations/room-types` (Lấy danh sách các loại phòng)
    - `POST /api/configurations/room-types` (Tạo mới loại phòng)
    - `GET /api/configurations/rooms` (Lấy danh sách các phòng)
    - `POST /api/configurations/rooms` (Thêm phòng mới)
    - `PUT /api/configurations/rooms/{id}/status` (Cập nhật trạng thái bảo trì/sẵn sàng của phòng)
    - `GET /api/configurations/services` (Lấy danh sách các dịch vụ khách sạn như spa, ăn uống)
    - `POST /api/configurations/services` (Thêm dịch vụ mới)
    - `GET /api/configurations/employees` (Lấy danh sách nhân viên)
    - `POST /api/configurations/employees` (Thêm nhân viên mới)
  - `BE/Services/Reservation` (Nghiệp vụ Đặt phòng)
    - `GET /api/reservations/availability` (Tìm kiếm phòng trống theo ngày và loại phòng)
    - `POST /api/reservations/book` (Tạo đơn đặt phòng mới)
    - `GET /api/reservations/{id}` (Xem chi tiết thông tin đơn đặt phòng)
    - `PUT /api/reservations/{id}` (Chỉnh sửa đơn đặt phòng)
    - `PUT /api/reservations/{id}/cancel` (Hủy đơn đặt phòng)
    - `GET /api/reservations/upcoming` (Lấy danh sách khách sắp nhận phòng)
  - `BE/Services/FrontDesk` (Nghiệp vụ Lễ tân)
    - `POST /api/frontdesk/checkin` (Check-in cho khách đã đặt phòng trước)
    - `POST /api/frontdesk/checkin/walk-in` (Check-in cho khách vãng lai đến thuê trực tiếp)
    - `POST /api/frontdesk/checkout/{stayId}` (Check-out cho khách rời đi)
    - `GET /api/frontdesk/stays/active` (Lấy danh sách các phòng đang có khách lưu trú)
    - `PUT /api/frontdesk/stays/{stayId}/change-room` (Đổi phòng cho khách đang lưu trú)
    - `POST /api/frontdesk/stays/{stayId}/add-guest` (Khai báo thêm khách ở ghép)
  - `BE/Services/Housekeeping` (Nghiệp vụ Dịch vụ & Buồng phòng)
    - `GET /api/housekeeping/rooms/status` (Xem trạng thái dọn dẹp của tất cả các phòng)
    - `PUT /api/housekeeping/rooms/{roomId}/status` (Cập nhật trạng thái dọn dẹp, ví dụ: từ Đang dọn sang Sạch)
    - `POST /api/housekeeping/requests` (Ghi nhận yêu cầu dọn phòng hoặc thêm đồ từ khách)
    - `GET /api/housekeeping/requests/pending` (Lấy danh sách các yêu cầu phục vụ phòng chưa xử lý)
    - `PUT /api/housekeeping/requests/{id}/complete` (Xác nhận đã hoàn thành yêu cầu phục vụ phòng)
    - `POST /api/housekeeping/rooms/{roomId}/minibar-usage` (Ghi nhận khách sử dụng đồ trong minibar để tính tiền)
  - `BE/Services/Billing` (Nghiệp vụ Thu ngân & Báo cáo Thống kê)
    - `GET /api/billing/folios/{stayId}` (Lấy chi tiết bảng kê hóa đơn tạm tính của phòng)
    - `POST /api/billing/folios/{stayId}/add-charge` (Ghi nhận thêm phụ phí hoặc chi phí dịch vụ vào hóa đơn)
    - `POST /api/billing/payments` (Xử lý thanh toán: tiền mặt, quẹt thẻ, chuyển khoản)
    - `POST /api/billing/invoices/generate/{stayId}` (Chốt sổ và xuất hóa đơn cuối cùng)
    - `GET /api/billing/reports/revenue` (Xem báo cáo tổng doanh thu theo thời gian)
    - `GET /api/billing/reports/occupancy` (Xem báo cáo tỷ lệ lấp đầy công suất phòng)

### Frontend (FE) - ReactJS (Vite + TypeScript)
- Khởi tạo dự án ReactJS sử dụng trình đóng gói **Vite** để đảm bảo tốc độ.
- Ngôn ngữ mặc định là **TypeScript** giúp việc quản lý kiểu dữ liệu tĩnh an toàn hơn trong các hệ thống nghiệp vụ phức tạp.
- Đã tạo sẵn các thư mục cơ bản:
  - `FE/src/components/` (chứa các phần tử tái sử dụng)
  - `FE/src/pages/Configuration/`
  - `FE/src/pages/Reservation/`
  - `FE/src/pages/FrontDesk/`
  - `FE/src/pages/Housekeeping/`
  - `FE/src/pages/Billing/`

## Kết quả Kiểm tra
- **Backend:** Lệnh `dotnet build` đã chạy thành công, project compile không có lỗi.
- **Frontend:** Các thư viện (`npm install`) đã được cài đặt và lệnh `npm run build` đã build thành công ứng dụng frontend.

> [!TIP]
> Bạn có thể xem cấu trúc thư mục thực tế tại `FE/` và `BE/` trên cây thư mục bên tay trái của bạn.

Để chạy ứng dụng lúc này, bạn có thể chạy:
- Frontend: `cd FE && npm run dev`
- Backend: `cd BE && dotnet run`

Bước tiếp theo, bạn muốn bắt đầu làm chi tiết vào nghiệp vụ nào trước (ví dụ: tạo giao diện đăng nhập hay thiết kế Database cho phần Quản lý Phòng)?
