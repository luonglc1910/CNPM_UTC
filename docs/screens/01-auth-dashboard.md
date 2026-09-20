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
- **Thứ tự kiểm tra: mật khẩu trước, trạng thái sau.** Yêu cầu "thông báo giống hệt nhau" và
  yêu cầu "báo riêng khi tài khoản bị khóa" chỉ dung hòa được khi thông báo riêng chỉ hiện ra
  cho người đã nhập đúng mật khẩu. Kiểm trạng thái trước sẽ để lộ tài khoản nào tồn tại.
- Khi tài khoản không tồn tại, hệ thống vẫn phải **chạy một lần băm giả** rồi mới báo lỗi;
  bỏ qua bước này thì thời gian đáp ứng nhanh hơn hẳn và vẫn lộ tài khoản nào có thật.
- Mật khẩu lưu dạng băm (BCrypt/PBKDF2), **không bao giờ** lưu hoặc log dạng gốc.
- Cookie đặt `HttpOnly`, `SameSite=Lax`, thời hạn 8 giờ (hoặc 7 ngày nếu chọn ghi nhớ).

### Phân quyền sau đăng nhập
Trang đích mặc định theo vai trò: Admin → Dashboard; Lễ tân → `/FrontDesk`.

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
5. Lưu mật khẩu băm mới, ghi audit log, phát hành lại cookie cho phiên hiện tại, báo thành công.

> **Chưa triển khai (ghi nhận nợ kỹ thuật):**
> - *Đăng xuất mọi phiên khác*: cookie authentication không giữ sổ phiên nên không thu hồi được
>   phiên trên máy khác. Muốn làm đúng phải thêm `SecurityStamp` và kiểm mỗi request, hoặc dùng
>   `ITicketStore` — để đợt sau. Phiên hiện tại vẫn được làm mới đúng.
> - *Tự khóa tài khoản sau 5 lần sai* (FR-A08): `FailedLoginCount` đã được đếm và reset đúng,
>   tài khoản ở trạng thái `Locked` đã bị từ chối, nhưng hệ thống chưa tự chuyển sang `Locked`.

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

### Bố cục — **đã sửa 20/09/2026**

```
┌─ Hàng thẻ trạng thái phòng ──────────────────────────────┐
│ [Phòng trống] [Đang ở] [Chờ dọn] [Bảo trì]                      │
├─ Hàng thẻ hôm nay ───────────────────────────────────┤
│ [Khách đến] [Khách đi] [Doanh thu hôm nay]                    │
├─ Việc cần xử lý ───────────────────────────────────┤
│ n  Phòng chờ dọn            → SCR-E01                        │
│ n  Đơn quá hạn giữ chỗ      → SCR-C09                        │
│ n  Yêu cầu chưa xử lý        → SCR-E04                        │
│ n  Hàng tồn dưới định mức    → SCR-A08                        │
└────────────────────────────────────────────────┘
```

> **Bỏ hai bảng khách đến / khách đi.** Bản đặc tả trước đây vẽ hai bảng này kèm nút
> Check-in / Check-out. Đó đúng là tab 1 và tab 2 của SCR-D01 — giữ cả hai nơi thì mỗi lần
> đổi nghiệp vụ phải sửa hai chỗ, và trái với đợt gọn màn hình cùng ngày. Thay vào đó
> mỗi con số trên thẻ bấm được, dẫn thẳng sang màn có bảng và nút.
>
> **Bỏ điều kiện "phòng chờ dọn quá 2 giờ".** `Room` không lưu mốc chuyển trạng thái;
> `BaseEntity.UpdatedAt` đổi vì bất kỳ sửa đổi nào trên phòng nên dùng nó sẽ cho số sai một
> cách âm thầm. Hiện đếm toàn bộ phòng `Dirty`. Muốn đúng đặc tả thì phải thêm cột
> `Room.StatusChangedAt` — một migration, chưa làm.

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

| Thành phần | Admin | Lễ tân |
|---|:-:|:-:|
| Thẻ trạng thái phòng | ✔ | ✔ |
| Thẻ doanh thu hôm nay | ✔ (toàn khách sạn) | Chỉ ca của mình |
| Danh sách khách đến / đi | ✔ | ✔ |
| Việc cần xử lý — phòng chờ dọn, bảo trì | ✔ | ✔ |
| Việc cần xử lý — đơn quá hạn, tồn kho | ✔ | ✔ |

Chỉ còn một khác biệt về nội dung: **thẻ doanh thu**. Admin thấy doanh thu toàn khách sạn,
lễ tân chỉ thấy phần thu trong ca của mình. Giới hạn này phải làm ở **tầng truy vấn**
(lọc theo `CashierShiftId`), không phải chỉ ẩn trên view.
