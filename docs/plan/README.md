# Hồ sơ thực hiện (`docs/plan/`)

Nơi lưu hồ sơ từng tính năng: đã làm gì, vì sao làm thế, chỗ nào còn nợ, và cách kiểm thử lại.
Mục đích là để lần sau mở ra đọc rồi làm tiếp hoặc sửa lỗi mà không phải dò lại toàn bộ code.

| File | Tính năng | Trạng thái |
|---|---|---|
| [`01-authentication-authorization.md`](01-authentication-authorization.md) | Đăng nhập, phân quyền theo vai trò, đổi mật khẩu, audit log, menu tự ẩn theo quyền | ✅ Đã xong, đã kiểm thử |
| [`02-catalog-rooms.md`](02-catalog-rooms.md) | Danh mục Loại phòng & Phòng (SCR-A01…A05) — kèm khuôn mẫu cho mọi màn hình sau | ✅ Đã xong, đã kiểm thử |

## Thứ tự làm tiếp

1. **SCR-A06/A07 — Dịch vụ**: gần như bản sao của Loại phòng, thêm phần tồn kho.
2. **SCR-B01…B03 — Hồ sơ khách**: cần có trước khi làm đặt phòng.
3. **Lát cắt dọc luồng chính**: tra phòng trống → đặt phòng → check-in → folio → check-out →
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
