# 03 — Danh mục Dịch vụ (SCR-A06, SCR-A07)

| | |
|---|---|
| **Trạng thái** | ✅ Hoàn thành, đã kiểm thử end-to-end trên app chạy thật |
| **Cập nhật lần cuối** | 20/09/2026 |
| **Màn hình** | SCR-A06, SCR-A07 (`docs/screens/02-catalog.md`) |
| **Yêu cầu** | FR-A04, BR-11, BR-12 |

---

## 1. Đã làm gì

| Màn hình | URL | Nội dung |
|---|---|---|
| SCR-A06 | `GET /HotelServices` | Danh sách dịch vụ: lọc theo mã/tên, nhóm, chỉ dịch vụ dưới định mức, ẩn/hiện dịch vụ đã ngừng; tô vàng dòng dưới định mức tồn; phân trang; nút Ngừng sử dụng |
| SCR-A07 | `GET/POST /HotelServices/Create`, `/Edit/{id}` | Form thêm/sửa dịch vụ, khối tham số kho bật/tắt được, kiểm tra ràng buộc, audit log khi đổi giá và khi bật/tắt kho |

Đây là bộ màn hình **nhân bản đúng khuôn mẫu** đã dựng ở
[`02-catalog-rooms.md`](02-catalog-rooms.md) mục 2 — không phát sinh khuôn mẫu mới, chỉ thêm
phần nghiệp vụ riêng của kho (BR-12).

### File thêm mới

| File | Nội dung |
|---|---|
| `Models/ViewModels/HotelServiceViewModels.cs` | `HotelServiceListItemViewModel`, `HotelServiceIndexViewModel`, `HotelServiceFormViewModel` |
| `Services/ServiceCatalogService.cs` | `IServiceCatalogService` + bản cài đặt: Search, GetForEdit, Create, Update, Deactivate |
| `Views/HotelServices/_Form.cshtml` | Form dùng chung Thêm/Sửa, khối tham số kho ẩn/hiện |

### File sửa

| File | Sửa gì |
|---|---|
| `Controllers/HotelServicesController.cs` | Viết lại toàn bộ: bỏ `Pending()`, gọi service, gắn lỗi vào `ModelState`, thêm action `Deactivate` |
| `Views/HotelServices/Index.cshtml` | Thay `_Placeholder` bằng bảng thật + bộ lọc + banner cảnh báo tồn |
| `Views/HotelServices/Create.cshtml`, `Edit.cshtml` | Dùng `_Form` |
| `Models/EnumDisplay.cs` | Thêm `ServiceCategory.ToBadgeClass()` |
| `Program.cs` | Đăng ký `IServiceCatalogService` |

Không phải sửa entity, `DbContext` hay migration: `HotelService` đã có đủ trường
(`IsStockManaged`, `StockQuantity`, `MinStockLevel`, `AllowNegativeStock`, `IsActive`) và
đã có chỉ mục duy nhất trên `Code`.

---

## 2. Quyết định thiết kế

### 2.1 Tên lớp là `ServiceCatalogService`, không phải `HotelServiceService`
`HotelService` là entity (đặt tên vậy để tránh trùng `IService` của Microsoft.Extensions).
Ghép thêm hậu tố `Service` lần nữa thành `HotelServiceService` thì đọc không ra nghĩa, nên lớp
nghiệp vụ lấy tên `ServiceCatalogService` — "danh mục dịch vụ".

### 2.2 Màn hình danh mục **không** đổi được số tồn
Form Sửa chỉ hiển thị tồn hiện tại ở dạng chữ. Muốn đổi tồn phải đi qua SCR-A08/A09 để luôn có
phiếu, có lý do và có `InventoryTransaction`. Đây là cùng một lý lẽ với việc trạng thái phòng
không sửa được ở form Sửa phòng (xem `02-catalog-rooms.md` mục 3.2): **mở hai đường vào cùng
một dữ liệu thì đường nào không ghi nhật ký sẽ thành đường vòng để né nhật ký.**

### 2.3 Bật kho thì tồn khởi tạo bằng 0, kể cả với dịch vụ đã có sẵn
Theo SCR-A07. Service tự gán `StockQuantity = 0` khi phát hiện `false → true`, không tin giá
trị nào gửi lên từ form. Tắt kho thì **không** xóa `StockQuantity` — số cũ giữ nguyên để nếu
bật lại còn đối chiếu được với lịch sử `InventoryTransaction`, nhưng `MinStockLevel` và
`AllowNegativeStock` bị đưa về 0/false vì hai tham số đó vô nghĩa khi không theo dõi tồn.

### 2.4 Hai tham số kho được server tự dọn, không chỉ ẩn bằng JS
`_Form.cshtml` ẩn khối tham số kho bằng JavaScript cho gọn mắt, nhưng service vẫn viết:
```csharp
entity.MinStockLevel = form.IsStockManaged ? form.MinStockLevel : 0;
entity.AllowNegativeStock = form.IsStockManaged && form.AllowNegativeStock;
```
Ẩn ở giao diện không phải là lớp phòng thủ — POST tay vẫn gửi được `AllowNegativeStock=true`
kèm `IsStockManaged=false`.

### 2.5 Số đếm ở banner cảnh báo không chạy theo bộ lọc
`LowStockCount` đếm trên **toàn bộ** danh mục đang dùng. Nếu để nó đổi theo bộ lọc thì lọc xong
lại tưởng đã hết hàng cần nhập. Bấm "Xem danh sách" ở banner sẽ bật bộ lọc `LowStockOnly`.

### 2.6 Ngưỡng cảnh báo dùng `<=`, không phải `<`
`StockQuantity <= MinStockLevel`. Tồn đúng bằng định mức tối thiểu đã là lúc phải đặt hàng,
báo sau một đơn vị là muộn.

### 2.7 Ngừng sử dụng không kèm điều kiện chặn như loại phòng
Loại phòng bị chặn khi còn phòng hoạt động, vì phòng bắt buộc phải trỏ tới một loại phòng.
Dịch vụ thì không có ràng buộc tương đương: `FolioItem` đã chép sẵn tên và đơn giá, nên ngừng
một dịch vụ không làm hỏng hóa đơn cũ. Vẫn **không xóa cứng** (nguyên tắc chung nhóm A) —
chỉ `IsActive = false`, và nếu còn tồn thì hiện cảnh báo nhắc số tồn vẫn được giữ để đối chiếu.

### 2.8 Bộ lọc nhận cả ViewModel làm tham số
`Index(HotelServiceIndexViewModel filter, int page)` — màn hình này có 4 điều kiện lọc, nhận
rời từng tham số như `RoomTypes` sẽ dài và dễ sót khi thêm bộ lọc mới. Service nhận chính
ViewModel đó, điền `Results` + `LowStockCount` rồi trả lại.

---

## 3. Kết quả kiểm thử (20/09/2026)

Chạy thật trên `http://localhost:5265` bằng một script PowerShell duy nhất
(đăng nhập → thao tác → kiểm tra DB → dọn dữ liệu). **21/21 kiểm tra PASS.**

### Danh sách & phân quyền
| Kịch bản | Kết quả |
|---|---|
| Admin xem danh sách | 200, 10 dịch vụ, có nút Thêm |
| Lễ tân xem danh sách | 200, không có nút Thêm, cột thao tác hiện "Chỉ xem" |
| Lọc theo mã `MB001` | Đúng 1 dòng: `MB001` |
| Lọc nhóm Giặt ủi | Đúng 2 dòng: `LD001`, `LD002` |
| Lễ tân `GET /HotelServices/Create` | **403** |
| Lễ tân `POST /HotelServices/Create` (có token hợp lệ) | **403**, DB không có bản ghi |

### Cảnh báo tồn kho
| Kịch bản | Kết quả |
|---|---|
| Hạ tồn `MB002` xuống 5 (định mức 24) | Dòng có class `table-warning` + biểu tượng cảnh báo |
| Lọc "chỉ dịch vụ dưới định mức" | Đúng `MB002` |
| Banner đầu trang | Hiện "Có 1 dịch vụ đang ở dưới định mức tồn tối thiểu" |

### Thêm dịch vụ
| Kịch bản | Kết quả |
|---|---|
| Thêm hợp lệ (`MB999`, 30.000 ₫/chai) | Lưu đúng, `IsActive = true` |
| Trùng mã | Chặn — *Mã dịch vụ "MB999" đã tồn tại.* |
| Mã chữ thường `mb888` | Chặn — *chỉ dùng chữ in hoa và số* |
| Đơn giá = 0 | Chặn — *Đơn giá phải lớn hơn 0.* |
| Thiếu đơn vị tính | Chặn — *Vui lòng nhập đơn vị tính.* |
| Thêm dịch vụ có kho (`MB998`, định mức 10) | `StockQuantity = 0`, `MinStockLevel = 10` |

### Sửa dịch vụ & audit log (BR-11)
| Kịch bản | Kết quả |
|---|---|
| Sửa tên + giá 30.000 → 45.000 | Lưu đúng |
| POST mã khác (`DOIMA`) khi sửa | Bị bỏ qua, DB vẫn giữ `MB999` |
| Audit đổi giá | `ChangeHotelServicePrice`: `30,000 ₫/chai -> 45,000 ₫/chai` |
| Bật quản lý kho (đang có tồn 0) | `StockQuantity = 0`, `MinStockLevel = 15`; audit `EnableStockTracking` |
| Tắt quản lý kho (tồn 40) | `MinStockLevel = 0`, `AllowNegativeStock = false`; audit `DisableStockTracking`: `Có quản lý kho, tồn 40 -> Không quản lý kho` |

### Ngừng sử dụng
| Kịch bản | Kết quả |
|---|---|
| Ngừng dịch vụ | `IsActive = false` |
| Danh sách mặc định | Không còn thấy; tick "Hiện cả dịch vụ đã ngừng" thì thấy lại |
| Ngừng lần thứ hai | Chặn — *Dịch vụ này đã ngừng sử dụng.* |

Sau khi chạy, script tự xóa `MB999`, `MB998` cùng audit log của chúng và trả tồn `MB002`
về 120 — DB quay lại đúng 10 dịch vụ seed.

---

## 4. Cách kiểm thử lại

```bash
dotnet build HotelManagement.Web.csproj
dotnet run --project HotelManagement.Web.csproj --launch-profile http
```

> ⚠️ **Mật khẩu tài khoản test đã đổi.** Lần chạy này `admin` và `letan` bị đặt lại thành
> `matkhau123` (và `MustChangePassword = 0`) vì mật khẩu từ phiên làm việc trước không còn
> tra ra được. Muốn về trạng thái seed gốc (`123456` + ép đổi lần đầu) thì xóa DB rồi chạy lại
> app, theo hướng dẫn ở [`01-authentication-authorization.md`](01-authentication-authorization.md) mục 5.

> ⚠️ **Script PowerShell phải lưu kèm BOM UTF-8.** Windows PowerShell 5.1 đọc file không BOM
> theo bảng mã ANSI, tiếng Việt trong chuỗi biến thành ký tự lạ và script **lỗi cú pháp**
> chứ không chỉ hiển thị sai. Mất một lượt chạy vì chuyện này.

> ⚠️ Vài bẫy PowerShell 5.1 khác đã dính: `$home` là biến chỉ đọc (đừng dùng làm biến tạm);
> `.Count` trên một `PSCustomObject` đơn lẻ trả về rỗng — phải bọc `@($x).Count`;
> `TempData` chỉ sống đúng một request nên phải đọc thông báo lỗi ngay trong phản hồi của POST
> (Invoke-WebRequest đã tự đi theo redirect), GET lại lần nữa là mất.

> ⚠️ Khi kiểm tra bộ lọc, đừng khẳng định bằng `-notmatch 'MB001'` trên cả trang: chuỗi `MB001`
> còn nằm trong `placeholder` của ô tìm kiếm. Hãy bóc danh sách mã trong thẻ `<code>` rồi so.

---

## 5. Nợ kỹ thuật

| # | Món nợ | Ghi chú |
|---|---|---|
| 1 | ~~**SCR-A08/A09 — Tồn kho** chưa làm~~ — đã trả xong 20/09/2026 | Xem [`04-inventory.md`](04-inventory.md) |
| 2 | Trừ tồn khi bán dịch vụ (BR-12) chưa có | Sẽ làm cùng lúc với ghi dịch vụ vào folio (SCR-E02/F02); `AllowNegativeStock` hiện mới được lưu chứ chưa ai đọc |
| 3 | `HasFolioHistory` mới dùng để hiện dòng nhắc | Khi có nút Xóa cứng cho dịch vụ chưa từng dùng thì dùng lại cờ này — giống món nợ "xóa mềm phòng" ở `02-catalog-rooms.md` |
| 4 | Chưa kiểm thử được "sửa giá không ảnh hưởng dòng folio cũ" | Cần có `Folio` thật; logic nằm ở chỗ `FolioItem` chép `UnitPrice` lúc ghi nhận, sẽ kiểm được khi làm nhóm F |
| 5 | Phân trang chưa chạy qua mốc 20 dòng | DB seed chỉ có 10 dịch vụ nên luôn 1 trang; dùng chung `PagedList` + `_Pager` đã kiểm ở màn hình Phòng (31 bản ghi) |

---

## 6. Làm tiếp từ đâu

1. ~~**SCR-A08/A09 — Tồn kho**~~ — đã làm, xem [`04-inventory.md`](04-inventory.md); FR-A05 và
   BR-12 đã đóng phần danh mục, chỉ còn phần trừ tồn lúc bán.
2. **SCR-A10/A11 — Nhân viên**, **SCR-A12 — Cấu hình**: nốt nhóm A.
3. **SCR-B01…B03 — Hồ sơ khách**, rồi tới lát cắt dọc luồng chính
   (tra phòng trống → đặt phòng → check-in → folio → check-out → thanh toán).

Nhóm A còn lại đều là danh mục, nhân bản khuôn mẫu là xong; đừng dồn thêm thời gian vào đó khi
luồng chính vẫn chưa chạy.
