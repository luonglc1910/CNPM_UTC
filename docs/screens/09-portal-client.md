# 09 — Cổng thông tin khách hàng / Portal (Nhóm P)

> Nhóm màn hình Front-end phục vụ trực tiếp khách thuê phòng. 
> Khách hàng không cần đăng nhập vẫn có thể tìm và đặt phòng, hệ thống sẽ tự động đối chiếu thông tin qua SĐT/Email để tạo hoặc tái sử dụng hồ sơ khách hàng.

---

## SCR-P01 — Trang chủ & Tìm kiếm phòng

| | |
|---|---|
| **URL** | `GET /Portal/Index` |
| **Quyền** | Công khai (Public) |
| **Yêu cầu** | FR-P01 |

### Mô tả nghiệp vụ
- Khách hàng chọn Ngày nhận phòng (`CheckIn`) và Ngày trả phòng (`CheckOut`).
- Hệ thống lọc và hiển thị danh sách các **Loại phòng (Room Type)** thỏa mãn số lượng phòng còn trống trong giai đoạn đó.
- Công thức kiểm tra: `Tổng số phòng - Số phòng đã có lịch đặt chồng lặp (overlapping reservations) > 0`.
- Hiển thị giá cơ bản và các ưu đãi đang khả dụng.

---

## SCR-P02 — Chi tiết loại phòng & Kiểm tra phòng trống

| | |
|---|---|
| **URL** | `GET /Portal/RoomDetails/{id}` |
| **Quyền** | Công khai (Public) |
| **Yêu cầu** | FR-P02, BR-13, BR-03 |

### Khối chức năng
1. **Thông tin loại phòng**: Hình ảnh, tiện nghi, mô tả, sức chứa (Standard Capacity).
2. **Giao diện đặt phòng tương tác**:
   - Khách có thể tùy chọn **Hình thức thuê (Rental Type)**: Theo ngày, Theo giờ, Qua đêm.
   - Giá phòng tổng cộng thay đổi động (client-side) dựa trên hình thức thuê, số ngày, phụ phí thêm khách và phụ phí giường phụ (Extra bed).

### Quy tắc sàng lọc phòng (Available Room Filtering)
- Danh sách thả xuống "Chọn phòng trống" chỉ hiển thị các phòng vật lý thuộc loại phòng này thỏa mãn **3 điều kiện ngặt nghèo**:
  1. `Room.Status == Available` (Trạng thái chữ trong cơ sở dữ liệu là Trống).
  2. Không có khách nào đang lưu trú thực tế bên trong (`StayStatus.CheckedIn`).
  3. Không có đơn đặt phòng nào khác đã chốt (Confirmed/CheckedIn/Draft) có lịch vướng vào ngày hôm nay.
- Nếu khách không chọn phòng cụ thể, hệ thống gán `RoomId = null` (chờ Lễ tân xếp phòng lúc Check-in).

---

## SCR-P03 — Điền thông tin Đặt phòng & Thanh toán

| | |
|---|---|
| **URL** | `GET/POST /Portal/BookRoom` |
| **Quyền** | Công khai (Public) |
| **Yêu cầu** | FR-P03, BR-02 |

### Nhập liệu
- Các thông số đặt phòng được truyền ngầm từ SCR-P02 (ngày giờ, hình thức thuê, số khách...).
- Khách bắt buộc nhập: **Họ và tên, Số điện thoại, Email, Hình thức thanh toán, Thời gian nhận phòng dự kiến**.

### Xử lý giao dịch (Transaction)
Khi khách nhấn "Hoàn tất Đặt phòng", hệ thống thực hiện đồng bộ:
1. **Kiểm tra/Tạo Hồ sơ Khách (Guest)**: Dò tìm khách trong database bằng Số điện thoại hoặc Email. Nếu chưa có, tạo mới với `IdNumber` tự sinh (Ví dụ: `W-[Ticks]`).
2. **Tạo Đơn (Reservation)**: Khởi tạo đơn hàng dạng `Draft` (Chờ xử lý). Gán nguồn đơn `ReservationSource.Other`. 
3. **Tạo Dòng chi tiết (ReservationRoom)**: Gán giá phòng và các biểu giá phụ chốt chặt tại thời điểm đặt (BR-02).
4. **Khóa phòng (Giữ chỗ)**: Nếu khách chọn một phòng cụ thể (ví dụ 202), cập nhật trạng thái vật lý của phòng thành `Reserved` (Đã đặt) để tránh overbooking.

---

## SCR-P04 — Thông báo Đặt phòng thành công

| | |
|---|---|
| **URL** | `GET /Portal/BookingSuccess/{id}` |
| **Quyền** | Công khai (Public) |
| **Yêu cầu** | FR-P04 |

### Hiển thị
- Khẳng định giao dịch thành công.
- Cấp **Mã đơn đặt phòng (Reservation Code)** (Ví dụ: RSV-261004-9999).
- Tóm tắt chi tiết: Thời gian Check-in/Check-out, Danh sách các phòng đã giữ (Tên loại phòng + Số phòng cụ thể).

Lúc này đơn hàng đã được đẩy thẳng về Bảng điều khiển Lễ tân (SCR-D01) để nhân viên khách sạn tiếp tục liên hệ và xử lý Check-in.
