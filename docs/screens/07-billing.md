# 07 — Thu ngân & Thanh toán (Nhóm F)

> Đây là nhóm nhạy cảm nhất về tiền. Ba nguyên tắc xuyên suốt:
> 1. Cả hai vai trò đều thu tiền được, nhưng **hủy hóa đơn (SCR-F07) chỉ Admin**, và lễ tân
>    chỉ giảm giá trong hạn mức (SCR-F04).
> 2. Mọi giao dịch thu/hoàn tiền phải gắn với một **ca làm việc đang mở** (BR-10).
> 3. Không sửa, không xóa chứng từ đã chốt — sai thì lập chứng từ điều chỉnh mới, có lý do
>    và audit log (BR-11).

---

## SCR-F01 — Danh sách folio / hóa đơn

| | |
|---|---|
| **URL** | `GET /Billing` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-F01 |

### Hai tab

**Tab 1 — Folio đang mở** (mặc định)
Phòng · Khách · Ngày vào · Ngày đi dự kiến · Tiền phòng · Dịch vụ · Phụ thu · Đã cọc ·
**Số dư** · Trạng thái khóa · Thao tác (Xem folio · Thêm chi phí · Thanh toán).

**Tab 2 — Hóa đơn đã chốt**
Số hóa đơn · Ngày chốt · Phòng · Khách · Tổng tiền · VAT · Phương thức · Trạng thái
(`Settled` / `Void`) · Người chốt · Ca · Thao tác (Xem/In · Hủy hóa đơn).

### Bộ lọc
Khoảng ngày · Trạng thái · Phương thức thanh toán · Ô tìm theo số hóa đơn, số phòng, tên khách.

### Phân quyền
| Thành phần | Admin | Lễ tân |
|---|:-:|:-:|
| Xem folio đang mở | Tất cả | Tất cả |
| Xem hóa đơn đã chốt | Tất cả, mọi thời điểm | Mặc định chỉ hóa đơn **trong ca của mình**; xem hóa đơn cũ cần mở bộ lọc và chỉ ở chế độ đọc |
| Nút Hủy hóa đơn | ✔ | Ẩn |

---

## SCR-F02 — Folio chi tiết

| | |
|---|---|
| **URL** | `GET /Billing/Folio/{stayId}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-F01, BR-04 |

### Bố cục

```
┌─ Thông tin lưu trú ──────────────────────────────────────────────┐
│ Phòng 305 (Deluxe) · Nguyễn Văn A · 18/09 14:20 → 21/09 (3 đêm)  │
│ Folio #F-000412 · Trạng thái: Đang mở                            │
├─ Các dòng chi phí ───────────────────────────────────────────────┤
│ Ngày   Loại      Nội dung              SL  Đơn giá    Thành tiền │
│ 18/09  Phòng     Deluxe đêm 18/09       1  1.200.000   1.200.000 │
│ 18/09  Dịch vụ   Bia Heineken           2     35.000      70.000 │
│ 19/09  Phòng     Deluxe đêm 19/09       1  1.200.000   1.200.000 │
│ 19/09  Phụ thu   Thêm người (1 khách)   1    200.000     200.000 │
│ 20/09  Giảm giá  Khách quen            -1    100.000    -100.000 │
├─ Tổng hợp ───────────────────────────────────────────────────────┤
│ Tiền phòng        2.400.000    Đã cọc        -1.200.000          │
│ Dịch vụ              70.000    ─────────────────────────         │
│ Phụ thu             200.000    Tạm tính       2.570.000          │
│ Giảm giá           -100.000    VAT 8%           205.600          │
│                                TỔNG            2.775.600         │
│                                Làm tròn        2.776.000         │
│                                SỐ DƯ PHẢI THU  2.776.000         │
└──────────────────────────────────────────────────────────────────┘
```

### Công thức (FR-F01, BR-04)
```
Tạm tính = Tiền phòng + Dịch vụ + Phụ thu − Giảm giá − Tiền cọc
VAT      = Tạm tính × thuế suất (mặc định 8%)
Tổng     = làm tròn(Tạm tính + VAT, 1.000 ₫)
```

### Thao tác

| Nút | Điều kiện | Admin | Lễ tân |
|---|---|:-:|:-:|
| Thêm chi phí | Folio đang mở | ✔ | ✔ |
| Hủy một dòng chi phí | Folio đang mở | ✔ | ✔ |
| Giảm giá | Folio đang mở | ✔ | ✔ (trong hạn mức) |
| **Khóa folio** | Folio đang mở | ✔ | ✔ |
| **Mở khóa folio** | Folio đã khóa, chưa chốt hóa đơn | ✔ | — |
| Thanh toán | Folio đã khóa | ✔ | ✔ |
| In bảng kê tạm tính | Luôn | ✔ | ✔ |

### Quy tắc hủy dòng chi phí (FR-E03)
- Chỉ hủy được khi folio **đang mở**.
- Bắt buộc nhập lý do; dòng bị hủy **không xóa khỏi bảng** mà hiển thị gạch ngang, ghi rõ
  ai hủy lúc nào — để lần sau đối chiếu được.
- Dòng dịch vụ có kho bị hủy → **hoàn lại tồn kho** (BR-12).
- Không hủy được dòng **tiền phòng** sinh tự động; muốn sửa phải qua SCR-D06/D07/D08.

---

## SCR-F03 — Thêm chi phí / dịch vụ

| | |
|---|---|
| **URL** | `GET/POST /Billing/AddCharge/{folioId}` |
| **Quyền** | Admin, Lễ tân |
| **Yêu cầu** | FR-E01, FR-E02, BR-12 |

### Các trường

| Trường | Bắt buộc | Kiểm tra |
|---|:-:|---|
| Loại chi phí | ✔ | Dịch vụ / Phụ thu thủ công |
| Dịch vụ | ✔ khi loại = Dịch vụ | Chọn từ danh mục đang dùng; hiện tồn kho bên cạnh |
| Số lượng | ✔ | > 0; với dịch vụ có kho: **≤ tồn hiện tại** (BR-12) |
| Đơn giá | ✔ | Mặc định lấy từ danh mục; **chỉ Admin sửa được**, sửa thì ghi log |
| Nội dung | ✔ khi loại = Phụ thu thủ công | Ví dụ "Bồi thường khăn tắm" |
| Ghi chú | | |

### Luồng xử lý
1. Kiểm tra folio còn mở; đã khóa → chặn, báo "Folio đã khóa, liên hệ quản lý để mở lại."
2. Với dịch vụ có kho: kiểm tra tồn. Hết hàng → chặn, trừ khi cấu hình *cho phép bán khi hết tồn*
   được Admin bật (BR-12).
3. Tạo `FolioItem` (chép đơn giá tại thời điểm ghi, lưu người ghi và thời điểm).
4. Trừ tồn kho, tạo `InventoryTransaction` loại `Sale`.
5. Cập nhật lại tổng folio, quay về SCR-F02 kèm thông báo thành công.

---

## SCR-F04 — Giảm giá

| | |
|---|---|
| **URL** | `GET/POST /Billing/Discount/{folioId}` |
| **Quyền** | Admin: không giới hạn · Lễ tân: trong hạn mức cấu hình |
| **Yêu cầu** | FR-F02, BR-11 |

### Các trường
Hình thức `*` (Số tiền cố định / Phần trăm) · Giá trị `*` · **Lý do** `*` (bắt buộc, chọn từ
danh sách: khách quen / khiếu nại dịch vụ / chương trình khuyến mại / quản lý duyệt / khác)
· Ghi chú.

### Quy tắc hạn mức (FR-F02)
```
Hạn mức lễ tân (cấu hình ở SCR-A12): 200.000 ₫ hoặc 10% hóa đơn — lấy giá trị nhỏ hơn
```
- Lễ tân nhập trong hạn mức → áp dụng ngay.
- Lễ tân nhập **vượt hạn mức** → chặn, hiện thông báo "Vượt hạn mức giảm giá, cần quản lý
  thực hiện." (v1.0 **không** có luồng gửi duyệt trực tuyến — Admin đăng nhập và tự làm).
- Admin giảm giá không giới hạn nhưng vẫn **bắt buộc lý do**.
- Không cho giảm giá vượt quá tạm tính (số dư không được âm do giảm giá).

### Ghi nhận
Tạo `FolioItem` loại *Giảm giá* với số tiền âm; **luôn ghi audit log** (BR-11) gồm người thực hiện,
số tiền, lý do, folio nào.

---

## SCR-F05 — Thanh toán

| | |
|---|---|
| **URL** | `GET/POST /Billing/Payment/{folioId}` |
| **Quyền** | Admin, Lễ tân — **bắt buộc có ca đang mở** (BR-10) |
| **Yêu cầu** | FR-F03, FR-F04, FR-F05, BR-04, BR-08, BR-10 |

### Bố cục

```
┌─ Cần thu ────────────────────────────────────────────┐
│ Tổng hóa đơn:        2.776.000 ₫                     │
│ Đã cọc:             -1.200.000 ₫                     │
│ SỐ DƯ PHẢI THU:      1.576.000 ₫                     │
├─ Nhận tiền ──────────────────────────────────────────┤
│ Phương thức: (•) Tiền mặt ( ) Chuyển khoản ( ) Thẻ   │
│ Số tiền khách đưa: [2.000.000]                       │
│ → Tiền thối lại:      424.000 ₫                      │
│ Mã giao dịch: [_______]  (bắt buộc với CK/Thẻ)       │
└──────────────────────────────────────────────────────┘
        [ Hoàn tất thanh toán & Check-out ]
```

### Kiểm tra trước khi cho thanh toán
1. Người dùng **có ca đang mở** (BR-10) — nếu không, thay form bằng nút "Mở ca làm việc".
2. Folio đã khóa.
3. Đã đủ điều kiện check-out theo BR-08 (đã kiểm phòng).

### Quy tắc số tiền
- Tiền mặt: số tiền khách đưa ≥ số dư; hệ thống tính tiền thối.
- Chuyển khoản / Thẻ: số tiền phải **đúng bằng** số dư; bắt buộc nhập mã giao dịch để đối soát.
- v1.0 **không hỗ trợ thanh toán từng phần**: một lần thanh toán phải tất toán hết số dư
  (đã chốt trong phạm vi ở REQUIREMENTS mục 1.2).

### Xử lý tiền cọc thừa (FR-F04)
Nếu **đã cọc > tổng hóa đơn** → màn hình chuyển sang chế độ **hoàn tiền**:
```
Đã cọc:        1.500.000 ₫
Tổng hóa đơn:  1.200.000 ₫
HOÀN LẠI KHÁCH:  300.000 ₫    Phương thức hoàn: (•) Tiền mặt ( ) Chuyển khoản
```
Tạo `Payment` số âm (phiếu hoàn) gắn ca hiện tại, in phiếu hoàn tiền cho khách ký nhận.

### Luồng xử lý khi nhấn "Hoàn tất" (một transaction — NFR-03)
1. Tạo `Payment` (phương thức, số tiền, mã giao dịch, ca, người thu).
2. Đối trừ `Deposit` vào hóa đơn.
3. Sinh `Invoice`: số hóa đơn liên tục không trùng, tổng tiền, VAT, trạng thái `Settled` (FR-F05).
4. Chuyển `Folio` sang đóng, `Stay` sang `CheckedOut`, `Reservation` (nếu có) sang `CheckedOut`.
5. **Phòng chuyển `Dirty`**, vào hàng đợi dọn ở SCR-E01.
6. Ghi audit log.
7. Chuyển sang SCR-F06 và tự mở hộp thoại in hóa đơn.

### Quy tắc sinh số hóa đơn
Dạng `HD-yyyyMM-#####`, số thứ tự **liên tục, không nhảy cóc, không trùng**, sinh trong
transaction bằng khóa ở tầng database. Hóa đơn bị hủy (`Void`) **vẫn giữ số** — không cấp lại
số đó cho hóa đơn khác.

---

## SCR-F06 — Hóa đơn (xem / in)

| | |
|---|---|
| **URL** | `GET /Billing/Invoice/{id}` |
| **Quyền** | Admin, Lễ tân (chỉ đọc) |
| **Yêu cầu** | FR-F05, NFR-06 |

### Nội dung bản in
- Đầu trang: tên khách sạn, địa chỉ, điện thoại, mã số thuế, logo (lấy từ SCR-A12).
- Số hóa đơn · ngày giờ chốt · nhân viên thu ngân.
- Thông tin khách: họ tên, số giấy tờ, phòng, ngày vào – ngày ra, số đêm.
- Bảng chi tiết: từng dòng tiền phòng, dịch vụ, phụ thu, giảm giá.
- Tổng hợp: tạm tính · VAT · tổng · đã cọc · đã thanh toán · phương thức.
- Chân trang: lời cảm ơn, ghi chú "Hóa đơn này không thay thế hóa đơn giá trị gia tăng".
- Hóa đơn `Void`: in đè chữ **ĐÃ HỦY** màu đỏ, kèm lý do và người hủy.

### Định dạng in (NFR-06)
Hai chế độ: **A4** (đầy đủ) và **K80** (máy in nhiệt, khổ hẹp, rút gọn cột).
Nút **Xuất PDF** để lưu hoặc gửi khách qua email thủ công.

### Quy tắc
Hóa đơn đã chốt **không sửa được** ở bất kỳ đâu. In lại bao nhiêu lần cũng được; mỗi lần in lại
ghi log nhẹ (ai in, lúc nào) để truy vết khi có tranh chấp.

---

## SCR-F07 — Hủy hóa đơn (Void)

| | |
|---|---|
| **URL** | `GET/POST /Billing/VoidInvoice/{id}` |
| **Quyền** | **Chỉ Admin.** Lễ tân không thấy nút, truy cập thẳng URL → 403 |
| **Yêu cầu** | FR-F06, BR-11 |

### Khi nào dùng
Hóa đơn chốt nhầm: sai khách, sai phòng, thu nhầm tiền. **Không dùng để sửa số tiền** —
sửa thì hủy rồi lập lại.

### Các trường
**Lý do hủy** `*` (bắt buộc, ≥ 10 ký tự) · Hộp kiểm xác nhận "Tôi hiểu thao tác này không thể
hoàn tác".

### Luồng xử lý (một transaction)
1. Chuyển `Invoice` sang `Void` — **không xóa bản ghi** (FR-F06).
2. Tạo `Payment` đối ứng (số âm) để trung hòa khoản đã thu, gắn **ca hiện tại của Admin**
   (không sửa ngược vào ca cũ đã đóng — sổ ca đã đóng là bất biến, BR-10).
3. Nếu cần mở lại folio để lập hóa đơn mới → Admin mở khóa folio ở SCR-F02.
4. Ghi audit log ở mức cao nhất: ai hủy, hóa đơn nào, số tiền, lý do, thời điểm.

### Hệ quả báo cáo
Hóa đơn `Void` **bị loại khỏi mọi báo cáo doanh thu** (FR-G01, FR-G02), nhưng vẫn hiện trong
danh sách hóa đơn và trong audit log. Báo cáo cuối ca của ca cũ **không bị thay đổi hồi tố**;
khoản điều chỉnh nằm ở ca hiện tại.

---

## SCR-F08 — Ca làm việc (mở / đóng ca)

| | |
|---|---|
| **URL** | `GET /Shifts`, `POST /Shifts/Open`, `POST /Shifts/Close` |
| **Quyền** | Admin (mọi ca) · Lễ tân (chỉ ca của mình) |
| **Yêu cầu** | FR-F08, BR-10 |

### Nội dung màn hình

**Khi chưa có ca mở:**
```
Bạn chưa mở ca làm việc. Mọi giao dịch thu tiền đều bị khóa.
Quỹ tiền mặt đầu ca: [__________]  ₫
                                    [ Mở ca ]
```

**Khi đang có ca mở:**
```
Ca #12 · Mở lúc 20/09/2026 06:00 · Nhân viên: Nguyễn Văn A
┌──────────────────────────────────────────────┐
│ Quỹ đầu ca:               2.000.000 ₫        │
│ Thu tiền mặt:            12.450.000 ₫        │
│ Hoàn tiền mặt:             -300.000 ₫        │
│ ───────────────────────────────────          │
│ Tiền mặt phải có:        14.150.000 ₫        │
│ Thu chuyển khoản:         8.200.000 ₫        │
│ Thu thẻ:                  3.100.000 ₫        │
│ Số giao dịch: 23 · Số hóa đơn: 14            │
└──────────────────────────────────────────────┘
                              [ Đóng ca ]
```

### Luồng đóng ca (BR-10)
1. Nhấn **Đóng ca** → yêu cầu nhập **số tiền mặt đếm thực tế** `*`.
2. Hệ thống tính **chênh lệch** = tiền đếm − tiền mặt phải có.
3. Chênh lệch ≠ 0 → **bắt buộc nhập lý do**; chênh lệch lớn hơn ngưỡng cấu hình thì hiện
   cảnh báo đỏ và ghi audit log mức cao.
4. Lưu ca: thời điểm đóng, tiền đếm, chênh lệch, lý do. Ca chuyển `Closed`.
5. **Ca đã đóng không sửa, không xóa** — kể cả Admin. Sai sót xử lý bằng phiếu điều chỉnh
   ở ca mới.
6. Chuyển sang SCR-F09 (báo cáo cuối ca) để in.

### Quy tắc
- Một người **chỉ có một ca mở** tại một thời điểm. Mở ca thứ hai → chặn.
- Nhiều người có thể mở ca song song (ca sáng/ca chiều chồng nhau lúc giao ca) — mỗi người
  một sổ riêng.
- Không cho đăng xuất... (cho phép), nhưng hệ thống **nhắc** nếu đăng xuất khi ca còn mở.
- Admin xem được ca của mọi người; Lễ tân chỉ xem ca của chính mình (kể cả ca đã đóng).

---

## SCR-F09 — Báo cáo cuối ca

| | |
|---|---|
| **URL** | `GET /Shifts/Report/{id}` |
| **Quyền** | Admin (mọi ca) · Lễ tân (chỉ ca của mình) |
| **Yêu cầu** | FR-F09 |

### Nội dung
| Khối | Chi tiết |
|---|---|
| Thông tin ca | Mã ca, nhân viên, giờ mở – giờ đóng |
| Tổng thu theo phương thức | Tiền mặt / Chuyển khoản / Thẻ, kèm số giao dịch mỗi loại |
| Tổng hoàn tiền | Hoàn cọc thừa, hoàn do hủy hóa đơn |
| Doanh thu theo nguồn | Tiền phòng · Dịch vụ · Phụ thu · Phí hủy |
| Đối soát tiền mặt | Quỹ đầu ca · Thu · Hoàn · Phải có · Đếm thực tế · **Chênh lệch** |
| Danh sách hóa đơn trong ca | Số HĐ · phòng · khách · tổng tiền · phương thức |

### Thao tác
**In báo cáo** (A4, có dòng ký tên người giao – người nhận ca) · **Xuất Excel**.

### Quy tắc
Báo cáo được **chốt cứng tại thời điểm đóng ca**: các điều chỉnh về sau (ví dụ hủy hóa đơn
của ca này ở ngày hôm sau) **không làm thay đổi** báo cáo ca đã in — đây là điều kiện để
bản in giấy có giá trị đối soát.
