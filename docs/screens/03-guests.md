# 03 — Khách hàng (Nhóm B)

> Nguyên tắc chung: cả Admin và Lễ tân đều dùng nhóm này, nhưng **thao tác đưa vào / gỡ khỏi
> danh sách hạn chế chỉ dành cho Admin**. Đây là dữ liệu cá nhân nhạy cảm (số CCCD/Passport)
> nên số giấy tờ bị che một phần ở màn hình danh sách, và mọi truy cập vào hồ sơ khách đều
> ghi audit log ở mức đọc.

---

## SCR-B01 — Danh sách khách

| | |
|---|---|
| **URL** | `GET /Guests` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-B01, FR-B02 |

### Cột hiển thị
Họ tên · Giới tính · Số CCCD/Passport (**che một phần**, ví dụ `0012****5678`) · SĐT ·
Quốc tịch · Số lần lưu trú · Lần ở gần nhất · Nhãn (Khách quen / Hạn chế) · Thao tác.

### Bộ lọc & tìm kiếm
- Ô tìm kiếm chung: khớp theo **họ tên, SĐT hoặc số giấy tờ**.
- Lọc: quốc tịch · có/không trong danh sách hạn chế · khoảng thời gian lưu trú gần nhất.

### Thao tác
| Nút | Quyền |
|---|---|
| Thêm khách | Admin, Lễ tân |
| Xem chi tiết | Admin, Lễ tân → SCR-B03 |
| Sửa | Admin, Lễ tân |
| Đưa vào / gỡ khỏi danh sách hạn chế | **Chỉ Admin** → SCR-B05 |
| Xóa | **Không ai** — hồ sơ khách không bao giờ xóa cứng |

### Quy tắc hiển thị số giấy tờ
Danh sách chỉ hiện số giấy tờ dạng che. Số đầy đủ chỉ hiện ở SCR-B03 và màn hình check-in,
nơi lễ tân thực sự cần đối chiếu với giấy tờ trên tay khách.

---

## SCR-B02 — Thêm / sửa hồ sơ khách

| | |
|---|---|
| **URL** | `GET/POST /Guests/Create`, `/Guests/Edit/{id}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-B01, FR-B02 |

### Các trường

| Trường | Bắt buộc | Kiểm tra |
|---|:-:|---|
| Họ tên | ✔ | ≤ 100 ký tự |
| Loại giấy tờ | ✔ | CCCD / CMND / Hộ chiếu |
| Số giấy tờ | ✔ | **Duy nhất**; CCCD phải đủ 12 chữ số; hộ chiếu 6–20 ký tự chữ-số |
| Ngày sinh | | Không được ở tương lai; cảnh báo nếu khách dưới 18 tuổi |
| Giới tính | | Nam / Nữ / Khác |
| Quốc tịch | ✔ | Mặc định Việt Nam |
| Số điện thoại | ✔ | Đúng định dạng; cảnh báo (không chặn) nếu trùng khách khác |
| Email | | Đúng định dạng nếu có |
| Địa chỉ thường trú | ✔ với khách Việt Nam | Cần cho khai báo tạm trú (FR-B05) |
| Ghi chú | | Ví dụ: "thích phòng tầng cao, không hút thuốc" |

### Chống tạo trùng hồ sơ
1. Khi rời khỏi ô **Số giấy tờ** hoặc **SĐT**, hệ thống tra cứu ngay (AJAX).
2. Nếu có hồ sơ khớp → hiện khối gợi ý: "Đã có khách *Nguyễn Văn B* với giấy tờ này"
   kèm hai nút: **Dùng hồ sơ có sẵn** (chuyển sang hồ sơ đó) hoặc **Vẫn tạo mới**.
3. Trùng **số giấy tờ** thì **chặn cứng** không cho lưu trùng; trùng SĐT chỉ cảnh báo
   (người nhà dùng chung số là chuyện bình thường).

### Quy tắc sửa
- Sửa số giấy tờ của khách **đang lưu trú** → cảnh báo và ghi audit log (thường là sửa lỗi nhập sai).
- Không cho sửa hồ sơ khách sang trùng số giấy tờ của hồ sơ khác → gợi ý gộp hồ sơ thủ công.

---

## SCR-B03 — Chi tiết khách & lịch sử lưu trú

| | |
|---|---|
| **URL** | `GET /Guests/Details/{id}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-B03, FR-B06 |

### Bố cục

```
┌─ Thẻ thông tin khách ────────────────────────────────────────┐
│ Họ tên · Giấy tờ (đầy đủ) · SĐT · Quốc tịch · Địa chỉ       │
│ [Nhãn: Khách quen — 7 lần ở]  [Nhãn đỏ: Hạn chế (nếu có)]   │
├─ Thẻ thống kê ───────────────────────────────────────────────┤
│ [Số lần lưu trú] [Tổng số đêm] [Tổng chi tiêu] [Lần cuối]   │
├─ Tab: Lịch sử lưu trú | Đơn đặt phòng | Ghi chú ────────────┤
│ Ngày vào · Ngày ra · Phòng · Số đêm · Tổng hóa đơn · Xem    │
└──────────────────────────────────────────────────────────────┘
```

### Quy tắc dữ liệu
- **Tổng chi tiêu** chỉ cộng hóa đơn trạng thái `Settled`, bỏ qua hóa đơn `Void`.
- Nhãn **Khách quen** tự gán khi số lần lưu trú hoàn tất ≥ 3 (tham số cấu hình).
- Nhấp vào một dòng lịch sử → mở hóa đơn tương ứng (SCR-F06) ở chế độ chỉ đọc.

### Phân quyền chi tiết
| Thành phần | Admin | Lễ tân |
|---|:-:|:-:|
| Thông tin cá nhân đầy đủ | ✔ | ✔ |
| Lịch sử lưu trú | ✔ | ✔ |
| Tổng chi tiêu, xem lại hóa đơn cũ | ✔ | ✔ (chỉ đọc) |
| Nút đưa vào danh sách hạn chế | ✔ | Ẩn |
| Xem lý do bị đưa vào danh sách hạn chế | ✔ | Chỉ thấy nhãn, không thấy lý do chi tiết |

---

## SCR-B04 — Khai báo tạm trú theo ngày

| | |
|---|---|
| **URL** | `GET /Guests/Residence?date=...` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-B05 |

### Mục đích
Kết xuất danh sách khách đang lưu trú trong một ngày để nộp cho công an khu vực theo quy định
về khai báo tạm trú.

### Nội dung
- Bộ chọn ngày (mặc định hôm nay) và nút **Xuất file**.
- Bảng: STT · Họ tên · Ngày sinh · Giới tính · Quốc tịch · Loại & số giấy tờ ·
  Địa chỉ thường trú · Số phòng · Thời gian từ · Thời gian đến.
- **Bao gồm cả khách ở cùng phòng** (`StayGuest`), không chỉ người đứng tên đơn (FR-B04).

### Quy tắc
- Tiêu chí lọc: mọi Stay có khoảng lưu trú **giao với ngày được chọn**.
- Khách thiếu thông tin bắt buộc (ngày sinh, địa chỉ) được **tô đỏ** để lễ tân bổ sung trước khi xuất.
- Xuất ra Excel/CSV theo mẫu; mỗi lần xuất ghi audit log (ai xuất dữ liệu cá nhân, lúc nào).

---

## SCR-B05 — Đưa vào / gỡ khỏi danh sách hạn chế

| | |
|---|---|
| **URL** | `POST /Guests/Blacklist/{id}` (hộp thoại) |
| **Quyền** | **Chỉ Admin** |
| **Yêu cầu** | FR-B06, BR-11 |

### Các trường
Hành động (Đưa vào / Gỡ khỏi) · **Lý do** `*` (bắt buộc, ≥ 10 ký tự) · Ghi chú.

### Hệ quả nghiệp vụ
- Khi tạo đơn đặt phòng hoặc check-in cho khách trong danh sách hạn chế (SCR-C04, SCR-D02,
  hệ thống hiện **cảnh báo đỏ** kèm nhắc "cần quản lý duyệt". (SCR-D03 đã bỏ nên không còn áp dụng ở đó.)
- Cảnh báo **không tự chặn** giao dịch — quyết định nhận hay từ chối khách là của con người,
  nhưng nếu lễ tân vẫn tiếp tục thì hệ thống ghi audit log việc bỏ qua cảnh báo.
- Mọi thao tác thêm/gỡ khỏi danh sách đều ghi audit log kèm lý do.
