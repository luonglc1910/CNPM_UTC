# 04 — Tồn kho & Phiếu kho (SCR-A08, SCR-A09)

| | |
|---|---|
| **Trạng thái** | ✅ Hoàn thành, đã kiểm thử end-to-end trên app chạy thật |
| **Cập nhật lần cuối** | 20/09/2026 |
| **Màn hình** | SCR-A08, SCR-A09 (`docs/screens/02-catalog.md`) |
| **Yêu cầu** | FR-A05, BR-11, BR-12 |

---

## 1. Đã làm gì

| Màn hình | URL | Nội dung |
|---|---|---|
| SCR-A08 | `GET /Inventory` | Hai tab: **Tồn kho** (dịch vụ · đơn vị · tồn hiện tại · tồn tối thiểu · lần nhập gần nhất · thao tác) và **Lịch sử giao dịch kho** (thời điểm · loại · số lượng ± · tồn sau · chứng từ · người thực hiện · lý do). Lọc theo mã/tên, theo dịch vụ, theo loại giao dịch, chỉ dịch vụ dưới định mức |
| SCR-A09 | `GET/POST /Inventory/Receive` | Nhập kho: chọn dịch vụ · số lượng > 0 · ghi chú (số hóa đơn NCC) → tồn tăng, sinh `InventoryTransaction` loại `Receive` |
| SCR-A09 | `GET/POST /Inventory/Adjust` | Điều chỉnh kiểm kê: chọn dịch vụ · tồn thực tế đếm được ≥ 0 · **lý do bắt buộc** → sinh giao dịch `Adjust` mang số chênh (+/−) |

Đây là màn hình đầu tiên **ghi vào bảng giao dịch chỉ-thêm** (`InventoryTransaction`), nên phần
lớn quyết định thiết kế dưới đây xoay quanh chuyện giữ cho sổ sách không bao giờ lệch khỏi phiếu.

### File thêm mới

| File | Nội dung |
|---|---|
| `Models/ViewModels/InventoryViewModels.cs` | `InventoryStockItemViewModel`, `InventoryTransactionListItemViewModel`, `InventoryIndexViewModel`, `InventoryReceiveViewModel`, `InventoryAdjustViewModel` |
| `Services/InventoryService.cs` | `IInventoryService` + bản cài đặt: FillIndex (2 tab), BuildReceiveForm/Receive, BuildAdjustForm/Adjust |
| `Controllers/InventoryController.cs` | Index (Admin + Lễ tân), Receive/Adjust (chỉ Admin) |
| `Views/Inventory/Index.cshtml`, `Receive.cshtml`, `Adjust.cshtml` | |

### File sửa

| File | Sửa gì |
|---|---|
| `Services/ServiceResult.cs` | Thêm `Message` — câu thông báo thành công do service soạn (xem mục 2.6) |
| `Models/EnumDisplay.cs` | Thêm nhãn + màu cho `InventoryTransactionType` |
| `Views/Shared/_Sidebar.cshtml` | Thêm mục "Tồn kho" |
| `Program.cs` | Đăng ký `IInventoryService` |

Không phải sửa entity hay migration: `InventoryTransaction` đã có sẵn đủ trường
(`Type`, `Quantity`, `StockAfter`, `FolioItemId`, `Reason`) và chỉ mục `(HotelServiceId, CreatedAt)`.

---

## 2. Quyết định thiết kế

### 2.1 Tồn và phiếu luôn lưu trong **một** lần `SaveChangesAsync`
```csharp
service.StockQuantity += form.Quantity;
_db.InventoryTransactions.Add(new InventoryTransaction { ... StockAfter = service.StockQuantity });
_audit.Log(...);
await _db.SaveChangesAsync();   // một lần duy nhất cho cả ba
```
EF Core gói mọi thay đổi của một `SaveChanges` vào một transaction, nên không có cảnh tồn đã
tăng mà phiếu chưa kịp ghi. Đây là lý do **không** tách hàm "cập nhật tồn" riêng khỏi hàm
"ghi phiếu" — tách ra là mở đường cho người sau gọi thiếu một nửa.

### 2.2 Không có đường nào đổi tồn mà không sinh phiếu
Màn hình danh mục dịch vụ (SCR-A07) chỉ hiển thị tồn dạng chữ, không cho sửa
(xem [`03-catalog-services.md`](03-catalog-services.md) mục 2.2). Cộng với 2.1 ở trên thì
`StockQuantity` chỉ đổi được qua đúng hai action của màn hình này — và cả hai đều ghi phiếu
lẫn audit log.

### 2.3 Phiếu điều chỉnh lưu **phần chênh**, không lưu số đếm được
`Quantity = counted - bookStock` (âm khi thiếu, dương khi thừa), còn `StockAfter = counted`.
Đọc lịch sử phải thấy ngay "lệch bao nhiêu" mà không cần trừ nhẩm với dòng trước. Đây cũng là
cách duy nhất để cột số lượng có chung ý nghĩa với các loại giao dịch khác
(`Receive` dương, `Sale` âm).

### 2.4 Kiểm kê khớp sổ sách thì **không** ghi phiếu
Chênh lệch bằng 0 → chặn kèm thông báo *"Tồn thực tế bằng tồn sổ sách (200 chai), không có
chênh lệch để điều chỉnh."* Ghi một dòng `Adjust` với `Quantity = 0` chỉ làm loãng lịch sử,
trong khi thông tin "đã kiểm kê và khớp" thuộc về biên bản kiểm kê chứ không phải sổ kho.

### 2.5 Hai tab đi qua query string, không phải JavaScript
`/Inventory?tab=history&Type=1&page=2`. Mỗi tab có bộ lọc và phân trang riêng, nếu giữ trạng
thái tab bằng JS thì bấm sang trang 2 là mất tab, mà `_Pager` lại dựng URL từ chính query
string hiện tại. Cho tab vào URL thì phân trang, lọc, F5 và chia sẻ link đều đúng, không tốn
thêm dòng JS nào.

### 2.6 `ServiceResult.Message` — thông báo thành công do service soạn
Câu *"Đã nhập 50 chai — tồn mới của MB001 là 250 chai"* chỉ service mới dựng được (controller
không biết tồn sau khi nhập). Trước đây `ServiceResult` chỉ có `Warning`, mà dùng `Warning` cho
việc này thì thông báo hiện ra màu vàng như đang có vấn đề. Thêm `Message` (hiện ở kênh
`TempData["Success"]`) và giữ nguyên ý nghĩa cũ của `Warning` = "thành công nhưng cần lưu ý".

### 2.7 Ô chọn dịch vụ kèm luôn tồn hiện tại trong nhãn
`MB001 — Nước suối 500ml (tồn 200 chai)`. Người kiểm kê cần con số sổ sách ngay lúc chọn để
đối chiếu với số vừa đếm. Nhét vào nhãn thì không phải viết JS nạp tồn theo lựa chọn, cũng
không phải tách thêm một bước "chọn dịch vụ rồi bấm Tiếp".

### 2.8 Ba điều kiện chung của cả hai phiếu nằm một chỗ
`ValidateStockedService`: dịch vụ tồn tại · đang bật quản lý kho · chưa ngừng sử dụng.
Giao diện chỉ liệt kê dịch vụ hợp lệ, nhưng POST tay vẫn gửi được id bất kỳ nên server phải
kiểm lại — cùng lý lẽ với máy trạng thái phòng ở SCR-A05.

### 2.9 Bảng tồn vẫn hiện dịch vụ **đã ngừng** còn tồn
Để đối chiếu kho thực tế (hàng vẫn nằm trong kho dù không bán nữa). Dòng đó mờ đi, có huy hiệu
"Ngừng dùng" và **không** có nút Nhập kho / Điều chỉnh. Ngược lại, ô chọn dịch vụ ở hai form chỉ
liệt kê dịch vụ đang dùng.

### 2.10 Lịch sử kho không có bất kỳ action ghi nào
Không `Edit`, không `Delete`, kể cả cho Admin — đúng yêu cầu "chỉ đọc, không sửa, không xóa" của
SCR-A08. Màn hình có dòng nhắc rõ điều này để người dùng biết phải sửa bằng phiếu điều chỉnh.

---

## 3. Kết quả kiểm thử (20/09/2026)

Chạy thật trên `http://localhost:5265` bằng một script PowerShell duy nhất. **32/32 kiểm tra PASS.**

### SCR-A08 — Bảng tồn & phân quyền
| Kịch bản | Kết quả |
|---|---|
| Bảng tồn của Admin | Đúng 4 dịch vụ có quản lý kho: `MB001…MB004` (6 dịch vụ không quản lý kho không lọt vào) |
| Lễ tân xem tồn | 200, thấy dữ liệu, **không** có nút Nhập kho / Điều chỉnh |
| Lễ tân `GET /Inventory/Receive` · `/Adjust` | **403** cả hai |
| Lễ tân `POST /Inventory/Receive` (token hợp lệ) | **403**, tồn giữ nguyên 200 |
| Sidebar | Mục "Tồn kho" hiện với cả Admin và Lễ tân |

### SCR-A09 — Nhập kho
| Kịch bản | Kết quả |
|---|---|
| Nhập 50 chai vào `MB001` | Tồn 200 → 250 |
| Phiếu sinh ra | `Type=Receive`, `Quantity=50`, `StockAfter=250` |
| Ghi chú + người thực hiện | `Reason='HĐ NCC 0012345'`, `CreatedBy=1` (admin) |
| Audit log | `ReceiveInventory`: `200 chai -> 250 chai`, kèm lý do |
| Thông báo sau khi lưu | *"Đã nhập 50 chai — tồn mới của MB001 là 250 chai."* |
| Số lượng = 0 và số lượng âm | Chặn — *Số lượng nhập phải lớn hơn 0.* |
| Không chọn dịch vụ | Chặn — *Vui lòng chọn dịch vụ cần nhập kho.* |
| POST id dịch vụ **không** quản lý kho (`FB001`) | Chặn — *không bật quản lý kho*, không sinh phiếu |
| POST id dịch vụ **đã ngừng sử dụng** | Chặn — *đã ngừng sử dụng* |

### SCR-A09 — Điều chỉnh kiểm kê
| Kịch bản | Kết quả |
|---|---|
| Sổ sách 250, đếm được 240 | Tồn = 240; phiếu `Adjust` `Quantity=-10`, `StockAfter=240` |
| Lý do | Lưu đúng `Kiểm kê: vỡ 10 chai` |
| Audit log | `AdjustInventory`: `250 chai -> 240 chai` kèm lý do |
| Sổ sách 240, đếm được 245 | Phiếu `Quantity=+5`, `StockAfter=245` |
| Đếm đúng bằng sổ sách | Chặn — *không có chênh lệch để điều chỉnh*, **không** sinh phiếu rỗng |
| Thiếu lý do | Chặn — *Vui lòng nhập lý do điều chỉnh...* |
| Tồn thực tế âm | Chặn — *Tồn thực tế không được âm.* |

### SCR-A08 — Tab lịch sử
| Kịch bản | Kết quả |
|---|---|
| Thứ tự | Mới nhất lên đầu: Điều chỉnh · Điều chỉnh · Nhập |
| Cột số lượng | `+5` (xanh) và `-10` (đỏ) |
| Cột người thực hiện | Hiện tên nhân viên từ `CreatedBy` |
| Lọc theo loại = Nhập | Chỉ còn dòng `Nhập` |
| Lọc theo dịch vụ `FB001` | *Chưa có giao dịch kho nào* |
| Lễ tân mở tab lịch sử | 200, xem được |
| Dòng nhắc chỉ đọc | Hiện, và trang không có action sửa/xóa nào |

### Cảnh báo dưới định mức
| Kịch bản | Kết quả |
|---|---|
| Hạ tồn `MB002` xuống 5 (định mức 24) | Dòng `table-warning` + biểu tượng cảnh báo |
| Lọc "chỉ dịch vụ dưới định mức" | Đúng `MB002` |
| Banner đầu trang | Hiện số dịch vụ dưới định mức |

Sau khi chạy, script tự xóa toàn bộ `InventoryTransaction` và audit log kho vừa tạo, trả
`MB001` về 200, `MB002` về 120, `MB004` về `IsActive = 1` — DB quay lại đúng trạng thái seed.

---

## 4. Cách kiểm thử lại

```bash
dotnet build HotelManagement.Web.csproj
dotnet run --project HotelManagement.Web.csproj --launch-profile http
```

Tài khoản test: `admin` / `letan`, mật khẩu `matkhau123` (xem ghi chú ở
[`03-catalog-services.md`](03-catalog-services.md) mục 4). Các bẫy PowerShell 5.1 đã ghi ở đó
vẫn đúng: **lưu script kèm BOM UTF-8**, `@($x).Count`, đọc `TempData` ngay trong phản hồi POST.

> ⚠️ **Razor mã hóa dấu `+` thành `&#x2B;`.** Trình duyệt hiện ra "+5" hoàn toàn bình thường,
> nhưng script kiểm thử khớp chuỗi `'+5'` trên HTML thô sẽ trượt. Dùng
> `[System.Net.WebUtility]::HtmlDecode($html)` trước khi so.

---

## 5. Nợ kỹ thuật

| # | Món nợ | Ghi chú |
|---|---|---|
| 1 | **Trừ tồn khi bán dịch vụ** (`Sale`) chưa có | Sẽ làm cùng lúc với ghi dịch vụ vào folio (SCR-E02/F02); lúc đó mới dùng tới `AllowNegativeStock` và cột `FolioItemId` (giao diện lịch sử **đã** sẵn sàng hiện mã folio, hiện luôn rỗng vì chưa có dòng `Sale` nào) |
| 2 | Loại giao dịch **`Return`** (hoàn) chưa có đường sinh ra | Sẽ đi kèm nghiệp vụ hủy dòng dịch vụ trên folio (FR-E03) |
| 3 | Chưa có khóa lạc quan trên `HotelService.StockQuantity` | Hai phiếu nhập cùng một dịch vụ chạy đồng thời có thể ghi đè nhau (đọc-sửa-ghi). Một quán nhỏ, một admin thì chưa xảy ra; muốn chắc thì thêm `RowVersion` hoặc `UPDATE ... SET StockQuantity = StockQuantity + @n` |
| 4 | Chưa có báo cáo xuất/nhập/tồn theo kỳ | Thuộc nhóm G (FR-G05); dữ liệu đã đủ vì mỗi phiếu đều chốt `StockAfter` |
| 5 | Phân trang lịch sử chưa chạy qua mốc 20 dòng | Test chỉ tạo 3 giao dịch; `PagedList` + `_Pager` đã kiểm ở màn hình Phòng (31 bản ghi) |

---

## 6. Làm tiếp từ đâu

Nhóm A chỉ còn phần tài khoản và cấu hình:
1. ~~**SCR-A10/A11 — Nhân viên**~~ — đã làm, xem [`05-employees.md`](05-employees.md).
2. **SCR-A12 — Cấu hình hệ thống**: giờ chuẩn, VAT, phụ thu, chính sách cọc/hủy — luồng chính
   sẽ đọc các tham số này nên làm trước khi làm đặt phòng thì đỡ phải quay lại sửa.
3. Rồi tới **SCR-B01…B03 — Hồ sơ khách** và lát cắt dọc luồng chính.
