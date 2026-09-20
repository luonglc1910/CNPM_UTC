# 05 — Lễ tân / Front Desk (Nhóm D)

> Đây là nhóm màn hình được dùng nhiều nhất trong ngày. Admin và Lễ tân quyền **như nhau**.
> Mọi thao tác trong nhóm này đều chạy trong **transaction** (NFR-03) — không được để xảy ra
> tình trạng phòng đã chuyển `Occupied` nhưng folio chưa tạo.

---

## SCR-D01 — Bảng điều khiển lễ tân

| | |
|---|---|
| **URL** | `GET /FrontDesk` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-D01 |

### Bố cục — 4 tab

**Tab 1 — Khách đến hôm nay (mặc định)**
Mã đơn · Khách · SĐT · Loại phòng · Phòng đã giữ · Số khách · Đã cọc · Trạng thái ·
nút **Check-in**.
Sắp xếp: đơn chưa check-in lên trước.

**Tab 2 — Khách đi hôm nay**
Phòng · Khách · Giờ trả dự kiến · Số đêm · Tạm tính folio · **Số dư phải thu** ·
đã kiểm phòng chưa · nút **Check-out**.
Dòng có giờ trả đã quá 12:00 mà chưa check-out được **tô vàng** (đang phát sinh phụ thu trễ — BR-03).

**Tab 3 — Đang lưu trú**
Phòng · Khách · Ngày vào · Ngày đi dự kiến · Số khách · Tạm tính folio ·
nút Ghi dịch vụ · Đổi phòng · Gia hạn.

**Tab 4 — Phòng trống**
Lưới phòng theo tầng, màu theo trạng thái; nhấp phòng `Available` → mở SCR-D03 (Walk-in)
với phòng điền sẵn.

### Thanh trạng thái nhanh (đầu trang)
`Trống: 12 · Đang ở: 25 · Chờ dọn: 5 · Bảo trì: 2 · Đến: 8 · Đi: 6`

### Quy tắc
- Nút **Check-in** mờ khi phòng chưa sẵn sàng (`Dirty`/`Maintenance`), tooltip nêu rõ lý do
  và gợi ý "Ghi nhận cần dọn phòng" (BR-07).
- Nút **Check-out** mờ khi chưa có xác nhận đã kiểm phòng (BR-08).

---

## SCR-D02 — Check-in khách có đặt trước

| | |
|---|---|
| **URL** | `GET/POST /FrontDesk/CheckIn/{reservationId}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-D02, BR-01, BR-02, BR-03, BR-07 |

### Bố cục — 4 bước trên một trang

**Bước 1 — Đối chiếu đơn và giấy tờ**
- Hiển thị thông tin đơn (chỉ đọc): mã đơn, ngày đến/đi, loại phòng, số khách, đã cọc.
- Hiển thị hồ sơ khách đứng tên với **số giấy tờ đầy đủ** để lễ tân đối chiếu giấy tờ trên tay.
- Hộp kiểm bắt buộc: **"Đã đối chiếu giấy tờ tùy thân"** — không tích thì không lưu được (BR-07).
- Nút sửa nhanh hồ sơ khách nếu thông tin sai lệch (mở SCR-B02 dạng hộp thoại).

**Bước 2 — Gán phòng**
- Với mỗi dòng phòng của đơn: nếu đã chọn phòng cụ thể thì hiện sẵn, nếu chưa thì bắt buộc chọn
  từ danh sách **phòng `Available` cùng loại**.
- Cho phép đổi sang phòng khác cùng loại (khách yêu cầu tầng cao...).
- Đổi sang **loại phòng khác** (nâng hạng/hạ hạng) → phải nhập lý do, giá/đêm tính lại theo
  loại mới, chênh lệch hiện rõ trên màn hình, và thao tác này ghi audit log.

**Bước 3 — Danh sách khách ở cùng** (FR-B04)
- Bảng khách trong phòng: người đứng tên đã có sẵn ở dòng đầu.
- Nút **Thêm khách**: nhập họ tên, loại & số giấy tờ (bắt buộc với người lớn), quốc tịch.
- Hệ thống đếm số khách và đối chiếu sức chứa:
  - Vượt **sức chứa chuẩn** → cảnh báo vàng và **tự thêm dòng phụ thu thêm người** (BR-03).
  - Vượt **sức chứa tối đa** → chặn, không cho check-in.

**Bước 4 — Xác nhận & tiền**
| Dòng | Nội dung |
|---|---|
| Giờ nhận phòng thực tế | Mặc định thời điểm hiện tại, sửa được |
| Phụ thu nhận phòng sớm | Tự tính nếu trước 14:00 theo BR-03, hiện rõ mức % |
| Tiền phòng dự kiến | Giá/đêm đã chốt × số đêm (BR-02) |
| Phụ thu thêm người | Nếu có |
| Đã cọc | Trừ ra khi thanh toán, không thu lại ở đây |

### Luồng xử lý khi nhấn "Xác nhận check-in" (một transaction)
1. Kiểm tra lại: đơn còn `Confirmed`, phòng còn `Available`, đã tích đối chiếu giấy tờ (BR-07).
2. Tạo `Stay` cho từng phòng: gắn đơn, phòng, khách, **giá/đêm chốt tại thời điểm này** (BR-02),
   giờ vào thực tế, ngày đi dự kiến.
3. Tạo `StayGuest` cho từng khách trong phòng.
4. Tạo `Folio` trạng thái `Open` cho mỗi Stay, nạp sẵn dòng tiền phòng và các dòng phụ thu.
5. Chuyển phòng sang `Occupied`; chuyển đơn sang `CheckedIn`.
6. Ghi audit log; hiện thông báo thành công và nút **In phiếu nhận phòng**.

### Xử lý lỗi thường gặp
| Tình huống | Phản hồi |
|---|---|
| Phòng vừa bị người khác nhận mất | "Phòng 201 vừa được nhận cho khách khác. Vui lòng chọn phòng khác." Giữ nguyên dữ liệu đã nhập |
| Phòng chưa dọn | Chặn, gợi ý chọn phòng khác hoặc mở SCR-E01 nhắc dọn |
| Khách trong danh sách hạn chế | Cảnh báo đỏ, cho tiếp tục nhưng ghi audit log |
| Đến sớm hơn ngày đặt | Cảnh báo, cho phép check-in nếu phòng trống, tính lại số đêm từ hôm nay |

---

## SCR-D03 — Check-in khách vãng lai (Walk-in)

| | |
|---|---|
| **URL** | `GET/POST /FrontDesk/WalkIn` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-D03, BR-07, BR-10 |

### Mục đích
Gộp *tạo khách + chọn phòng + nhận cọc + check-in* vào **một màn hình, một lần lưu** —
vì khách đang đứng chờ ở quầy.

### Các khối
1. **Khách**: ô tìm nhanh theo CCCD/SĐT (khách cũ quay lại) hoặc nhập mới
   (họ tên `*`, loại & số giấy tờ `*`, SĐT `*`, quốc tịch `*`).
2. **Phòng & thời gian**: chọn phòng từ danh sách `Available` · ngày đi dự kiến `*`
   (mặc định hôm sau) · số khách `*`. Hiển thị giá/đêm và số đêm tự tính.
3. **Khách ở cùng**: như bước 3 của SCR-D02.
4. **Đặt cọc** (tùy chọn): số tiền · phương thức · mã giao dịch nếu chuyển khoản/thẻ.

### Luồng xử lý (một transaction)
Tạo `Guest` (nếu mới) → tạo `Stay` + `StayGuest` → tạo `Folio` → ghi nhận `Deposit`/`Payment`
gắn ca hiện tại (nếu có thu cọc) → phòng chuyển `Occupied` → audit log.

**Không tạo `Reservation`** cho khách vãng lai — Stay đứng độc lập.

### Quy tắc
- Thu cọc yêu cầu **ca đang mở** (BR-10). Nếu chưa mở ca, vẫn cho check-in nhưng phần thu cọc
  bị vô hiệu hóa kèm nhắc mở ca.
- Nếu tất cả phòng đều bận → màn hình hiện thông báo và liên kết sang SCR-C02 để tra ngày khác.

---

## SCR-D04 — Chi tiết lượt lưu trú

| | |
|---|---|
| **URL** | `GET /FrontDesk/Stay/{id}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-D08 |

### Nội dung
- **Khối phòng & khách**: số phòng, loại, giá/đêm đã chốt, giờ vào thực tế, ngày đi dự kiến,
  số đêm, danh sách khách ở cùng.
- **Khối chi phí tạm tính** (đọc từ Folio): tiền phòng · dịch vụ · phụ thu · giảm giá ·
  đã cọc · **số dư phải thu**.
- **Khối lịch sử**: các lần đổi phòng, gia hạn, thêm khách, dịch vụ đã ghi — kèm người thực hiện.

### Thanh nút
Ghi dịch vụ (→ SCR-F03) · Thêm khách (→ SCR-D05) · Đổi phòng (→ SCR-D06) ·
Gia hạn (→ SCR-D07) · Xem folio (→ SCR-F02) · **Check-out** (→ SCR-D08).

---

## SCR-D05 — Thêm khách vào phòng

| | |
|---|---|
| **URL** | `GET/POST /FrontDesk/AddGuest/{stayId}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-D04, FR-B04, BR-03 |

### Các trường
Họ tên `*` · Loại & số giấy tờ `*` (với người lớn) · Ngày sinh · Quốc tịch `*` ·
Là trẻ em (hộp kiểm — trẻ em dưới độ tuổi cấu hình không tính phụ thu).

### Quy tắc
1. Đếm lại tổng số khách trong Stay sau khi thêm.
2. ≤ sức chứa chuẩn → thêm bình thường, không phát sinh chi phí.
3. > sức chứa chuẩn, ≤ sức chứa tối đa → hiện rõ **"Sẽ phát sinh phụ thu X ₫/đêm × N đêm còn lại"**,
   yêu cầu xác nhận, sau đó tự thêm dòng phụ thu vào folio (BR-03).
4. > sức chứa tối đa → **chặn**, gợi ý đặt thêm phòng.
5. Phụ thu chỉ tính cho **số đêm còn lại** kể từ đêm hiện tại, không hồi tố các đêm đã qua.

---

## SCR-D06 — Đổi phòng

| | |
|---|---|
| **URL** | `GET/POST /FrontDesk/ChangeRoom/{stayId}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-D05, BR-09, BR-11 |

### Nội dung màn hình
- Thông tin phòng hiện tại: số phòng, loại, giá/đêm, số đêm đã ở.
- Danh sách **phòng đích** khả dụng: chỉ những phòng `Available` và **trống trong suốt
  khoảng thời gian còn lại** của lượt lưu trú (BR-06).
- Bảng so sánh chi phí:

```
Phòng cũ 201 (Standard, 800.000/đêm) — đã ở 2 đêm = 1.600.000 ₫  (giữ nguyên trên folio)
Phòng mới 305 (Deluxe, 1.200.000/đêm) — còn 3 đêm = 3.600.000 ₫
Chênh lệch phát sinh: +1.200.000 ₫
```

- **Lý do đổi phòng** `*` (bắt buộc — BR-09): khách yêu cầu / phòng hỏng / nâng hạng /
  lỗi xếp phòng / khác.
- Hộp kiểm **Miễn chênh lệch giá** — chỉ Admin (dùng khi đổi phòng do lỗi khách sạn).

### Luồng xử lý (một transaction)
1. Chốt chi phí phòng cũ đến thời điểm đổi, **giữ nguyên trên folio hiện hành** (BR-09).
2. Cập nhật Stay: trỏ sang phòng mới, giá/đêm mới cho các đêm còn lại.
3. Phòng cũ → `Dirty`; phòng mới → `Occupied`.
4. Ghi `RoomChangeLog`: phòng cũ, phòng mới, lý do, thời điểm, người thực hiện.
5. Ghi audit log (BR-11).

### Quy tắc
- Không tạo folio mới — **một lượt lưu trú chỉ có một folio**, dù đổi phòng bao nhiêu lần.
- Nếu phòng mới rẻ hơn, chênh lệch âm được ghi nhận, khách được trừ khi thanh toán.
- Không đổi sang phòng đang `Dirty` — phải dọn xong trước.

---

## SCR-D07 — Gia hạn lưu trú

| | |
|---|---|
| **URL** | `GET/POST /FrontDesk/Extend/{stayId}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-D06, BR-02, BR-06 |

### Các trường
Ngày đi mới `*` (phải > ngày đi hiện tại) · Ghi chú.

### Hiển thị
Số đêm thêm · giá/đêm áp dụng cho các đêm thêm · thành tiền phát sinh.

### Quy tắc
1. Kiểm tra phòng có trống trong các đêm thêm không (BR-06). **Nếu đã có đơn khác đặt** →
   chặn và gợi ý danh sách phòng khác còn trống (khách sẽ phải đổi phòng ở SCR-D06).
2. Giá các đêm thêm: mặc định dùng giá/đêm đã chốt của Stay; Admin sửa được, có ghi log.
3. Thêm dòng tiền phòng tương ứng vào folio.
4. Rút ngắn kỳ ở (trả sớm) **không làm ở màn hình này** — cứ check-out sớm ở SCR-D08,
   hệ thống tự tính theo số đêm thực ở.

---

## SCR-D08 — Check-out

| | |
|---|---|
| **URL** | `GET/POST /FrontDesk/CheckOut/{stayId}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-D07, BR-01, BR-03, BR-08 |

### Bố cục — 3 khối kiểm tra trước khi cho thanh toán

**Khối 1 — Điều kiện check-out (BR-08)**
| Điều kiện | Trạng thái | Hành động nếu chưa đạt |
|---|---|---|
| Đã kiểm phòng & minibar | ✔ / ✘ | Nút "Kiểm phòng ngay" → mở SCR-E02 |
| Folio đã khóa thêm chi phí | ✔ / ✘ | Nút "Khóa folio" |
| Không còn yêu cầu dịch vụ đang dở | ✔ / ✘ | Liên kết sang danh sách yêu cầu |

Chưa đạt đủ điều kiện thì nút **Tiếp tục thanh toán** bị vô hiệu hóa.

**Khối 2 — Chốt thời gian và phụ thu (BR-01, BR-03)**
```
Giờ trả phòng chuẩn: 12:00
Giờ trả thực tế:     [16:30]   ← mặc định giờ hiện tại, sửa được (có ghi log nếu sửa)
→ Trả trễ 4 giờ 30 phút, thuộc khung 15:00–18:00
→ Phụ thu trả phòng trễ: 50% × 1.200.000 = 600.000 ₫
```
- Trả sau 18:00 → hệ thống **tính thêm nguyên 1 đêm** thay vì phụ thu %.
- Trả **sớm hơn** ngày dự kiến → tính lại theo **số đêm thực ở**, dòng tiền phòng được điều chỉnh
  giảm và hiện rõ chênh lệch.
- Hộp kiểm **Miễn phụ thu trễ giờ** — chỉ Admin, bắt buộc lý do, ghi audit log.

**Khối 3 — Tổng hợp folio**
| Dòng | |
|---|---|
| Tiền phòng (số đêm thực) | |
| Dịch vụ đã dùng | Liệt kê rút gọn, liên kết sang SCR-F02 |
| Phụ thu | Nhận sớm / trả trễ / thêm người |
| Giảm giá | Nếu có |
| Đã cọc | |
| **Số dư phải thu** | |

### Luồng xử lý khi nhấn "Tiếp tục thanh toán"
1. Kiểm tra lại đủ điều kiện BR-08 ở server.
2. Ghi giờ trả thực tế vào Stay; thêm các dòng phụ thu vào folio.
3. Khóa folio (`IsLocked = true`) — từ đây không thêm chi phí được nữa, trừ khi Admin mở lại.
4. Chuyển sang SCR-F05 (Thanh toán).

### Sau khi thanh toán hoàn tất (xử lý ở SCR-F05, mô tả lại ở đây cho trọn luồng)
- Stay chuyển `CheckedOut`, đơn đặt phòng (nếu có) chuyển `CheckedOut`.
- Phòng tự động chuyển **`Dirty`** (FR-D07) và xuất hiện trong hàng đợi dọn ở SCR-E01.
- Hóa đơn chuyển `Settled`, in được ở SCR-F06.

### Quy tắc quan trọng
- **Không cho check-out khi số dư > 0** (BR-08). Trường hợp khách không đủ tiền: chỉ Admin
  được ghi nhận công nợ, bắt buộc lý do, và việc này ghi audit log.
- Toàn bộ bước cuối (chốt Stay + Folio + Invoice + đổi trạng thái phòng) chạy trong **một
  transaction** — mất điện giữa chừng thì không được để khách đã trả tiền mà phòng vẫn `Occupied`.
