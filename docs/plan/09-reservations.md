# 09 — Đặt phòng (SP1 — nhóm C)

| | |
|---|---|
| **Trạng thái** | ⏳ Đã viết code, chưa build/verify (máy dev chưa có .NET SDK) |
| **Cập nhật** | 20/09/2026 |
| **Màn hình** | SCR-C01, C02, C04, C05, C06, C07, C08, C09 (MVP; C03 sơ đồ & C10 in phiếu để sau) |
| **Yêu cầu** | FR-C01…C09, BR-02, BR-05, BR-06, BR-10, BR-11 |

## 1. Đã làm gì

Luồng đặt phòng đầy đủ, dựng trên hạ tầng SP0:

| Màn | URL | Nội dung |
|---|---|---|
| C01 | `/Reservations` | Danh sách + lọc (ngày đến, trạng thái, nguồn, từ khóa); mặc định "việc cần xử lý" (Confirmed, đến từ hôm nay) |
| C02 | `/Reservations/Availability` | Tra phòng trống theo ngày/loại/số khách (dùng `IAvailabilityService`), nhóm theo loại, nút "Chọn đặt" |
| C04 | `/Reservations/Create` | Tạo đơn nhiều phòng (JS dòng động), chống trùng BR-06, sinh mã `RSV-`, Lưu nháp / Lưu và xác nhận |
| C05 | `/Reservations/Details/{id}` | Chi tiết + tiền + lịch sử cọc + nút theo trạng thái |
| C06 | `/Reservations/Edit/{id}` | Sửa đơn Draft/Confirmed, kiểm trùng lại |
| C07 | `/Reservations/Deposit/{id}` | Thu cọc — gate ca mở (BR-10), tạo Deposit + Payment |
| C08 | `/Reservations/Cancel/{id}` | Hủy + tính phí hủy BR-05 + hoàn cọc; miễn phí hủy chỉ Admin |
| C09 | `/Reservations/NoShow` | Đơn quá hạn giữ chỗ; đánh dấu No-show / gia hạn giữ / check-in muộn |

**File:** `Services/ReservationService.cs`, `Controllers/ReservationsController.cs`,
`Models/ViewModels/ReservationViewModels.cs`, `Views/Reservations/*`
(`_Form.cshtml` + `_RoomLinesScript.cshtml` dùng chung Create/Edit), EnumDisplay
(ReservationStatus, ReservationSource), DI trong `Program.cs`, menu No-show trong `_Sidebar`.

## 2. Quyết định thiết kế

- **Form nhiều phòng:** dòng động bằng JS vanilla (`Rooms[i].*`, reindex khi thêm/xóa). Giá/đêm tự
  điền theo loại; chỉ Admin sửa tay (lễ tân dùng giá niêm yết) — chốt cả ở server (`PrepareRoomsAsync`).
- **Chống trùng ở server:** mọi dòng có RoomId đều `IsRoomAvailableAsync` lúc lưu, không tin giao diện;
  chặn chọn cùng phòng hai dòng.
- **Cọc:** Deposit + Payment gắn ca; thu cọc xóa `HoldUntil` (thoát diện hết hạn). Mã đơn/cọc/hóa đơn
  đều qua `INumberSequenceService` trong transaction (`ITransactionRunner`).
- **Hủy/No-show:** phí hủy theo BR-05 (`PricingService.CancellationFee`); hoàn cọc tạo Payment âm gắn ca;
  No-show thu 100% cọc (Deposit→Forfeited), lưu `Reservation.CancellationFee`.

## 3. Cách verify (máy có .NET SDK)

Sau khi build+chạy: tạo đơn 2 phòng → trùng phòng bị chặn → xác nhận → thu cọc (chưa mở ca bị chặn) →
mở ca rồi thu cọc → hủy đơn thấy phí/hoàn đúng chính sách → đơn quá hạn hiện ở No-show.

## 4. Nợ kỹ thuật

| # | Món nợ | Ghi chú |
|---|---|---|
| 1 | Chưa build/verify | Máy code không có SDK |
| 2 | Tra khách C04 dùng `<select>` toàn bộ | Chưa có tìm-nhanh AJAX (FR-B02); ổn cho quy mô nhỏ |
| 3 | Phí hủy chưa vào Payment(CancellationFee) | Ghi ở `Reservation.CancellationFee`; báo cáo doanh thu (SP4) lấy từ đó; shift "phí hủy" tạm = 0 |
| 4 | Hủy khi có cọc bắt buộc mở ca (nghiêm hơn spec cho phần refund) | Đơn giản & an toàn; xem lại nếu cần |
| 5 | C03 sơ đồ phòng, C10 in phiếu xác nhận | Nice-to-have, để sau |
| 6 | Link Check-in trỏ `FrontDesk/CheckIn/{id}` | Do SP2 hiện thực; kiểm route khớp khi ghép |

## 5. Làm tiếp
SP2 (Lễ tân + Thu ngân + minibar), SP3 (Buồng phòng), SP4 (Báo cáo) — đang chạy song song bằng subagent.
