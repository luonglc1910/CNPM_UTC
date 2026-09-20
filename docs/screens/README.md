# Danh mục màn hình — Hệ thống Quản lý Khách sạn

Tài liệu này liệt kê **toàn bộ màn hình** của hệ thống, kèm ma trận phân quyền tổng.
Chi tiết nghiệp vụ từng màn hình nằm trong các file con.

> Nguồn gốc yêu cầu: [`../../REQUIREMENTS.md`](../../REQUIREMENTS.md).
> Mỗi màn hình đều tham chiếu ngược về mã `FR-xx` (yêu cầu chức năng) và `BR-xx` (quy tắc nghiệp vụ).

## Cấu trúc tài liệu

| File | Nội dung |
|---|---|
| [`00-conventions.md`](00-conventions.md) | Quy ước chung: layout, menu, phân quyền, thông báo, validate, phân trang |
| [`01-auth-dashboard.md`](01-auth-dashboard.md) | Đăng nhập, đổi mật khẩu, trang 403, Dashboard tổng quan |
| [`02-catalog.md`](02-catalog.md) | Loại phòng, phòng, dịch vụ, tồn kho, nhân viên, cấu hình hệ thống |
| [`03-guests.md`](03-guests.md) | Hồ sơ khách, lịch sử lưu trú, khai báo tạm trú, blacklist |
| [`04-reservations.md`](04-reservations.md) | Tra phòng trống, sơ đồ phòng, tạo/sửa/hủy đơn, thu cọc, no-show |
| [`05-frontdesk.md`](05-frontdesk.md) | Bảng lễ tân, check-in, walk-in, đổi phòng, gia hạn, check-out |
| [`06-housekeeping.md`](06-housekeeping.md) | Bảng buồng phòng, kiểm minibar, báo hỏng, yêu cầu phục vụ |
| [`07-billing.md`](07-billing.md) | Folio, thêm chi phí, giảm giá, thanh toán, hóa đơn, ca làm việc |
| [`08-reports-admin.md`](08-reports-admin.md) | Doanh thu, công suất, ADR/RevPAR, báo cáo ca, nhật ký thao tác |

## Bảng tổng hợp màn hình

Ký hiệu quyền: **A** = Admin, **L** = Lễ tân.
`✔` = vào được đầy đủ, `R` = chỉ xem (read-only), `—` = không vào được (403).

> Hệ thống chỉ có **2 vai trò**. Nhân viên dọn phòng không có tài khoản đăng nhập — nhóm màn hình E
> vẫn giữ nguyên nhưng do lễ tân thao tác thay (xem [`06-housekeeping.md`](06-housekeeping.md)).

### Nhóm S — Hệ thống & Tổng quan

| Mã | Màn hình | URL | A | L |
|---|---|---|:-:|:-:|
| SCR-S01 | Đăng nhập | `/Account/Login` | ✔ | ✔ |
| SCR-S02 | Đổi mật khẩu | `/Account/ChangePassword` | ✔ | ✔ |
| SCR-S03 | Không có quyền (403) | `/Home/Forbidden` | ✔ | ✔ |
| SCR-S04 | Dashboard tổng quan | `/Home/Index` | ✔ | ✔ |

### Nhóm A — Danh mục & Cấu hình

| Mã | Màn hình | URL | A | L |
|---|---|---|:-:|:-:|
| SCR-A01 | Danh sách loại phòng | `/RoomTypes` | ✔ | R |
| SCR-A02 | Thêm / sửa loại phòng | `/RoomTypes/Create`, `/Edit/{id}` | ✔ | — |
| SCR-A03 | Danh sách phòng | `/Rooms` | ✔ | R |
| SCR-A04 | Thêm / sửa phòng | `/Rooms/Create`, `/Edit/{id}` | ✔ | — |
| SCR-A05 | Đổi trạng thái phòng | `/Rooms/UpdateStatus/{id}` | ✔ | ✔ |
| SCR-A06 | Danh sách dịch vụ | `/HotelServices` | ✔ | R |
| SCR-A07 | Thêm / sửa dịch vụ | `/HotelServices/Create`, `/Edit/{id}` | ✔ | — |
| SCR-A08 | Tồn kho | `/Inventory` | ✔ | R |
| SCR-A09 | Nhập kho / điều chỉnh kho | `/Inventory/Receive`, `/Adjust` | ✔ | — |
| SCR-A10 | Danh sách nhân viên | `/Employees` | ✔ | — |
| SCR-A11 | Thêm / sửa nhân viên | `/Employees/Create`, `/Edit/{id}` | ✔ | — |
| SCR-A12 | Cấu hình hệ thống | `/Settings` | ✔ | — |

### Nhóm B — Khách hàng

| Mã | Màn hình | URL | A | L |
|---|---|---|:-:|:-:|
| SCR-B01 | Danh sách khách | `/Guests` | ✔ | ✔ |
| SCR-B02 | Thêm / sửa hồ sơ khách | `/Guests/Create`, `/Edit/{id}` | ✔ | ✔ |
| SCR-B03 | Chi tiết khách & lịch sử lưu trú | `/Guests/Details/{id}` | ✔ | ✔ |
| SCR-B04 | Khai báo tạm trú theo ngày | `/Guests/Residence` | ✔ | ✔ |
| SCR-B05 | Đưa vào / gỡ khỏi blacklist | `/Guests/Blacklist/{id}` | ✔ | — |

### Nhóm C — Đặt phòng

| Mã | Màn hình | URL | A | L |
|---|---|---|:-:|:-:|
| SCR-C01 | Danh sách đơn đặt phòng | `/Reservations` | ✔ | ✔ |
| SCR-C02 | Tra cứu phòng trống | `/Reservations/Availability` | ✔ | ✔ |
| SCR-C03 | Sơ đồ phòng theo ngày | `/Reservations/RoomChart` | ✔ | ✔ |
| SCR-C04 | Tạo đơn đặt phòng | `/Reservations/Create` | ✔ | ✔ |
| SCR-C05 | Chi tiết đơn đặt phòng | `/Reservations/Details/{id}` | ✔ | ✔ |
| SCR-C06 | Sửa đơn đặt phòng | `/Reservations/Edit/{id}` | ✔ | ✔ |
| SCR-C07 | Thu tiền cọc | `/Reservations/Deposit/{id}` | ✔ | ✔ |
| SCR-C08 | Hủy đơn đặt phòng | `/Reservations/Cancel/{id}` | ✔ | ✔ |
| SCR-C09 | Danh sách đơn quá hạn / No-show | `/Reservations/NoShow` | ✔ | ✔ |

### Nhóm D — Lễ tân

| Mã | Màn hình | URL | A | L |
|---|---|---|:-:|:-:|
| SCR-D01 | Bảng điều khiển lễ tân | `/FrontDesk` | ✔ | ✔ |
| SCR-D02 | Check-in khách có đặt trước | `/FrontDesk/CheckIn/{reservationId}` | ✔ | ✔ |
| ~~SCR-D03~~ | ~~Check-in khách vãng lai~~ — **đã bỏ** | — | — | — |
| SCR-D04 | Chi tiết lượt lưu trú | `/FrontDesk/Stay/{id}` | ✔ | ✔ |
| SCR-D05 | Thêm khách vào phòng | `/FrontDesk/AddGuest/{stayId}` | ✔ | ✔ |
| SCR-D06 | Đổi phòng | `/FrontDesk/ChangeRoom/{stayId}` | ✔ | ✔ |
| SCR-D07 | Gia hạn lưu trú | `/FrontDesk/Extend/{stayId}` | ✔ | ✔ |
| SCR-D08 | Check-out | `/FrontDesk/CheckOut/{stayId}` | ✔ | ✔ |

### Nhóm E — Buồng phòng

| Mã | Màn hình | URL | A | L |
|---|---|---|:-:|:-:|
| SCR-E01 | Bảng trạng thái buồng phòng | `/Housekeeping` | ✔ | ✔ |
| SCR-E02 | Kiểm minibar khi trả phòng | `/Housekeeping/MinibarUsage/{stayId}` | ✔ | ✔ |
| SCR-E03 | Tạo yêu cầu (báo hỏng / phục vụ) | `/Housekeeping/CreateRequest` | ✔ | ✔ |
| SCR-E04 | Danh sách yêu cầu | `/Housekeeping/Requests` | ✔ | ✔ |

### Nhóm F — Thu ngân

| Mã | Màn hình | URL | A | L |
|---|---|---|:-:|:-:|
| SCR-F01 | Danh sách folio / hóa đơn | `/Billing` | ✔ | ✔ |
| SCR-F02 | Folio chi tiết | `/Billing/Folio/{stayId}` | ✔ | ✔ |
| SCR-F03 | Thêm chi phí / dịch vụ | `/Billing/AddCharge/{folioId}` | ✔ | ✔ |
| SCR-F04 | Giảm giá | `/Billing/Discount/{folioId}` | ✔ | ✔ (hạn mức) |
| SCR-F05 | Thanh toán | `/Billing/Payment/{folioId}` | ✔ | ✔ |
| SCR-F06 | Hóa đơn (xem / in) | `/Billing/Invoice/{id}` | ✔ | ✔ |
| SCR-F07 | Hủy hóa đơn (Void) | `/Billing/VoidInvoice/{id}` | ✔ | — |
| SCR-F08 | Ca làm việc — mở / đóng ca | `/Shifts` | ✔ | ✔ |
| SCR-F09 | Báo cáo cuối ca | `/Shifts/Report/{id}` | ✔ | ✔ (ca mình) |

### Nhóm G — Báo cáo & Quản trị

| Mã | Màn hình | URL | A | L |
|---|---|---|:-:|:-:|
| SCR-G01 | Báo cáo doanh thu | `/Reports/Revenue` | ✔ | — |
| SCR-G02 | Báo cáo công suất phòng | `/Reports/Occupancy` | ✔ | — |
| SCR-G03 | Báo cáo dịch vụ & tồn kho | `/Reports/Services` | ✔ | — |
| SCR-G04 | Báo cáo theo nhân viên / ca | `/Reports/Staff` | ✔ | — |
| SCR-G05 | Nhật ký thao tác (Audit log) | `/Reports/AuditLog` | ✔ | — |

## Thống kê

- **Tổng số màn hình: 56** (Create và Edit của cùng một danh mục tính là một màn hình vì
  dùng chung form).

| Nhóm | S | A | B | C | D | E | F | G | Tổng |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Số màn hình | 4 | 12 | 5 | 9 | 8 | 4 | 9 | 5 | **56** |

- **Bản chạy được tối thiểu (MVP) — 44 màn hình:** toàn bộ nhóm S, D, E, C; A01–A07;
  B01–B03; F01–F07; G01–G02.
- **Có thể lùi sang giai đoạn sau — 12 màn hình:** SCR-A08, A09 (tồn kho) · A10, A11
  (quản lý nhân viên — giai đoạn đầu seed sẵn tài khoản) · A12 (cấu hình — giai đoạn đầu
  để hằng số trong code) · B04, B05 · F08, F09 (ca làm việc) · G03, G04, G05.
