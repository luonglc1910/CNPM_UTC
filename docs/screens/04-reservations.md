# 04 — Đặt phòng (Nhóm C)

> Nguyên tắc chung: Admin và Lễ tân có quyền **như nhau** ở toàn bộ nhóm này — lễ tân phải tự
> chủ được việc đặt phòng. Ngoại lệ duy nhất là thao tác miễn phí hủy (SCR-C08), chỉ Admin.

---

## SCR-C01 — Danh sách đơn đặt phòng

| | |
|---|---|
| **URL** | `GET /Reservations` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-C09 |

### Cột hiển thị
Mã đơn · Khách đứng tên · SĐT · Ngày đến · Ngày đi · Số đêm · Số phòng · Loại phòng ·
Tổng tiền dự kiến · Đã cọc · Trạng thái · Nguồn đặt · Thao tác.

### Bộ lọc
Khoảng ngày đến · Khoảng ngày đi · Trạng thái (Draft / Confirmed / CheckedIn / CheckedOut /
Cancelled / NoShow) · Nguồn đặt · Ô tìm theo mã đơn, tên khách, SĐT.

Mặc định: hiển thị đơn có ngày đến **từ hôm nay trở đi**, trạng thái `Confirmed` — vì đó là
việc lễ tân cần xử lý; các bộ lọc khác phải chọn chủ động.

### Huy hiệu trạng thái
`Draft` xám · `Confirmed` xanh dương · `CheckedIn` cam · `CheckedOut` xanh lá ·
`Cancelled` đỏ nhạt · `NoShow` đỏ đậm.

### Thao tác theo trạng thái đơn

| Trạng thái | Nút hiện ra | Quyền |
|---|---|---|
| `Draft` | Xem · Sửa · Xác nhận · Thu cọc · Xóa | Admin, Lễ tân |
| `Confirmed` | Xem · Sửa · Thu cọc · **Check-in** · Hủy · Đánh dấu No-show | Admin, Lễ tân |
| `CheckedIn` | Xem · Mở lượt lưu trú (SCR-D04) | Admin, Lễ tân |
| `CheckedOut` | Xem · Xem hóa đơn | Admin, Lễ tân |
| `Cancelled`, `NoShow` | Xem (chỉ đọc) | Admin, Lễ tân |

Nút **Check-in** chỉ bật khi ngày đến ≤ hôm nay (BR-07). Đến sớm hơn ngày đặt thì nút mờ,
tooltip: "Đơn này đến ngày 22/09, cần chuyển sang nhận phòng sớm nếu khách đến trước."

---

## SCR-C02 — Tra cứu phòng trống

| | |
|---|---|
| **URL** | `GET /Reservations/Availability` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-C01, BR-02, BR-06 |

### Mục đích
Trả lời trong một thao tác: *"Từ ngày X đến ngày Y, còn phòng nào, giá bao nhiêu?"*

### Form tìm kiếm
| Trường | Bắt buộc | Kiểm tra |
|---|:-:|---|
| Ngày đến | ✔ | Không nhỏ hơn hôm nay (Admin được phép chọn quá khứ để tra cứu) |
| Ngày đi | ✔ | **Phải lớn hơn ngày đến**; tối thiểu 1 đêm |
| Số khách | ✔ | ≥ 1 |
| Loại phòng | | Bỏ trống = tất cả |

### Kết quả
Nhóm theo loại phòng:

```
Deluxe — còn 4 / 10 phòng — 1.200.000 ₫/đêm × 3 đêm = 3.600.000 ₫
   Phòng trống: 201, 203, 305, 402        [Chọn đặt]
```

### Thuật toán xác định phòng trống (BR-06)
Một phòng là **trống** trong khoảng `[đến, đi)` khi đồng thời:
1. Trạng thái không phải `Maintenance` và không phải `OutOfService`.
2. Không có `Reservation` trạng thái `Confirmed`/`CheckedIn` nào **chồng lấn** khoảng thời gian.
3. Không có `Stay` đang mở nào chồng lấn khoảng thời gian.

Điều kiện chồng lấn: `existing.CheckIn < requested.CheckOut AND existing.CheckOut > requested.CheckIn`
(ngày trả và ngày nhận **trùng nhau thì không tính là chồng lấn** — khách này đi buổi trưa,
khách kia đến buổi chiều; nhưng phòng phải kịp dọn).

### Cảnh báo hiển thị
- Phòng `Dirty` vẫn được coi là bán được cho **ngày mai trở đi**, nhưng nếu đặt cho **hôm nay**
  thì hiện nhãn "cần dọn trước khi nhận khách".
- Số khách vượt sức chứa chuẩn của loại phòng → hiện dòng phụ thu thêm người dự kiến (BR-03).
- Không còn phòng nào → thông báo kèm gợi ý khoảng ngày gần nhất còn phòng.

### Thao tác
Nút **Chọn đặt** chuyển sang SCR-C04 mang theo sẵn ngày, loại phòng và phòng đã chọn.

---

## SCR-C03 — Tình trạng phòng theo ngày

| | |
|---|---|
| **URL** | `GET /Reservations/RoomChart?from=...&days=14` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-C02 |

### Hiển thị
Ma trận **phòng × ngày** (mặc định 14 ngày):

```
Phòng │ 20/09 │ 21/09 │ 22/09 │ 23/09 │ ...
 201  │███ Nguyễn Văn A ███│       │      │   ← dải liền = một lượt đặt
 203  │       │███ Trần B ███████████│      │
 305  │  BẢO TRÌ (đến 25/09)         │      │
```

- Mỗi ô màu theo trạng thái; một đơn nhiều đêm vẽ thành **một dải liền** để dễ nhìn khoảng trống.
- Di chuột lên dải → tooltip: mã đơn, khách, số khách, trạng thái.
- Nhấp vào ô trống → mở nhanh SCR-C04 với phòng và ngày đã điền sẵn.
- **Thuê theo giờ (BR-13)** nằm gọn trong một ngày nên không chiếm trọn đêm nào. Lưới mỗi ô là
  một đêm, không vẽ được từng khung giờ, nên ngày đó hiện ô **kẻ sọc** với nhãn *"N lượt giờ"*;
  tooltip liệt kê từng khung giờ kèm tên khách, nhấp vào mở danh sách đơn của đúng ngày đó.
  Ô kẻ sọc **không bao giờ gộp** sang ngày khác vì số lượt là của riêng từng ngày.
- **Qua đêm** chiếm trọn đêm của ngày mở gói nên vẽ như đơn theo ngày bình thường.

### Ghi chú
Sơ đồ này cũng là chỗ nhìn nhanh lịch dọn phòng: ô vừa kết thúc một dải đặt phòng chính là
phòng sắp chuyển sang `Dirty`.

---

## SCR-C04 — Tạo đơn đặt phòng

| | |
|---|---|
| **URL** | `GET/POST /Reservations/Create` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-C03, FR-C04, BR-05, BR-06 |

### Bố cục — form 4 khối

**Khối 1 — Khách đứng tên**
- Ô tìm khách theo CCCD/SĐT/tên (gợi ý tức thời từ hồ sơ đã có — FR-B02).
- Nếu chưa có: nút **Tạo khách mới** mở hộp thoại nhập nhanh (họ tên, giấy tờ, SĐT bắt buộc).
- Khách trong danh sách hạn chế → cảnh báo đỏ (SCR-B05).

**Khối 2 — Hình thức thuê, thời gian & phòng**
- **Hình thức thuê** `*` là trường đầu tiên — ba lựa chọn **Theo ngày / Theo giờ / Qua đêm**
  (BR-13). Nó quyết định luôn kiểu của hai ô thời gian bên dưới, nên phải chọn trước.
- Ô thời gian đổi theo hình thức:

| Hình thức | Ô nhập | Giá trị hệ thống lưu |
|---|---|---|
| Theo ngày | Ngày đến `*`, ngày đi `*` | ngày đến + giờ nhận chuẩn → ngày đi + giờ trả chuẩn; tính **số đêm** (BR-02) |
| Theo giờ | **Chỉ ngày** `*` — không có ô giờ nào | mốc kỹ thuật một giờ; tiền tính từ **giờ check-in** đến giờ trả phòng |
| Qua đêm | **Đêm ngày** `*` | 22:00 ngày đó → 10:00 hôm sau, hai mốc lấy từ cấu hình |

- Bảng dòng phòng, **thêm được nhiều dòng** (FR-C03): mỗi dòng gồm loại phòng · phòng cụ thể
  (chọn hoặc để hệ thống xếp sau) · số người lớn · số trẻ em · giá/đêm (tự điền, Admin sửa được).
  Giá giờ và giá qua đêm lấy thẳng từ bảng giá loại phòng, không có ô nhập tay.
- Mỗi lần thêm/đổi dòng, hệ thống kiểm tra lại xung đột (BR-06) ngay trên giao diện.

**Khối 3 — Thông tin đơn**
- Nguồn đặt `*`: Điện thoại / Trực tiếp tại quầy / Giới thiệu / Khác.
- Yêu cầu đặc biệt (văn bản tự do: phòng tầng cao, giường đôi, nôi em bé...).
- Ghi chú nội bộ.

**Khối 4 — Tiền (chỉ hiển thị, tính tự động)**
| Dòng | Cách tính |
|---|---|
| Tiền phòng — theo ngày | Σ (giá/đêm × số đêm) của các dòng phòng |
| Tiền phòng — qua đêm | Σ giá gói qua đêm của các dòng phòng |
| Tiền phòng — theo giờ | Chỉ là **mức tối thiểu một giờ** (giá giờ đầu); số thật chốt lúc trả phòng |
| Phụ thu thêm người dự kiến | Theo BR-03 nếu vượt sức chứa chuẩn — chỉ áp cho thuê theo ngày |
| **Tổng dự kiến** | Tổng các dòng trên (chưa VAT — VAT chốt ở hóa đơn) |

> Tính năng **đặt cọc đã bị vô hiệu** — `BuildDepositFormAsync`/`TakeDepositAsync` trả về rỗng,
> màn hình SCR-C07 không còn dùng. Bảng `Deposits` giữ lại để đọc dữ liệu cũ.

### Kiểm tra khi lưu
1. Theo ngày: ngày đi > ngày đến. Theo giờ và qua đêm: không có ô ngày đi nên không kiểm.
2. Có ít nhất một dòng phòng.
3. Số khách mỗi dòng ≤ sức chứa tối đa của loại phòng → nếu vượt thì **chặn**.
4. Không có phòng nào bị trùng lịch (BR-06) — kiểm tra lại ở server, **không tin giao diện**,
   vì có thể có lễ tân khác vừa đặt mất phòng đó.
5. Nếu phát hiện trùng lúc lưu → giữ nguyên dữ liệu đã nhập, báo rõ phòng nào trùng với đơn nào.

### Sau khi lưu
- Sinh **mã đơn** duy nhất dạng `RSV-yyMMdd-####` (FR-C04).
- Trạng thái ban đầu: `Confirmed` (nếu nhấn *Lưu và xác nhận*) hoặc `Draft` (nếu nhấn *Lưu nháp*).
- Chuyển sang SCR-C05, hiện thông báo thành công và gợi ý nút **Thu cọc**.
- Đơn `Confirmed` chưa cọc sẽ hết hạn giữ chỗ lúc 18:00 ngày đến (BR-05) — màn hình chi tiết
  hiện rõ dòng nhắc này.

---

## SCR-C05 — Chi tiết đơn đặt phòng

| | |
|---|---|
| **URL** | `GET /Reservations/Details/{id}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-C09, FR-C10 |

### Nội dung
- Khối tóm tắt: mã đơn · trạng thái · khách đứng tên · liên hệ · nguồn đặt · người tạo · ngày tạo.
- Bảng dòng phòng: phòng · loại · số khách · giá/đêm · thành tiền.
- Khối tiền: tổng dự kiến · đã cọc · còn lại dự kiến.
- Khối lịch sử thay đổi đơn: sửa ngày, đổi phòng, thu cọc, hủy — kèm người thực hiện và thời điểm.

### Thanh nút (hiện theo trạng thái, xem bảng ở SCR-C01)
Sửa · Thu cọc · Check-in · Hủy · Đánh dấu No-show · **In phiếu xác nhận** (FR-C10).

### In phiếu xác nhận đặt phòng
Bản in gồm: thông tin khách sạn, mã đơn, tên khách, ngày đến/đi, loại phòng, số khách,
tổng tiền dự kiến, số tiền đã cọc, **chính sách hủy** (trích từ cấu hình BR-05).
Không in ghi chú nội bộ.

---

## SCR-C06 — Sửa đơn đặt phòng

| | |
|---|---|
| **URL** | `GET/POST /Reservations/Edit/{id}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-C06, BR-06 |

### Phạm vi được sửa theo trạng thái

| Trạng thái đơn | Được sửa |
|---|---|
| `Draft`, `Confirmed` | Ngày đến/đi, dòng phòng, số khách, yêu cầu đặc biệt, khách đứng tên |
| `CheckedIn` | **Không sửa ở đây** — dùng SCR-D06 (đổi phòng) / SCR-D07 (gia hạn) |
| `CheckedOut`, `Cancelled`, `NoShow` | Không sửa |

### Quy tắc
- Mọi thay đổi ngày hoặc phòng đều **kiểm tra lại xung đột** (BR-06).
- Đổi ngày làm thay đổi số đêm → tính lại tổng tiền dự kiến và hiển thị chênh lệch so với trước.
- Rút ngắn kỳ ở của đơn **đã cọc vượt tổng tiền mới** → cảnh báo sẽ phải hoàn cọc khi thanh toán.
- Sửa đơn ghi lịch sử thay đổi (hiển thị ở SCR-C05); sửa giá/đêm thủ công là thao tác **chỉ Admin**
  và ghi audit log (BR-11).

---

## SCR-C07 — Thu tiền cọc

| | |
|---|---|
| **URL** | `GET/POST /Reservations/Deposit/{id}` |
| **Quyền** | Admin, Lễ tân — **bắt buộc đang có ca mở** (BR-10) |
| **Yêu cầu** | FR-C05, BR-05, BR-10 |

### Các trường
| Trường | Bắt buộc | Kiểm tra |
|---|:-:|---|
| Số tiền cọc | ✔ | > 0; mặc định điền mức đề xuất; cảnh báo nếu vượt tổng tiền dự kiến |
| Phương thức | ✔ | Tiền mặt / Chuyển khoản / Thẻ |
| Mã giao dịch | ✔ khi chuyển khoản hoặc thẻ | Ghi để đối soát |
| Ghi chú | | |

### Luồng xử lý
1. Kiểm tra người dùng có ca đang mở; nếu không → chặn, hiện nút mở ca.
2. Tạo bản ghi `Deposit` + `Payment` gắn với **ca hiện tại**.
3. Cập nhật tổng đã cọc của đơn; đơn thoát khỏi diện "hết hạn giữ chỗ".
4. In **phiếu thu cọc** (có thể in lại sau ở SCR-C05).
5. Ghi audit log.

### Quy tắc
- Được thu cọc **nhiều lần** cho một đơn; màn hình liệt kê các lần đã thu.
- Không thu cọc cho đơn ở trạng thái `Cancelled`, `NoShow`, `CheckedOut`.
- Tiền cọc **chưa phải doanh thu** — chỉ ghi nhận là khoản khách đã trả trước, sẽ đối trừ
  vào hóa đơn ở SCR-F05 hoặc chuyển thành phí hủy ở SCR-C08.

---

## SCR-C08 — Hủy đơn đặt phòng

| | |
|---|---|
| **URL** | `GET/POST /Reservations/Cancel/{id}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-C07, BR-05, BR-11 |

### Nội dung màn hình
Hệ thống **tự tính và hiển thị rõ** trước khi xác nhận:

```
Đơn RSV-260920-0031 — Nguyễn Văn A — đến 22/09/2026
Thời điểm hủy: 20/09/2026 15:30  →  còn 44 giờ trước ngày đến
Chính sách áp dụng: hủy trong 24–48 giờ → thu 50% tiền cọc

Đã cọc:              1.200.000 ₫
Phí hủy (50%):         600.000 ₫
Hoàn lại khách:        600.000 ₫
```

### Các trường
**Lý do hủy** `*` (bắt buộc, chọn từ danh sách + ô nhập thêm: khách đổi lịch / khách hủy /
khách sạn không đáp ứng được / lý do khác) · Ghi chú.

### Nút
**Xác nhận hủy** (có hộp thoại xác nhận lần hai nêu rõ số tiền thu và hoàn) · Quay lại.

### Luồng xử lý (trong một transaction)
1. Tính phí hủy theo BR-05 dựa trên **thời điểm hủy thực tế** so với ngày đến.
2. Chuyển đơn sang `Cancelled`, giải phóng các phòng đã giữ.
3. Ghi nhận phí hủy thành khoản **doanh thu** (loại "Phí hủy"), sinh chứng từ thu.
4. Nếu còn tiền hoàn → tạo phiếu hoàn tiền gắn ca hiện tại; nếu không có ca mở thì
   cho phép hủy đơn nhưng **chặn phần hoàn tiền** và nhắc mở ca.
5. Ghi audit log: người hủy, lý do, số tiền thu/hoàn.

### Quyền đặc biệt
- Lễ tân hủy được đơn thông thường.
- **Miễn phí hủy** (thu 0% khác với chính sách) là thao tác **chỉ Admin**, bắt buộc nhập lý do,
  ghi audit log riêng.

---

## SCR-C09 — Danh sách đơn quá hạn / No-show

| | |
|---|---|
| **URL** | `GET /Reservations?tab=noshow` — tab của SCR-C01 |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-C08, BR-05 |

### Mục đích
Cuối ngày, lễ tân rà soát các đơn `Confirmed` có ngày đến là hôm nay (hoặc trước đó) mà khách
chưa check-in, để giải phóng phòng cho ngày hôm sau.

### Nội dung
Bảng: Mã đơn · Khách · SĐT · Ngày đến · Phòng đã giữ · Đã cọc · Số giờ quá hạn · Thao tác.
Tiêu chí lọc mặc định: `Confirmed` + `CheckInDate ≤ hôm nay` + đã qua **giờ hết hạn giữ chỗ** (18:00).

### Thao tác
| Nút | Mô tả |
|---|---|
| Gọi khách (hiện SĐT) | Chỉ hiển thị, không tự gọi |
| **Đánh dấu No-show** | Chuyển `NoShow`, thu 100% cọc thành doanh thu, giải phóng phòng, ghi audit log |
| Gia hạn giữ chỗ | Dời hạn giữ chỗ thêm N giờ (khách báo đến muộn), ghi lý do |
| Check-in muộn | Chuyển thẳng sang SCR-D02 |

### Quy tắc
- Hệ thống **không tự động** chuyển No-show — luôn cần con người xác nhận, vì khách có thể
  đang trên đường. Hệ thống chỉ liệt kê và nhắc.
- Đánh dấu No-show cho đơn **chưa cọc** thì không thu được gì, chỉ giải phóng phòng và
  ghi nhận vào lịch sử khách (ảnh hưởng uy tín khách ở lần đặt sau).
