# 06 — Dịch vụ & Buồng phòng (Nhóm E)

> Đây là nhóm màn hình **duy nhất mà vai trò Buồng phòng được thao tác**. Nguyên tắc thiết kế:
> giao diện đơn giản, nút to, ít chữ — nhân viên buồng phòng thường dùng trên điện thoại hoặc
> máy tính bảng khi đang đi hành lang. Mọi thông tin về tiền và khách đều bị ẩn với vai trò này,
> trừ số lượng hàng minibar cần ghi.

---

## SCR-E01 — Bảng trạng thái buồng phòng

| | |
|---|---|
| **URL** | `GET /Housekeeping` |
| **Quyền** | Admin, Lễ tân, Buồng phòng (nội dung khác nhau) |
| **Yêu cầu** | FR-E04, FR-E05 |

### Bố cục
Lưới phòng nhóm theo tầng, mỗi ô là một phòng:

```
Tầng 2
┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
│  201   │ │  202   │ │  203   │ │  205   │
│ CHỜ DỌN│ │ ĐANG Ở │ │  TRỐNG │ │ BẢO TRÌ│
│ 2 giờ  │ │        │ │        │ │ Hỏng ĐH│
│[Bắt đầu│ │        │ │        │ │        │
│  dọn]  │ │        │ │        │ │        │
└────────┘ └────────┘ └────────┘ └────────┘
```

### Sắp xếp ưu tiên
1. Phòng `Dirty` **vừa check-out** và **có khách sẽ nhận trong hôm nay** — nhãn đỏ "Gấp".
2. Phòng `Dirty` khác, cũ nhất lên trước (hiển thị "chờ dọn X giờ").
3. Phòng `Maintenance`.
4. Các phòng còn lại.

### Bộ lọc
Tầng · Trạng thái · Hộp kiểm "Chỉ hiện phòng cần xử lý" (mặc định bật với vai trò Buồng phòng).

### Thao tác và phân quyền

| Nút | Điều kiện hiện | Admin | Lễ tân | Buồng phòng |
|---|---|:-:|:-:|:-:|
| Bắt đầu dọn | Phòng `Dirty` | ✔ | ✔ | ✔ |
| Hoàn thành dọn | Đang dọn | ✔ | ✔ | ✔ |
| Báo hỏng | Mọi phòng không có khách | ✔ | ✔ | ✔ |
| Đánh dấu đã sửa xong | Phòng `Maintenance` | ✔ | ✔ | — |
| Ngừng khai thác | Mọi phòng trống | ✔ | — | — |
| Xem khách đang ở | Phòng `Occupied` | ✔ | ✔ | **Ẩn** |

### Luồng "Bắt đầu dọn → Hoàn thành dọn" (FR-E05)
1. Nhấn **Bắt đầu dọn** → tạo `HousekeepingTask`: phòng, người dọn = người đăng nhập,
   thời điểm bắt đầu, trạng thái `InProgress`. Ô phòng đổi sang nhãn "Đang dọn — Nguyễn Thị C".
2. Nhấn **Hoàn thành dọn** → ghi thời điểm kết thúc, trạng thái `Done`,
   **phòng chuyển `Available`** và lập tức bán được ở SCR-C02.
3. Nếu trong lúc dọn phát hiện hỏng hóc → nút **Báo hỏng** (SCR-E03), phòng chuyển `Maintenance`
   thay vì `Available`.

### Quy tắc
- Một phòng chỉ có **một tác vụ dọn đang mở** tại một thời điểm; người thứ hai nhấn "Bắt đầu dọn"
  sẽ thấy thông báo ai đang dọn.
- Không cho chuyển `Occupied` → `Available` từ màn hình này (BR: chỉ check-out mới làm việc đó).
- Thời gian dọn được lưu để làm báo cáo năng suất buồng phòng (FR-G06).

### Phân quyền hiển thị với vai trò Buồng phòng
Ô phòng `Occupied` chỉ hiện chữ "Đang ở" — **không hiện tên khách, không hiện ngày đi,
không hiện tiền**. Dữ liệu này cũng không được gửi xuống client (tránh xem qua mã nguồn trang).

---

## SCR-E02 — Kiểm minibar khi trả phòng

| | |
|---|---|
| **URL** | `GET/POST /Housekeeping/MinibarUsage/{stayId}` |
| **Quyền** | Admin, Lễ tân, Buồng phòng |
| **Yêu cầu** | FR-E06, BR-08, BR-12 |

### Mục đích
Nhân viên buồng phòng lên kiểm phòng trước khi khách rời đi, ghi các mặt hàng minibar khách đã
dùng; hệ thống đẩy thẳng vào folio để lễ tân thu tiền (BR-08 — đây là điều kiện để check-out).

### Nội dung
- Tiêu đề: số phòng + tên khách (**ẩn tên khách với vai trò Buồng phòng**, chỉ hiện số phòng).
- Danh sách mặt hàng minibar (dịch vụ thuộc nhóm *Minibar*), mỗi dòng có nút `−` / `+`
  và ô số lượng, mặc định 0. Nút to, bấm được trên điện thoại.
- Ô ghi chú tình trạng phòng: hư hỏng tài sản, mất đồ, cần thay đồ vải.
- Nút **Xác nhận đã kiểm phòng**.

### Luồng xử lý
1. Với mỗi mặt hàng số lượng > 0 → thêm `FolioItem` loại *Dịch vụ*, đơn giá lấy từ danh mục
   tại thời điểm ghi, người ghi = người đăng nhập.
2. **Trừ tồn kho** tương ứng (BR-12); nếu tồn không đủ vẫn cho ghi (hàng đã bị khách dùng rồi —
   đây là trường hợp tồn kho sổ sách bị lệch) nhưng **cảnh báo** và ghi audit log để Admin
   kiểm kê lại ở SCR-A09.
3. Đánh dấu Stay là **đã kiểm phòng** → mở khóa nút Check-out ở SCR-D08.
4. Ghi nhận người kiểm và thời điểm.

### Quy tắc
- Kiểm phòng lại lần hai (khách dùng thêm sau khi kiểm) → được phép, các dòng thêm mới cộng dồn,
  không ghi đè lần trước.
- Sau khi folio đã khóa (khách đang thanh toán), màn hình này **chỉ đọc**; muốn thêm phải
  nhờ lễ tân mở khóa folio (chỉ Admin — xem SCR-F02).
- Với vai trò Buồng phòng: **không hiển thị đơn giá và thành tiền**, chỉ hiện tên hàng và
  số lượng. Việc của họ là đếm, không phải tính tiền.

---

## SCR-E03 — Tạo yêu cầu (báo hỏng / phục vụ)

| | |
|---|---|
| **URL** | `GET/POST /Housekeeping/CreateRequest` |
| **Quyền** | Admin, Lễ tân, Buồng phòng |
| **Yêu cầu** | FR-E07, FR-E08 |

### Các trường

| Trường | Bắt buộc | Ghi chú |
|---|:-:|---|
| Loại yêu cầu | ✔ | **Báo hỏng / bảo trì** hoặc **Yêu cầu phục vụ** (thêm khăn, thêm nước, gọi đồ) |
| Phòng | ✔ | Chọn từ danh sách |
| Mô tả | ✔ | ≥ 10 ký tự |
| Mức độ | ✔ | Thấp / Trung bình / **Khẩn cấp** |
| Người xử lý | | Để trống = chưa phân công |

### Quy tắc theo loại yêu cầu

**Báo hỏng / bảo trì (FR-E07)**
- Nếu phòng **không có khách** → hỏi "Chuyển phòng sang trạng thái Bảo trì?"; đồng ý thì
  phòng chuyển `Maintenance` và **ngừng bán** ngay.
- Nếu phòng **đang có khách** → không đổi trạng thái phòng (không thể đuổi khách), tạo yêu cầu
  mức khẩn cấp và **nhắc lễ tân cân nhắc đổi phòng** cho khách (SCR-D06).
- Nếu phòng có **đơn đặt trong tương lai** → cảnh báo kèm danh sách đơn cần xếp lại.

**Yêu cầu phục vụ (FR-E08)**
- Không đổi trạng thái phòng.
- Nếu yêu cầu có phát sinh chi phí (gọi đồ ăn) → sau khi hoàn thành, người xử lý được nhắc
  ghi dịch vụ vào folio (SCR-F03).

---

## SCR-E04 — Danh sách yêu cầu

| | |
|---|---|
| **URL** | `GET /Housekeeping/Requests` |
| **Quyền** | Admin, Lễ tân, Buồng phòng |
| **Yêu cầu** | FR-E07, FR-E08 |

### Cột
Mã YC · Loại · Phòng · Mô tả · Mức độ · Trạng thái (Mới / Đang xử lý / Hoàn thành / Hủy) ·
Người tạo · Người xử lý · Thời gian tạo · Thời gian hoàn thành · Thao tác.

### Bộ lọc
Loại · Trạng thái (mặc định: chưa hoàn thành) · Phòng · Mức độ.
Yêu cầu **Khẩn cấp** luôn hiện đầu danh sách, nền đỏ nhạt.

### Thao tác

| Nút | Điều kiện | Admin | Lễ tân | Buồng phòng |
|---|---|:-:|:-:|:-:|
| Nhận xử lý | Trạng thái Mới | ✔ | ✔ | ✔ |
| Hoàn thành | Đang xử lý | ✔ | ✔ | ✔ (yêu cầu mình nhận) |
| Phân công cho người khác | Mọi trạng thái mở | ✔ | ✔ | — |
| Hủy yêu cầu | Mọi trạng thái mở | ✔ | ✔ | — |
| Đánh dấu sửa xong & mở bán lại phòng | Yêu cầu bảo trì hoàn thành | ✔ | ✔ | — |

### Luồng đóng yêu cầu bảo trì
1. Nhấn **Hoàn thành** → ghi thời điểm và người xử lý.
2. Nếu phòng đang `Maintenance` → hỏi "Phòng đã sửa xong, chuyển sang Chờ dọn?".
3. Đồng ý → phòng chuyển **`Dirty`** (không chuyển thẳng `Available`: sửa xong thì phòng bẩn,
   phải dọn lại rồi mới bán — đúng máy trạng thái ở REQUIREMENTS mục 4.1).
4. Ghi audit log.
