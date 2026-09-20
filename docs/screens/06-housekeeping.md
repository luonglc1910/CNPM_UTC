# 06 — Dịch vụ & Buồng phòng (Nhóm E)

> **Hệ thống không có vai trò Buồng phòng.** Nhân viên dọn phòng vẫn làm việc ngoài thực tế
> nhưng không có tài khoản đăng nhập: dọn xong hoặc phát hiện hỏng hóc thì báo về quầy
> (miệng hoặc bộ đàm), **lễ tân bấm cập nhật hộ** trên các màn hình dưới đây.
>
> Hệ quả thiết kế: nhóm này giờ được dùng ngay tại quầy lễ tân chứ không phải trên điện thoại
> ngoài hành lang, nên không cần giao diện rút gọn và không cần ẩn thông tin tiền/khách.
> Trường "người dọn" của `HousekeepingTask` vì vậy ghi lại **người bấm xác nhận**, không phải
> người thực sự cầm chổi — đừng dùng nó để đo năng suất (xem SCR-G04).

---

## SCR-E01 — Bảng trạng thái buồng phòng

| | |
|---|---|
| **URL** | `GET /Housekeeping` |
| **Quyền** | Admin, Lễ tân |
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
Tầng · Trạng thái · Hộp kiểm "Chỉ hiện phòng cần xử lý" (mặc định bật).

### Thao tác và phân quyền

| Nút | Điều kiện hiện | Admin | Lễ tân |
|---|---|:-:|:-:|
| Bắt đầu dọn | Phòng `Dirty` | ✔ | ✔ |
| Hoàn thành dọn | Đang dọn | ✔ | ✔ |
| Báo hỏng | Mọi phòng không có khách | ✔ | ✔ |
| Đánh dấu đã sửa xong | Phòng `Maintenance` | ✔ | ✔ |
| Ngừng khai thác | Mọi phòng trống | ✔ | ✔ (bắt buộc nhập lý do — SCR-A05) |
| Xem khách đang ở | Phòng `Occupied` | ✔ | ✔ |

### Luồng "Bắt đầu dọn → Hoàn thành dọn" (FR-E05)
1. Nhấn **Bắt đầu dọn** → tạo `HousekeepingTask`: phòng, người xác nhận = người đăng nhập,
   thời điểm bắt đầu, trạng thái `InProgress`. Ô phòng đổi sang nhãn "Đang dọn".
2. Nhấn **Hoàn thành dọn** → ghi thời điểm kết thúc, trạng thái `Done`,
   **phòng chuyển `Available`** và lập tức bán được ở SCR-C02.
3. Nếu trong lúc dọn phát hiện hỏng hóc → nút **Báo hỏng** (SCR-E03), phòng chuyển `Maintenance`
   thay vì `Available`.

### Quy tắc
- Một phòng chỉ có **một tác vụ dọn đang mở** tại một thời điểm; người thứ hai nhấn "Bắt đầu dọn"
  sẽ thấy thông báo ai đang xử lý.
- Không cho chuyển `Occupied` → `Available` từ màn hình này (BR: chỉ check-out mới làm việc đó).
- Thời gian giữa hai mốc được lưu để tính **thời gian phòng nằm chờ dọn** — chỉ số này thuộc
  báo cáo công suất (SCR-G02), không dùng để đo năng suất cá nhân.

---

## SCR-E02 — Kiểm minibar khi trả phòng

| | |
|---|---|
| **URL** | `GET/POST /Housekeeping/MinibarUsage/{stayId}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-E06, BR-08, BR-12 |

### Mục đích
Nhân viên dọn phòng lên kiểm phòng trước khi khách rời đi rồi báo về quầy; lễ tân nhập các mặt
hàng minibar khách đã dùng. Hệ thống đẩy thẳng vào folio để thu tiền — đây là **điều kiện bắt
buộc để check-out** (BR-08), và là chốt chặn duy nhất ngăn việc quên thu tiền minibar.

### Nội dung
- Tiêu đề: số phòng + tên khách.
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
- Màn hình hiện cả đơn giá và thành tiền để lễ tân đối chiếu ngay với số khách khai báo —
  không còn vai trò nào cần giấu thông tin tiền.

---

## SCR-E03 — Tạo yêu cầu (báo hỏng / phục vụ)

| | |
|---|---|
| **URL** | `GET/POST /Housekeeping/CreateRequest` |
| **Quyền** | Admin, Lễ tân |
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
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-E07, FR-E08 |

### Cột
Mã YC · Loại · Phòng · Mô tả · Mức độ · Trạng thái (Mới / Đang xử lý / Hoàn thành / Hủy) ·
Người tạo · Người xử lý · Thời gian tạo · Thời gian hoàn thành · Thao tác.

### Bộ lọc
Loại · Trạng thái (mặc định: chưa hoàn thành) · Phòng · Mức độ.
Yêu cầu **Khẩn cấp** luôn hiện đầu danh sách, nền đỏ nhạt.

### Thao tác

| Nút | Điều kiện | Admin | Lễ tân |
|---|---|:-:|:-:|
| Nhận xử lý | Trạng thái Mới | ✔ | ✔ |
| Hoàn thành | Đang xử lý | ✔ | ✔ |
| Phân công cho người khác | Mọi trạng thái mở | ✔ | ✔ |
| Hủy yêu cầu | Mọi trạng thái mở | ✔ | ✔ |
| Đánh dấu sửa xong & mở bán lại phòng | Yêu cầu bảo trì hoàn thành | ✔ | ✔ |

### Luồng đóng yêu cầu bảo trì
1. Nhấn **Hoàn thành** → ghi thời điểm và người xử lý.
2. Nếu phòng đang `Maintenance` → hỏi "Phòng đã sửa xong, chuyển sang Chờ dọn?".
3. Đồng ý → phòng chuyển **`Dirty`** (không chuyển thẳng `Available`: sửa xong thì phòng bẩn,
   phải dọn lại rồi mới bán — đúng máy trạng thái ở REQUIREMENTS mục 4.1).
4. Ghi audit log.
