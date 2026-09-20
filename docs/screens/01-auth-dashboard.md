# 01 — Hệ thống & Tổng quan (Nhóm S)

---

## SCR-S01 — Đăng nhập

| | |
|---|---|
| **URL** | `GET/POST /Account/Login` |
| **Controller** | `AccountController.Login` |
| **Quyền truy cập** | Mọi người (`[AllowAnonymous]`) |
| **Yêu cầu liên quan** | FR-A08, NFR-02 |

### Mục đích
Xác thực nhân viên trước khi vào hệ thống.

### Thành phần giao diện
- Ô **Tên đăng nhập** (bắt buộc), ô **Mật khẩu** (bắt buộc, dạng password).
- Hộp kiểm **Ghi nhớ đăng nhập**.
- Nút **Đăng nhập**.
- Vùng hiển thị lỗi phía trên form.

### Luồng xử lý
1. Người dùng nhập tài khoản, mật khẩu, nhấn Đăng nhập.
2. Hệ thống tìm nhân viên theo tên đăng nhập.
3. Kiểm tra lần lượt:
   - Không tìm thấy tài khoản → báo lỗi chung (xem mục Bảo mật).
   - Tài khoản bị khóa → "Tài khoản đã bị khóa. Liên hệ quản lý."
   - Nhân viên đã nghỉ việc → "Tài khoản không còn hiệu lực."
   - Mật khẩu sai → tăng `FailedLoginCount`, báo lỗi chung.
4. Sai **5 lần liên tiếp** → tự khóa tài khoản, ghi audit log.
5. Đúng → đặt lại `FailedLoginCount = 0`, tạo cookie xác thực chứa `UserId` và `Role`,
   ghi thời điểm đăng nhập cuối, chuyển tới `returnUrl` hoặc `/Home/Index`.

### Quy tắc bảo mật
- Thông báo lỗi khi sai tài khoản và sai mật khẩu phải **giống hệt nhau**:
  "Tên đăng nhập hoặc mật khẩu không đúng" — tránh lộ tài khoản nào tồn tại.
- Mật khẩu lưu dạng băm (BCrypt/PBKDF2), **không bao giờ** lưu hoặc log dạng gốc.
- Cookie đặt `HttpOnly`, `SameSite=Lax`, thời hạn 8 giờ (hoặc 7 ngày nếu chọn ghi nhớ).

### Phân quyền sau đăng nhập
Trang đích mặc định theo vai trò: Admin → Dashboard; Lễ tân → `/FrontDesk`;
Buồng phòng → `/Housekeeping`.

---

## SCR-S02 — Đổi mật khẩu

| | |
|---|---|
| **URL** | `GET/POST /Account/ChangePassword` |
| **Quyền truy cập** | Mọi vai trò đã đăng nhập (chỉ đổi mật khẩu của chính mình) |
| **Yêu cầu liên quan** | FR-A08 |

### Thành phần
Mật khẩu hiện tại `*`, mật khẩu mới `*`, xác nhận mật khẩu mới `*`, nút **Cập nhật**.

### Luồng xử lý & kiểm tra
1. Mật khẩu hiện tại phải đúng, nếu không → "Mật khẩu hiện tại không đúng."
2. Mật khẩu mới tối thiểu 8 ký tự, có chữ và số.
3. Mật khẩu mới phải khác mật khẩu hiện tại.
4. Xác nhận phải trùng khớp.
5. Lưu mật khẩu băm mới, **đăng xuất mọi phiên khác**, ghi audit log, báo thành công.

### Phân quyền
Không ai — kể cả Admin — đổi được mật khẩu người khác ở màn hình này. Admin chỉ có thể
**đặt lại** mật khẩu người khác ở SCR-A11 (đặt mật khẩu tạm, buộc đổi ở lần đăng nhập sau).

---

## SCR-S03 — Không có quyền (403)

| | |
|---|---|
| **URL** | `/Home/Forbidden` |
| **Quyền truy cập** | Mọi vai trò đã đăng nhập |

### Nội dung
Thông báo "Bạn không có quyền truy cập chức năng này", tên vai trò hiện tại, nút quay về
trang chủ theo vai trò. Có ghi audit log (mức Warning) mỗi lần bị chặn để phát hiện dò quyền.

---

## SCR-S04 — Dashboard tổng quan

| | |
|---|---|
| **URL** | `GET /Home/Index` |
| **Controller** | `HomeController.Index` |
| **Quyền truy cập** | Mọi vai trò (nội dung khác nhau theo vai trò) |
| **Yêu cầu liên quan** | FR-G09 |

### Mục đích
Cho người trực ca nhìn thấy trong 5 giây: hôm nay còn bao nhiêu phòng bán được, ai sắp đến,
ai sắp đi, có việc gì đang tồn.

### Bố cục

```
┌─ Hàng thẻ số liệu ───────────────────────────────────────────────┐
│ [Phòng trống 12] [Đang ở 25] [Chờ dọn 5] [Bảo trì 2]            │
│ [Khách đến hôm nay 8] [Khách đi hôm nay 6] [Doanh thu hôm nay]   │
├─ Cột trái ───────────────────┬─ Cột phải ────────────────────────┤
│ Danh sách khách đến hôm nay  │ Danh sách khách đi hôm nay        │
│ (mã đơn, khách, loại phòng,  │ (phòng, khách, giờ trả dự kiến,   │
│  nút Check-in)               │  số dư phải thu, nút Check-out)   │
├──────────────────────────────┴───────────────────────────────────┤
│ Việc cần xử lý: phòng chờ dọn quá 2 giờ · đơn quá hạn giữ chỗ ·  │
│ yêu cầu bảo trì chưa xử lý · hàng tồn dưới định mức              │
└──────────────────────────────────────────────────────────────────┘
```

### Dữ liệu và cách tính

| Thẻ | Cách tính |
|---|---|
| Phòng trống | Số phòng `Available` (loại trừ `OutOfService`) |
| Đang ở | Số phòng `Occupied` |
| Chờ dọn | Số phòng `Dirty` |
| Bảo trì | Số phòng `Maintenance` |
| Khách đến hôm nay | Reservation `Confirmed` có `CheckInDate = hôm nay` |
| Khách đi hôm nay | Stay `CheckedIn` có `ExpectedCheckOut = hôm nay` |
| Doanh thu hôm nay | Tổng `Invoice` trạng thái `Settled` chốt trong ngày |

### Phân quyền nội dung

| Thành phần | Admin | Lễ tân | Buồng phòng |
|---|:-:|:-:|:-:|
| Thẻ trạng thái phòng | ✔ | ✔ | ✔ |
| Thẻ doanh thu hôm nay | ✔ | Chỉ ca của mình | Ẩn |
| Danh sách khách đến / đi | ✔ | ✔ | Ẩn |
| Việc cần xử lý — phòng chờ dọn, bảo trì | ✔ | ✔ | ✔ |
| Việc cần xử lý — đơn quá hạn, tồn kho | ✔ | ✔ | Ẩn |

Với vai trò Buồng phòng, dashboard rút gọn chỉ còn phần trạng thái phòng và việc cần dọn;
mọi số liệu tiền bạc đều bị ẩn ở cả tầng view lẫn tầng dữ liệu (không truy vấn, không gửi xuống client).
