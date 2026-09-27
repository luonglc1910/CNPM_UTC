# 08 — Sprint nền tảng vận hành (SP0)

| | |
|---|---|
| **Trạng thái** | ⏳ Đã viết code, **chưa build/verify** (máy dev chưa có .NET SDK khi code) |
| **Cập nhật** | 20/09/2026 |
| **Màn hình** | SCR-F08, SCR-F09 (Ca làm việc) + hạ tầng dùng chung cho nhóm C/D/E/F/G |
| **Yêu cầu** | BR-02, BR-03, BR-04, BR-05, BR-06, BR-10, BR-12, NFR-03, FR-C04, FR-F05, FR-F08, FR-F09 |

---

## 1. Đã làm gì

Trước khi làm các màn nghiệp vụ phần 4-8, dựng **tầng hạ tầng dùng chung** để mọi màn hình
tính tiền / chống trùng / cấp số / chạy transaction theo **một** cách nhất quán, cộng màn
**Ca làm việc** (tiền đề cho mọi giao dịch tiền — BR-10).

| # | Thành phần | File | Vai trò |
|---|---|---|---|
| 1 | `ISettingsReader` | `Services/SettingsReader.cs` | Đọc cấu hình theo kiểu (giá trị thô), cache 1 lần/request; gom `PricingSettings` |
| 2 | `IPricingService` | `Services/PricingService.cs` | Số đêm, phụ thu sớm/trễ/thêm người, VAT, làm tròn, phí hủy — **logic thuần** |
| 3 | `IAvailabilityService` | `Services/AvailabilityService.cs` | Phòng trống / chống trùng (BR-06), đẩy NOT EXISTS xuống DB |
| 4 | `INumberSequenceService` | `Services/NumberSequenceService.cs` | Cấp số `HD-`/`RSV-`/`F-` liên tục, an toàn đồng thời |
| 5 | `ITransactionRunner` | `Services/TransactionRunner.cs` | Chạy nghiệp vụ nhiều bước trong 1 transaction (bọc execution strategy) |
| 6 | Inventory Sale/Return | `Services/InventoryService.cs` | `SellAsync`/`ReturnStockAsync` trừ/hoàn tồn (BR-12), không tự SaveChanges |
| 7 | Ca làm việc | `Services/ShiftService.cs`, `Controllers/ShiftsController.cs`, `Views/Shifts/*` | Mở/đóng ca, đối soát tiền mặt, báo cáo cuối ca (SCR-F08/F09) |

**Thay đổi mô hình dữ liệu (fold vào migration InitialCreate — sản phẩm mới chưa chạy):**
- `Models/Entities/NumberSequence.cs` (mới) + DbSet + unique index `(Prefix, Period)`.
- `Invoice`: thêm `AmountPaid`, `DebtAmount`; `InvoiceStatus.Debt` — công nợ (REQUIREMENTS 6.3, SCR-D08).
- Index filtered-unique `UX_Shift_OpenPerEmployee` (1 ca mở / nhân viên) và
  `UX_Housekeeping_OpenPerRoom` (1 task dọn mở / phòng) — chuẩn hóa DB còn thiếu.
- `SystemSettingKeys.CashDifferenceThreshold` + mặc định 50.000 (nhóm Limit).
- `Program.cs`: bật `EnableRetryOnFailure`; đăng ký DI 6 service mới.

---

## 2. Quyết định thiết kế

### 2.1 VAT KHÔNG trừ cọc — sửa mâu thuẫn trong docs/screens/07-billing.md
Công thức chữ ở SCR-F02 ghi "Tạm tính = ... − Tiền cọc" nhưng **các con số ví dụ** (SCR-F02 lẫn
luồng SCR-F05) lại tính VAT trên (phòng + dịch vụ + phụ thu − giảm giá), rồi mới trừ cọc ở bước
cuối để ra **số dư phải thu**. Đã chọn cách khớp con số (đúng bản chất: cọc là trả trước, không
giảm doanh thu chịu thuế):
```
SubTotal   = phòng + dịch vụ + phụ thu − giảm giá     (KHÔNG trừ cọc)
VAT        = SubTotal × thuế suất
Total      = làm_tròn(SubTotal + VAT, 1.000)
BalanceDue = Total − cọc                              (âm = hoàn khách)
```
→ `PricingService.ComputeFolioTotal`. **Cần xác nhận lại khi làm SP2 (billing).**

### 2.2 Cấp số bằng bảng đếm + khóa dòng, không dùng `max()+1`
`EmployeeService.NextCodeAsync` tải hết mã rồi `max()+1` — **không an toàn đồng thời**, cấm dùng cho
hóa đơn. `NumberSequenceService` dùng UPDATE (khóa dòng tới commit) + đọc lại trong cùng transaction;
tránh `UPDATE ... OUTPUT` qua `SqlQueryRaw` (EF có thể bọc subquery làm hỏng SQL).

### 2.3 Ca làm việc nằm trong SP0 dù README ghi "có thể lùi"
BR-10 bắt mọi giao dịch tiền gắn ca đang mở; thu cọc (C07) và thanh toán (F05) đều là MVP → làm ca
trước để khỏi viết logic ca giả rồi gỡ.

### 2.4 Chống đồng thời ở tầng DB, không chỉ ở service
Ca mở trùng và task dọn trùng chặn bằng index filtered-unique (theo mẫu `UX_Stay_ActiveRoom`),
để hai người bấm cùng lúc không lách qua được.

---

## 3. Cách build & verify (chạy trên máy có .NET SDK)

```bash
# 1. Tạo lại migration cho sạch (sản phẩm mới chưa chạy). Nếu đã có DB dev thì xóa/DROP trước.
dotnet ef migrations remove   # bỏ InitialCreate cũ nếu muốn gộp; hoặc:
dotnet ef migrations add AddFoundationInfra   # tạo migration tăng dần cũng được

# 2. Build
dotnet build

# 3. Chạy (app tự MigrateAsync + seed)
dotnet run
```

Kịch bản kiểm thử tối thiểu (SCR-F08/F09):
1. Đăng nhập `letan/123456` → thanh trên hiện **"Chưa mở ca"** (đỏ).
2. Vào **Ca làm việc** → mở ca quỹ đầu 2.000.000 → thanh trên đổi thành **"Ca #… đang mở"**.
3. Mở ca lần 2 → bị chặn ("đang có một ca mở").
4. Đóng ca: đếm khác sổ sách mà bỏ trống lý do → bị chặn; nhập lý do → đóng được, sang báo cáo.
5. Đóng ca đã đóng lần nữa → bị chặn.
6. `letan` không xem được báo cáo ca của người khác (Forbid).

---

## 4. Nợ kỹ thuật

| # | Món nợ | Ghi chú |
|---|---|---|
| 1 | **Chưa build/verify** | Máy code không có .NET SDK; cần chạy mục 3 trên máy dev |
| 2 | Doanh thu theo nguồn / hóa đơn trong ca **hiện = 0** | Đúng như mong đợi: chưa có Payment/Invoice nào; sẽ có dữ liệu khi làm SP1/SP2 |
| 3 | Xác nhận lại quy tắc VAF/cọc (2.1) khi làm SP2 | Sửa luôn comment `Invoice.SubTotal` cho khớp |
| 4 | `Stay.ReservationRoomId`, `FolioItem.RoomTypeId` (nice-to-have từ audit) | Chưa thêm; cân nhắc khi làm SP2/báo cáo |

---

## 5. Làm tiếp

SP1 — Đặt phòng (nhóm C): tra phòng trống (C02) → tạo đơn (C04) → danh sách/chi tiết/sửa →
thu cọc (C07) → hủy (C08) → no-show (C09). Dùng lại `IAvailabilityService`, `IPricingService`,
`INumberSequenceService`, `ITransactionRunner`, `IShiftService` đã dựng ở SP0.
