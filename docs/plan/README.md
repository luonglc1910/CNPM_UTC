# Thư mục `plan/`

Nơi lưu hồ sơ từng tính năng: đã làm gì, vì sao làm thế, chỗ nào còn nợ, và cách kiểm thử lại.
Mục đích là để lần sau mở ra đọc rồi làm tiếp hoặc sửa lỗi mà không phải dò lại toàn bộ code.

| File | Tính năng | Trạng thái |
|---|---|---|
| [`01-authentication-authorization.md`](01-authentication-authorization.md) | Đăng nhập, phân quyền theo vai trò, đổi mật khẩu, audit log | ✅ Đã xong, đã kiểm thử |

## Quy ước

- Mỗi tính năng một file, đánh số theo thứ tự làm.
- Mỗi file phải trả lời được 5 câu: **làm gì · file nào · quyết định thiết kế nào và vì sao ·
  kiểm thử ra sao · còn nợ gì**.
- Phần **Nợ kỹ thuật** là quan trọng nhất khi quay lại — luôn cập nhật khi trả xong một món.
- Tài liệu đặc tả nghiệp vụ nằm ở [`../REQUIREMENTS.md`](../REQUIREMENTS.md) và
  [`../docs/screens/`](../docs/screens/); thư mục này chỉ ghi **quá trình thực hiện**.
