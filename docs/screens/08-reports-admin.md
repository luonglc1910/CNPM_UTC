# 08 — Báo cáo & Quản trị (Nhóm G)

> **Toàn bộ nhóm này chỉ dành cho Admin.** Lễ tân truy cập → 403.
> Ngoại lệ duy nhất: lễ tân xem được số liệu ca của chính mình, nhưng qua SCR-F09
> (báo cáo cuối ca) chứ không qua nhóm màn hình này.
>
> Quy tắc dữ liệu chung cho mọi báo cáo:
> - Chỉ tính hóa đơn trạng thái **`Settled`**; hóa đơn `Void` bị loại hoàn toàn.
> - Mốc thời gian dùng **ngày chốt hóa đơn**, không phải ngày khách check-in.
> - Mọi báo cáo đều có nút **Xuất Excel/CSV** (FR-G08) và mỗi lần xuất đều ghi audit log.

---

## SCR-G01 — Báo cáo doanh thu

| | |
|---|---|
| **URL** | `GET /Reports/Revenue` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-G01, FR-G02, FR-G08 |

### Bộ lọc
Kỳ báo cáo (Hôm nay / Tuần này / Tháng này / Năm nay / Tùy chọn khoảng ngày) ·
Mức gộp (Theo ngày / Theo tháng / Theo năm).

### Nội dung

**Khối 1 — Thẻ số liệu**
`Tổng doanh thu` · `Tiền phòng` · `Dịch vụ` · `Phụ thu` · `Phí hủy & no-show` ·
`Số hóa đơn` · `Giá trị hóa đơn trung bình`.

**Khối 2 — Biểu đồ xu hướng**
Biểu đồ cột/đường theo trục thời gian, mỗi mốc tách thành các phần: tiền phòng, dịch vụ,
phụ thu, phí hủy (FR-G02).

**Khối 3 — Bảng chi tiết**
| Kỳ | Tiền phòng | Dịch vụ | Phụ thu | Phí hủy | Giảm giá | VAT | Tổng |
|---|---|---|---|---|---|---|---|

Dòng cuối là dòng tổng cộng. Nhấp vào một kỳ → xem danh sách hóa đơn của kỳ đó.

### Quy tắc tính
```
Doanh thu kỳ = Σ (hóa đơn Settled chốt trong kỳ)
             + Σ (phí hủy, phí no-show ghi nhận trong kỳ)
```
- **Tiền cọc chưa đối trừ không phải doanh thu** — chỉ ghi nhận khi hóa đơn được chốt hoặc
  khi cọc chuyển thành phí hủy.
- Giảm giá hiển thị thành cột riêng (số dương) để thấy được tổng số tiền đã "bớt" cho khách.

---

## SCR-G02 — Báo cáo công suất phòng

| | |
|---|---|
| **URL** | `GET /Reports/Occupancy` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-G03, FR-G04 |

### Bộ lọc
Khoảng ngày · Loại phòng (tất cả / từng loại).

### Nội dung

**Khối 1 — Thẻ số liệu**
| Chỉ số | Công thức |
|---|---|
| Công suất phòng (Occupancy) | `Số đêm-phòng bán được / Số đêm-phòng khả dụng × 100%` |
| ADR (giá phòng bình quân) | `Doanh thu tiền phòng / Số đêm-phòng bán được` |
| RevPAR | `Doanh thu tiền phòng / Số đêm-phòng khả dụng` (= Công suất × ADR) |
| Số đêm-phòng bán được | Tổng số đêm thực tế có khách |

**Khối 2 — Biểu đồ công suất theo ngày** — đường tỷ lệ %, có đường ngang đánh dấu mức trung bình kỳ.

**Khối 3 — Bảng theo loại phòng**
| Loại phòng | Số phòng | Đêm khả dụng | Đêm bán được | Công suất | ADR | RevPAR |

### Quy tắc tính (FR-G03)
- **Mẫu số** = Σ (số phòng khai thác được × số ngày trong kỳ). Phòng `OutOfService`
  **bị loại khỏi mẫu số** trong những ngày nó ngừng khai thác — nếu không, công suất bị
  kéo xuống sai lệch.
- Phòng `Maintenance` **vẫn tính vào mẫu số** (đó là phòng lẽ ra bán được nhưng khách sạn
  không bán được — cần thấy rõ tổn thất này).
- **Tử số** = số đêm-phòng có `Stay` thực tế, tính theo số đêm thực ở sau check-out.

---

## SCR-G03 — Báo cáo dịch vụ & tồn kho

| | |
|---|---|
| **URL** | `GET /Reports/Services` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-G05 |

### Nội dung

**Tab 1 — Dịch vụ bán chạy**
| Dịch vụ | Nhóm | Số lượng bán | Doanh thu | Tỷ trọng % |
Sắp xếp giảm dần theo doanh thu; có biểu đồ tròn theo nhóm dịch vụ.

**Tab 2 — Tồn kho hiện tại**
| Dịch vụ | Đơn vị | Tồn đầu kỳ | Nhập | Bán | Điều chỉnh | Tồn cuối kỳ | Tồn tối thiểu | Cảnh báo |

Dòng có `Tồn cuối kỳ ≤ Tồn tối thiểu` tô vàng; `= 0` tô đỏ.

### Quy tắc đối chiếu
Báo cáo phải thỏa: `Tồn đầu + Nhập − Bán ± Điều chỉnh = Tồn cuối`.
Nếu lệch → có giao dịch kho bị ghi sai, hệ thống hiện cảnh báo đỏ ở đầu báo cáo
(đây là chốt kiểm soát cho BR-12).

---

## SCR-G04 — Báo cáo theo nhân viên / ca

| | |
|---|---|
| **URL** | `GET /Reports/Staff` |
| **Quyền** | Chỉ Admin |
| **Yêu cầu** | FR-G06 |

### Bộ lọc
Khoảng ngày · Nhân viên · Vai trò.

### Nội dung

**Tab 1 — Theo ca thu ngân**
| Ca | Nhân viên | Giờ mở – đóng | Tổng thu | Tiền mặt | Chuyển khoản | Thẻ | **Chênh lệch quỹ** |

Ca có chênh lệch ≠ 0 được tô màu; nhấp vào xem SCR-F09 của ca đó.
Cột **Tổng chênh lệch lũy kế theo nhân viên** ở cuối bảng — dùng để phát hiện người
thường xuyên lệch quỹ.

**Tab 2 — Theo nhân viên lễ tân**
Số lượt check-in · check-out · đơn đặt tạo mới · đơn hủy · tổng giảm giá đã cấp ·
số lần bỏ qua cảnh báo (khách blacklist, bán âm kho).

> **Đã bỏ tab "Năng suất nhân viên buồng phòng".** Không còn tài khoản buồng phòng riêng nên
> số liệu chỉ phản ánh *ai bấm nút cập nhật*, không phải *ai thực sự dọn* — đo ra sẽ sai lệch.
> Thống kê số lượt dọn và thời gian phòng nằm chờ dọn thuộc về báo cáo công suất (SCR-G02).

### Mục đích kiểm soát
Hai tab này là công cụ chống gian lận: giảm giá bất thường, hủy đơn bất thường, lệch quỹ
lặp lại đều lộ ra ở đây.

---

## SCR-G05 — Nhật ký thao tác (Audit log)

| | |
|---|---|
| **URL** | `GET /Reports/AuditLog` |
| **Quyền** | **Chỉ Admin.** Không ai được xóa, kể cả Admin |
| **Yêu cầu** | FR-G07, BR-11, NFR-07 |

### Cột hiển thị
Thời điểm · Người thực hiện · Vai trò · Hành động · Đối tượng (loại + mã) ·
Giá trị cũ → giá trị mới · Lý do · Địa chỉ IP.

### Bộ lọc
Khoảng thời gian · Người thực hiện · Loại hành động · Ô tìm theo mã đối tượng
(mã đơn, số hóa đơn, số phòng).

### Danh sách hành động được ghi (BR-11)

| Nhóm | Hành động |
|---|---|
| Giá & danh mục | Sửa giá loại phòng · sửa giá dịch vụ · sửa cấu hình hệ thống |
| Đặt phòng | Hủy đơn · miễn phí hủy · đánh dấu no-show · sửa giá thủ công trên đơn |
| Lễ tân | Đổi phòng · nâng/hạ hạng phòng khi check-in · sửa giờ trả thực tế · miễn phụ thu |
| Tiền | Giảm giá · hủy dòng chi phí · hủy hóa đơn · hoàn tiền · đóng ca lệch quỹ |
| Kho | Điều chỉnh kho · bán khi hết tồn |
| Tài khoản | Tạo/sửa/khóa nhân viên · đổi vai trò · đặt lại mật khẩu · đăng nhập sai 5 lần · truy cập bị chặn 403 |
| Dữ liệu cá nhân | Xuất danh sách khai báo tạm trú · xem hồ sơ khách |

### Quy tắc bất biến
- Bảng `AuditLog` **chỉ ghi thêm**: không có chức năng sửa, không có chức năng xóa trong
  toàn bộ ứng dụng.
- Log được ghi **trong cùng transaction** với thao tác nghiệp vụ — nghiệp vụ thành công mà
  log thất bại thì cả hai cùng bị hủy bỏ.
- Lưu tối thiểu **2 năm**; cần dọn thì Admin xuất ra file lưu trữ trước khi xóa theo lô,
  và bản thân thao tác dọn đó cũng phải được ghi log.

### Xuất dữ liệu
Nút **Xuất Excel** theo bộ lọc hiện tại — dùng khi cần đối chiếu sự cố hoặc giải trình.
