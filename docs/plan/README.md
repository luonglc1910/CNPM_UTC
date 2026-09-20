# Hồ sơ thực hiện (`docs/plan/`)

Nơi lưu hồ sơ từng tính năng: đã làm gì, vì sao làm thế, chỗ nào còn nợ, và cách kiểm thử lại.
Mục đích là để lần sau mở ra đọc rồi làm tiếp hoặc sửa lỗi mà không phải dò lại toàn bộ code.

| File | Tính năng | Trạng thái |
|---|---|---|
| [`01-authentication-authorization.md`](01-authentication-authorization.md) | Đăng nhập, phân quyền theo vai trò, đổi mật khẩu, audit log, menu tự ẩn theo quyền | ✅ Đã xong, đã kiểm thử |
| [`02-catalog-rooms.md`](02-catalog-rooms.md) | Danh mục Loại phòng & Phòng (SCR-A01…A05) — kèm khuôn mẫu cho mọi màn hình sau | ✅ Đã xong, đã kiểm thử |
| [`03-catalog-services.md`](03-catalog-services.md) | Danh mục Dịch vụ (SCR-A06, A07) — kèm tham số quản lý kho và cảnh báo dưới định mức | ✅ Đã xong, đã kiểm thử |
| [`04-inventory.md`](04-inventory.md) | Tồn kho & phiếu kho (SCR-A08, A09) — nhập kho, điều chỉnh kiểm kê, lịch sử chỉ đọc | ✅ Đã xong, đã kiểm thử |
| [`05-employees.md`](05-employees.md) | Nhân viên & tài khoản (SCR-A10, A11) — khóa/mở khóa, đặt lại mật khẩu, đổi vai trò là đăng xuất mọi phiên | ✅ Đã xong, đã kiểm thử |
| [`06-system-settings.md`](06-system-settings.md) | Cấu hình hệ thống (SCR-A12) — 6 nhóm tham số, logo, khôi phục mặc định từng nhóm. **Đóng lại nhóm A** | ✅ Đã xong, đã kiểm thử |

## Thứ tự làm tiếp

Nhóm A (danh mục & cấu hình) đã xong toàn bộ SCR-A01…A12.

1. **SCR-B01…B03 — Hồ sơ khách**: cần có trước khi làm đặt phòng.
2. **Lát cắt dọc luồng chính**: tra phòng trống → đặt phòng → check-in → folio → check-out →
   thanh toán. Đây là phần khó và quan trọng nhất (chống trùng phòng BR-06, tính tiền
   BR-02/BR-03, transaction lúc check-out). Đừng dồn quá nhiều thời gian vào màn hình danh mục.

Khuôn mẫu code để nhân bản (Service, ServiceResult, PagedList, form dùng chung, ẩn nút theo quyền)
mô tả ở [`02-catalog-rooms.md`](02-catalog-rooms.md) mục 2.

## Quy ước

- Mỗi tính năng một file, đánh số theo thứ tự làm.
- Mỗi file phải trả lời được 5 câu: **làm gì · file nào · quyết định thiết kế nào và vì sao ·
  kiểm thử ra sao · còn nợ gì**.
- Phần **Nợ kỹ thuật** là quan trọng nhất khi quay lại — luôn cập nhật khi trả xong một món.

## Chỗ đứng trong bộ tài liệu

| Thư mục | Nội dung |
|---|---|
| [`../../REQUIREMENTS.md`](../../REQUIREMENTS.md) | Đặc tả tổng: phạm vi, tác nhân, quy tắc nghiệp vụ (BR), yêu cầu chức năng (FR) |
| [`../screens/`](../screens/) | Đặc tả chi tiết từng màn hình (SCR-xxx) và ma trận phân quyền |
| `docs/plan/` (thư mục này) | **Quá trình thực hiện**: đã code gì, quyết định ra sao, còn nợ gì |

Hai thư mục đầu trả lời *"hệ thống phải làm gì"*; thư mục này trả lời *"đã làm tới đâu và như thế nào"*.
