# 00 — Quy ước chung cho mọi màn hình

Mọi màn hình mô tả trong thư mục này đều tuân theo các quy ước dưới đây; các file sau
sẽ **không lặp lại** những nội dung này.

## 1. Bố cục chung (`Views/Shared/_Layout.cshtml`)

```
┌──────────────────────────────────────────────────────────────────────┐
│ Thanh trên: [Logo khách sạn]   Ngày làm việc: 20/09/2026             │
│             Ca: #12 đang mở ▸  |  Nguyễn Văn A (Lễ tân) ▾ [Đăng xuất]│
├───────────────┬──────────────────────────────────────────────────────┤
│ Menu trái     │  Tiêu đề màn hình                    [Nút hành động] │
│ (_Sidebar)    │  ── breadcrumb ───────────────────────────────────── │
│               │                                                      │
│ • Tổng quan   │  Vùng thông báo (thành công / lỗi / cảnh báo)        │
│ • Đặt phòng   │                                                      │
│ • Lễ tân      │  Nội dung chính (bảng / form / thẻ số liệu)          │
│ • Thu ngân    │                                                      │
│ • Khách hàng  │                                                      │
│ • Danh mục ▾  │                                                      │
│ • Báo cáo ▾   │                                                      │
└───────────────┴──────────────────────────────────────────────────────┘
```

- **Menu trái tự ẩn mục không có quyền.** Ẩn menu chỉ là lớp tiện dụng, không phải bảo mật —
  quyền vẫn phải chặn ở controller bằng `[Authorize(Roles = "...")]`.
- **Thanh trên** hiển thị ca làm việc đang mở của người đăng nhập (chỉ với Admin/Lễ tân).
  Nếu chưa mở ca, hiện nhãn đỏ "Chưa mở ca" kèm liên kết tới SCR-F08.

## 2. Quy ước phân quyền

| Tình huống | Hành vi hệ thống |
|---|---|
| Chưa đăng nhập, truy cập URL bất kỳ | Chuyển hướng về `/Account/Login?returnUrl=...` |
| Đã đăng nhập nhưng sai vai trò | Trả về SCR-S03 (403), **không** chuyển về trang chủ âm thầm |
| Có quyền xem, không có quyền sửa | Vào được màn hình danh sách nhưng nút Thêm/Sửa/Xóa bị ẩn, và POST tương ứng vẫn bị chặn ở server |
| Hết hạn phiên đăng nhập | Về trang đăng nhập kèm thông báo "Phiên làm việc đã hết hạn" |

**Nguyên tắc bắt buộc:** mọi kiểm tra quyền phải có ở **cả hai tầng** — ẩn/hiện trên giao diện
và `[Authorize]` + kiểm tra nghiệp vụ trong action. Ẩn nút mà không chặn server là lỗi bảo mật.

## 3. Quy ước form và validate

- Mọi form POST đều có `@Html.AntiForgeryToken()` và action gắn `[ValidateAntiForgeryToken]`.
- Lỗi nhập liệu hiển thị **ngay dưới ô nhập** (màu đỏ) + tóm tắt ở đầu form.
- Trường bắt buộc đánh dấu dấu `*` đỏ.
- Số tiền nhập/hiển thị theo định dạng `1.250.000 ₫`; ngày theo `dd/MM/yyyy`; giờ theo `HH:mm`.
- Nút **Lưu** bị vô hiệu hóa sau cú nhấp đầu tiên để tránh gửi trùng (double submit).
- Form Create và Edit dùng chung một partial view `_Form.cshtml` cho mỗi danh mục.

## 4. Quy ước thông báo

| Loại | Màu | Ví dụ |
|---|---|---|
| Thành công | Xanh lá | "Đã check-in phòng 201 cho khách Nguyễn Văn B." |
| Cảnh báo | Vàng | "Phòng 305 vượt sức chứa chuẩn, sẽ tính phụ thu thêm người." |
| Lỗi nghiệp vụ | Đỏ | "Phòng 201 đã có đơn đặt từ 20/09 đến 22/09." |
| Xác nhận | Hộp thoại | "Bạn chắc chắn hủy đơn RSV-000123? Khách sẽ bị thu 100% tiền cọc." |

Thông báo sau khi chuyển trang dùng `TempData["Success"] / TempData["Error"]`.

## 5. Quy ước bảng danh sách

- Mặc định **20 dòng/trang**, có phân trang ở cuối bảng.
- Có ô tìm kiếm nhanh ở góc trên phải; bộ lọc nâng cao thu gọn được.
- Cột thao tác luôn nằm cuối, dạng biểu tượng kèm tooltip.
- Danh sách mặc định **không hiện bản ghi đã ngừng sử dụng**, có hộp kiểm "Hiện cả mục đã ngừng".
- Bảng rỗng hiển thị dòng hướng dẫn, ví dụ: "Chưa có loại phòng nào. Nhấn *Thêm loại phòng* để bắt đầu."

## 6. Quy ước màu trạng thái phòng

| Trạng thái | Màu | Ý nghĩa |
|---|---|---|
| `Available` | Xanh lá | Trống, đã dọn sạch, sẵn sàng bán |
| `Reserved` | Xanh dương | Có đơn đặt đến trong ngày |
| `Occupied` | Cam | Khách đang ở |
| `Dirty` | Xám | Chờ dọn |
| `Maintenance` | Đỏ | Đang bảo trì |
| `OutOfService` | Đen nhạt | Ngừng khai thác (không tính vào công suất) |

Bảng chú thích màu này hiển thị ở SCR-A03, SCR-C03 và SCR-E01.

## 7. Quy ước ghi nhật ký (audit)

Mọi action thuộc danh sách ở **BR-11** phải gọi `IAuditService.Log(...)` **trong cùng
transaction** với thao tác nghiệp vụ, lưu: người thực hiện, hành động, đối tượng,
giá trị cũ → giá trị mới, lý do (nếu có), thời điểm, địa chỉ IP.

## 8. Quy ước chặn thao tác theo ca làm việc

Mọi màn hình có thu / hoàn tiền (SCR-C07, SCR-F05, SCR-F07)
đều kiểm tra trước: **người dùng phải có một ca đang mở**. Nếu chưa, màn hình hiển thị cảnh báo
và nút "Mở ca làm việc" thay cho form thu tiền (BR-10).

## 9. Quy ước đặt tên

| Thành phần | Quy ước | Ví dụ |
|---|---|---|
| Controller | `{Module}Controller` | `ReservationsController` |
| View | `Views/{Module}/{Action}.cshtml` | `Views/FrontDesk/CheckIn.cshtml` |
| ViewModel | `{Màn hình}ViewModel` | `CheckInViewModel` |
| Partial dùng chung | `_TênPartial.cshtml` | `_RoomStatusBadge.cshtml` |
