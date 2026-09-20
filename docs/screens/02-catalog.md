# 02 — Danh mục & Cấu hình hệ thống (Nhóm A)

> Nguyên tắc chung cho cả nhóm: **Admin toàn quyền; Lễ tân và Buồng phòng chỉ xem**
> (cần xem để biết giá và tiện nghi khi tư vấn khách). Ngoại lệ duy nhất là SCR-A05.
> Danh mục đã phát sinh giao dịch **không được xóa cứng** — chỉ đánh dấu ngừng sử dụng.

---

## SCR-A01 — Danh sách loại phòng

| | |
|---|---|
| **URL** | `GET /RoomTypes` |
| **Quyền** | Admin: đầy đủ · Lễ tân, Buồng phòng: chỉ xem |
| **Yêu cầu** | FR-A01 |

### Cột hiển thị
Mã loại · Tên loại · Sức chứa chuẩn / tối đa · Giá/đêm · Phí thêm người/đêm ·
Số phòng thuộc loại · Trạng thái (Đang dùng / Ngừng) · Thao tác.

### Bộ lọc
Ô tìm theo tên/mã; hộp kiểm "Hiện cả loại đã ngừng dùng".

### Thao tác
| Nút | Quyền | Ghi chú |
|---|---|---|
| Thêm loại phòng | Admin | → SCR-A02 |
| Sửa | Admin | → SCR-A02 |
| Ngừng sử dụng | Admin | Chỉ khi loại phòng **không còn phòng đang hoạt động** nào |

---

## SCR-A02 — Thêm / sửa loại phòng

| | |
|---|---|
| **URL** | `GET/POST /RoomTypes/Create`, `/RoomTypes/Edit/{id}` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-A01, BR-02, BR-03 |

### Các trường

| Trường | Bắt buộc | Kiểm tra |
|---|:-:|---|
| Mã loại phòng | ✔ | Duy nhất, 2–10 ký tự, chữ hoa và số. Không sửa được sau khi tạo |
| Tên loại phòng | ✔ | ≤ 100 ký tự |
| Sức chứa chuẩn | ✔ | Số nguyên 1–10 |
| Sức chứa tối đa | ✔ | ≥ sức chứa chuẩn, ≤ 10 |
| Giá / đêm | ✔ | > 0 |
| Phí thêm người / đêm | | ≥ 0, mặc định 0 |
| Phí giường phụ / đêm | | ≥ 0 |
| Tiện nghi | | Danh sách chọn nhiều (điều hòa, tivi, tủ lạnh, ban công...) |
| Mô tả | | Văn bản dài |

### Quy tắc nghiệp vụ
- **Sửa giá không ảnh hưởng hồi tố**: các Stay đang mở đã chốt giá lúc check-in (BR-02),
  giá mới chỉ áp cho lượt lưu trú tạo sau đó. Màn hình phải hiện dòng nhắc rõ điều này.
- Mọi lần sửa giá đều **ghi audit log** giá cũ → giá mới (BR-11).
- Giảm sức chứa tối đa xuống dưới số khách đang thực ở của một phòng thuộc loại đó → cảnh báo
  nhưng vẫn cho lưu (không đuổi khách), chỉ ảnh hưởng lần nhận phòng sau.

---

## SCR-A03 — Danh sách phòng

| | |
|---|---|
| **URL** | `GET /Rooms` |
| **Quyền** | Admin: đầy đủ · Lễ tân, Buồng phòng: chỉ xem + SCR-A05 |
| **Yêu cầu** | FR-A02 |

### Hiển thị
Hai chế độ chuyển đổi được:
- **Dạng bảng**: Số phòng · Tầng · Loại phòng · Giá/đêm · Trạng thái (huy hiệu màu) · Khách đang ở · Thao tác.
- **Dạng lưới theo tầng**: mỗi phòng là một ô màu theo trạng thái (bảng màu ở `00-conventions.md` mục 6),
  nhấp vào ô mở nhanh menu thao tác.

### Bộ lọc
Tầng · Loại phòng · Trạng thái · Ô tìm theo số phòng.

### Thao tác
| Nút | Quyền |
|---|---|
| Thêm phòng / Sửa | Admin |
| Đổi trạng thái | Admin, Lễ tân, Buồng phòng (xem SCR-A05) |
| Ngừng khai thác | Admin, chỉ khi phòng không có khách và không có đơn đặt trong tương lai |
| Xem lượt lưu trú hiện tại | Admin, Lễ tân — mở SCR-D04 |

---

## SCR-A04 — Thêm / sửa phòng

| | |
|---|---|
| **URL** | `GET/POST /Rooms/Create`, `/Rooms/Edit/{id}` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-A02 |

### Các trường

| Trường | Bắt buộc | Kiểm tra |
|---|:-:|---|
| Số phòng | ✔ | **Duy nhất toàn hệ thống**, 1–10 ký tự |
| Tầng | ✔ | Số nguyên ≥ 0 |
| Loại phòng | ✔ | Chọn từ danh mục đang dùng |
| Trạng thái ban đầu | ✔ | Mặc định `Available`; phòng mới xây chọn `Maintenance` |
| Ghi chú | | Ví dụ: "phòng góc, view hồ" |

### Quy tắc nghiệp vụ
- **Không xóa cứng** phòng đã phát sinh Stay — nút Xóa chỉ hiện với phòng chưa từng dùng.
- Đổi loại phòng của một phòng **đang có khách** → chặn, báo "Không thể đổi loại phòng khi
  đang có khách lưu trú."
- Đổi loại phòng khi phòng **đã có đơn đặt trong tương lai** → cảnh báo kèm danh sách đơn
  bị ảnh hưởng, yêu cầu Admin xác nhận, và ghi audit log.

---

## SCR-A05 — Đổi trạng thái phòng

| | |
|---|---|
| **URL** | `POST /Rooms/UpdateStatus/{id}` (hộp thoại từ SCR-A03 hoặc SCR-E01) |
| **Quyền** | Admin, Lễ tân, Buồng phòng — **nhưng khác nhau về trạng thái được chọn** |
| **Yêu cầu** | FR-A03, BR-11 |

### Ma trận chuyển trạng thái được phép

| Từ → Đến | Admin | Lễ tân | Buồng phòng |
|---|:-:|:-:|:-:|
| `Dirty` → `Available` (dọn xong) | ✔ | ✔ | ✔ |
| bất kỳ → `Maintenance` (báo hỏng) | ✔ | ✔ | ✔ |
| `Maintenance` → `Dirty` (sửa xong) | ✔ | ✔ | — |
| `Available` → `Dirty` (bẩn lại) | ✔ | ✔ | ✔ |
| bất kỳ → `OutOfService` | ✔ | — | — |
| `OutOfService` → `Dirty` | ✔ | — | — |
| `Occupied` → bất kỳ | **Không ai** — chỉ thay đổi qua check-out / đổi phòng | | |

### Các trường trong hộp thoại
Trạng thái mới `*` (chỉ liệt kê trạng thái người dùng được phép), **Lý do** `*` khi chuyển sang
`Maintenance` hoặc `OutOfService`, ghi chú.

### Quy tắc
- Chuyển sang `Maintenance` khi phòng có **đơn đặt trong tương lai** → cảnh báo danh sách đơn
  cần xếp lại phòng; hệ thống không tự hủy đơn.
- Mọi lần đổi trạng thái đều ghi audit log: ai, từ trạng thái nào sang trạng thái nào, lý do.

---

## SCR-A06 — Danh sách dịch vụ

| | |
|---|---|
| **URL** | `GET /HotelServices` |
| **Quyền** | Admin: đầy đủ · Lễ tân, Buồng phòng: chỉ xem |
| **Yêu cầu** | FR-A04 |

### Cột hiển thị
Mã · Tên dịch vụ · Nhóm (Ăn uống / Minibar / Giặt ủi / Thuê xe / Khác) · Đơn giá · Đơn vị tính ·
Có quản lý kho · Tồn hiện tại (nếu có kho) · Trạng thái · Thao tác.

### Hiển thị đặc biệt
Dòng có tồn kho **dưới định mức tối thiểu** được tô nền vàng kèm biểu tượng cảnh báo.

---

## SCR-A07 — Thêm / sửa dịch vụ

| | |
|---|---|
| **URL** | `GET/POST /HotelServices/Create`, `/Edit/{id}` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-A04, BR-12 |

### Các trường

| Trường | Bắt buộc | Kiểm tra |
|---|:-:|---|
| Mã dịch vụ | ✔ | Duy nhất, không sửa sau khi tạo |
| Tên dịch vụ | ✔ | ≤ 100 ký tự |
| Nhóm dịch vụ | ✔ | Chọn từ danh sách cố định |
| Đơn giá | ✔ | > 0 |
| Đơn vị tính | ✔ | lon, chai, kg, lượt, giờ... |
| Có quản lý kho | | Hộp kiểm. Bật lên thì hiện thêm 2 trường bên dưới |
| Tồn tối thiểu (cảnh báo) | Khi bật kho | ≥ 0 |
| Cho phép bán khi hết hàng | Khi bật kho | Mặc định **không**; chỉ Admin bật được |

### Quy tắc
- Bật "có quản lý kho" cho dịch vụ đã tồn tại → tồn khởi tạo = 0, phải nhập kho ở SCR-A09 mới bán được.
- Tắt "có quản lý kho" → cảnh báo sẽ **ngừng theo dõi tồn**, ghi audit log.
- Sửa đơn giá không ảnh hưởng các dòng dịch vụ đã ghi vào folio trước đó (đơn giá được chép
  vào `FolioItem` tại thời điểm ghi nhận).

---

## SCR-A08 — Tồn kho

| | |
|---|---|
| **URL** | `GET /Inventory` |
| **Quyền** | Admin: đầy đủ · Lễ tân, Buồng phòng: chỉ xem tồn |
| **Yêu cầu** | FR-A05, BR-12 |

### Nội dung
- Bảng: Dịch vụ · Đơn vị · Tồn hiện tại · Tồn tối thiểu · Lần nhập gần nhất · Thao tác.
- Tab **Lịch sử giao dịch kho**: thời điểm · loại (Nhập / Bán / Điều chỉnh / Hoàn) ·
  số lượng (+/−) · tồn sau giao dịch · chứng từ liên quan (mã folio) · người thực hiện · lý do.

### Quy tắc
Lịch sử kho **chỉ đọc, không sửa, không xóa**. Sai sót được chỉnh bằng một phiếu điều chỉnh mới
có lý do, không phải bằng cách sửa bản ghi cũ.

---

## SCR-A09 — Nhập kho / Điều chỉnh kho

| | |
|---|---|
| **URL** | `GET/POST /Inventory/Receive`, `/Inventory/Adjust` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-A05, BR-11, BR-12 |

### Nhập kho
Chọn dịch vụ `*` · Số lượng nhập `*` (> 0) · Ghi chú (số hóa đơn nhà cung cấp).
→ Tồn tăng, tạo `InventoryTransaction` loại `Receive`.

### Điều chỉnh kho (kiểm kê)
Chọn dịch vụ `*` · Tồn thực tế đếm được `*` (≥ 0) · **Lý do** `*` (bắt buộc, ví dụ: hỏng, mất, kiểm kê).
→ Hệ thống tính chênh lệch so với tồn sổ sách, tạo giao dịch `Adjust` với số lượng chênh (+/−),
ghi audit log.

---

## SCR-A10 — Danh sách nhân viên

| | |
|---|---|
| **URL** | `GET /Employees` |
| **Quyền** | **Chỉ Admin.** Lễ tân và Buồng phòng truy cập → 403 |
| **Yêu cầu** | FR-A06 |

### Cột
Mã NV · Họ tên · Vai trò · SĐT · Tên đăng nhập · Trạng thái (Đang làm / Đã nghỉ / Bị khóa) ·
Đăng nhập gần nhất · Thao tác (Sửa · Khóa/Mở khóa · Đặt lại mật khẩu).

### Quy tắc
- Không xóa nhân viên đã phát sinh giao dịch — chỉ chuyển sang "Đã nghỉ việc".
- **Admin không thể tự khóa hoặc tự hạ quyền chính mình** (tránh khóa cứng hệ thống).
- Hệ thống luôn phải còn **ít nhất một tài khoản Admin đang hoạt động** — thao tác vi phạm bị chặn.

---

## SCR-A11 — Thêm / sửa nhân viên

| | |
|---|---|
| **URL** | `GET/POST /Employees/Create`, `/Edit/{id}` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-A06, FR-A08, BR-11 |

### Các trường

| Trường | Bắt buộc | Kiểm tra |
|---|:-:|---|
| Họ tên | ✔ | ≤ 100 ký tự |
| Số điện thoại | ✔ | Đúng định dạng SĐT Việt Nam |
| Email | | Đúng định dạng nếu có nhập |
| CCCD | | Duy nhất nếu có nhập |
| Vai trò | ✔ | Admin / Lễ tân / Buồng phòng |
| Tên đăng nhập | ✔ | Duy nhất, không dấu, không khoảng trắng, không sửa sau khi tạo |
| Mật khẩu ban đầu | ✔ khi tạo mới | ≥ 8 ký tự; buộc đổi ở lần đăng nhập đầu |
| Trạng thái | ✔ | Đang làm / Đã nghỉ |

### Quy tắc nghiệp vụ
- **Đổi vai trò** ghi audit log bắt buộc (BR-11); người bị đổi vai trò bị đăng xuất khỏi
  mọi phiên để nạp lại quyền mới.
- **Đặt lại mật khẩu**: Admin sinh mật khẩu tạm, hệ thống không hiển thị mật khẩu cũ,
  người dùng bị buộc đổi ở lần đăng nhập kế tiếp.
- Không cho chuyển nhân viên sang "Đã nghỉ" khi họ **còn ca làm việc đang mở** (BR-10) —
  phải đóng ca trước.

---

## SCR-A12 — Cấu hình hệ thống

| | |
|---|---|
| **URL** | `GET/POST /Settings` |
| **Quyền** | **Chỉ Admin** |
| **Yêu cầu** | FR-A07, BR-01, BR-03, BR-04, BR-05 |

### Các nhóm cấu hình

**1. Thông tin khách sạn** — tên, địa chỉ, điện thoại, mã số thuế, logo (in trên hóa đơn).

**2. Giờ chuẩn (BR-01)**
| Tham số | Mặc định |
|---|---|
| Giờ nhận phòng chuẩn | 14:00 |
| Giờ trả phòng chuẩn | 12:00 |

**3. Thuế & làm tròn (BR-04)** — VAT (%) mặc định 8; đơn vị làm tròn mặc định 1.000 ₫.

**4. Phụ thu (BR-03)** — 7 dòng tham số tương ứng bảng BR-03, mỗi dòng gồm mốc giờ và
tỷ lệ % giá đêm (riêng "trả sau 18:00" cấu hình là *tính thêm 1 đêm*).

**5. Chính sách cọc & hủy (BR-05)**
| Tham số | Mặc định |
|---|---|
| Mức cọc đề xuất | Tiền 1 đêm |
| Giờ hết hạn giữ chỗ đơn chưa cọc | 18:00 ngày đến |
| Phí hủy khi hủy ≥ 48 giờ | 0% |
| Phí hủy khi hủy 24–48 giờ | 50% cọc |
| Phí hủy khi hủy < 24 giờ / no-show | 100% cọc |

**6. Hạn mức nghiệp vụ**
| Tham số | Mặc định |
|---|---|
| Hạn mức giảm giá của Lễ tân | 200.000 ₫ hoặc 10% hóa đơn |
| Cho phép bán dịch vụ khi hết tồn kho | Không |

### Quy tắc
- Mọi thay đổi cấu hình đều **ghi audit log** giá trị cũ → mới.
- Thay đổi giờ chuẩn và mức phụ thu **chỉ áp dụng cho lượt lưu trú mới**; các Stay đang mở
  giữ nguyên tham số đã chốt lúc check-in để tránh đổi tiền của khách giữa chừng.
- Màn hình có nút **Khôi phục mặc định** cho từng nhóm, kèm hộp thoại xác nhận.
