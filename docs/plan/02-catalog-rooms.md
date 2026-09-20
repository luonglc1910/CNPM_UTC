# 02 — Danh mục Loại phòng & Phòng (SCR-A01…A05)

| | |
|---|---|
| **Trạng thái** | ✅ Hoàn thành, đã kiểm thử end-to-end trên app chạy thật |
| **Cập nhật lần cuối** | 20/09/2026 |
| **Màn hình** | SCR-A01, SCR-A02, SCR-A03, SCR-A04, SCR-A05 (`docs/screens/02-catalog.md`) |
| **Yêu cầu** | FR-A01, FR-A02, FR-A03, BR-02, BR-11 |

---

## 1. Đã làm gì

Đây là **bộ màn hình nghiệp vụ thật đầu tiên** của dự án, nên ngoài 5 màn hình còn có nhiệm vụ
đặt khuôn mẫu cho hơn 40 màn hình còn lại.

| Màn hình | URL | Nội dung |
|---|---|---|
| SCR-A01 | `/RoomTypes` | Danh sách loại phòng: tìm kiếm, ẩn/hiện loại đã ngừng, phân trang, ngừng sử dụng |
| SCR-A02 | `/RoomTypes/Create`, `/Edit/{id}` | Form thêm/sửa, chọn tiện nghi, kiểm tra ràng buộc, audit log khi đổi giá |
| SCR-A03 | `/Rooms` | Danh sách phòng: 2 chế độ (bảng / lưới theo tầng), lọc tầng–loại–trạng thái–số phòng |
| SCR-A04 | `/Rooms/Create`, `/Edit/{id}` | Form thêm/sửa phòng, chặn đổi loại khi có khách, cảnh báo đơn tương lai |
| SCR-A05 | `/Rooms/UpdateStatus/{id}` | Đổi trạng thái theo máy trạng thái, bắt buộc lý do, audit log |

---

## 2. Khuôn mẫu đã thiết lập (dùng lại cho mọi màn hình sau)

| Thành phần | File tham chiếu |
|---|---|
| **Tầng Service** (`NFR-08`: Controller → Service → EF Core) | `Services/RoomTypeService.cs`, `Services/RoomService.cs` |
| **Kết quả nghiệp vụ** có phân biệt lỗi / cảnh báo | `Services/ServiceResult.cs` |
| **Phân trang** 20 dòng/trang | `Models/ViewModels/PagedList.cs` + `Views/Shared/_Pager.cshtml` |
| **Nhãn & màu enum** | `Models/EnumDisplay.cs` + `Views/Shared/_RoomStatusBadge.cshtml` |
| **Form dùng chung Create/Edit** | `Views/RoomTypes/_Form.cshtml`, `Views/Rooms/_Form.cshtml` |
| **Ẩn nút theo quyền** | `@if (User.IsInRole(Roles.Admin))` trong các view Index |
| **Thông báo** | `TempData["Success"] / ["Warning"] / ["Error"]` |

### Ba quy ước quan trọng

**a. Service trả `ServiceResult`, không ném exception cho lỗi nghiệp vụ.**
`ServiceResult.Fail(message, field)` cho phép controller gắn lỗi đúng vào ô nhập:
```csharp
var result = await _service.CreateAsync(form);
if (!result.Succeeded)
{
    ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
    return View(form);
}
```

**b. `Warning` khác `Error`.** Thao tác vẫn thành công nhưng có điều người dùng cần biết —
ví dụ giảm sức chứa tối đa xuống dưới số khách đang ở, hoặc đưa phòng vào bảo trì khi còn đơn đặt.

**c. Ràng buộc liên trường nằm ở Service, không ở DataAnnotations.**
"Sức chứa tối đa ≥ sức chứa chuẩn" không diễn đạt được bằng attribute nên đặt trong
`RoomTypeService.ValidateCapacity`.

---

## 3. Quyết định thiết kế

### 3.1 Mã loại phòng không sửa được sau khi tạo
View hiện ô `readonly`, nhưng **quan trọng hơn là server bỏ qua hoàn toàn giá trị `Code` gửi lên**
trong `UpdateAsync`. Chỉ khóa ở giao diện là vô nghĩa — đã kiểm thử bằng cách POST mã `HACKED`,
DB vẫn giữ `FAM`.

### 3.2 Trạng thái phòng không sửa được ở form Sửa phòng
`RoomService.UpdateAsync` **không** gán `entity.Status`. Muốn đổi trạng thái phải đi qua SCR-A05
để bắt buộc có lý do và audit log. Nếu cho sửa ở cả hai chỗ thì sẽ có đường vòng né mất nhật ký.

### 3.3 Máy trạng thái khai báo thành bảng, kiểm ở server
`RoomService.AllowedTransitions` là `Dictionary<RoomStatus, RoomStatus[]>`. Giao diện chỉ liệt kê
trạng thái hợp lệ, nhưng server **luôn kiểm lại** — không tin dữ liệu từ client.
`Occupied` ánh xạ tới mảng rỗng, nên phòng đang có khách tự động không đổi tay được.

### 3.4 Lưới theo tầng không phân trang
Sơ đồ phòng mất ý nghĩa nếu bị cắt trang, nên chế độ `view=grid` nạp toàn bộ danh sách;
chế độ bảng mới phân trang. Xem `RoomService.FillIndexAsync`.

### 3.5 "Ngừng khai thác" phòng dùng `Status = OutOfService`, không phải `IsActive = false`
Hai khái niệm khác nhau: `OutOfService` là trạng thái nghiệp vụ (quay lại được, không tính vào
mẫu số công suất — SCR-G02); `IsActive = false` là xóa mềm bản ghi khỏi danh mục.
Hiện chỉ `RoomType` có nút ngừng sử dụng (`IsActive`); phòng thì dùng SCR-A05.

---

## 4. Kết quả kiểm thử (20/09/2026)

Đã chạy thật trên `http://localhost:5265`, không chỉ biên dịch.

### SCR-A01 / A02 — Loại phòng
| Kịch bản | Kết quả |
|---|---|
| Admin xem danh sách | 3 loại, có nút Thêm |
| Lễ tân xem danh sách | Không có nút Thêm, cột thao tác hiện "Chỉ xem" |
| Tìm "DLX" | 1 kết quả |
| Thêm hợp lệ | 302 → danh sách |
| Trùng mã | Chặn — *Mã loại phòng "FAM" đã tồn tại.* |
| Mã chữ thường | Chặn — *Mã loại phòng gồm 2–10 ký tự, chỉ dùng chữ in hoa và số.* |
| Sức chứa tối đa < chuẩn | Chặn — *Sức chứa tối đa phải lớn hơn hoặc bằng sức chứa chuẩn.* |
| Giá = 0 | Chặn — *Giá mỗi đêm phải lớn hơn 0.* |
| Thiếu tên | Chặn — *Vui lòng nhập tên loại phòng.* |
| Lễ tân POST Create | **403** |
| Đổi giá 2.5tr → 2.8tr | Lưu + audit log `2,500,000 ₫/đêm -> 2,800,000 ₫/đêm` |
| POST mã khác khi sửa | Bị bỏ qua, DB vẫn giữ mã cũ |
| Ngừng loại còn 20 phòng | Chặn, `IsActive` vẫn `True` |
| Ngừng loại không còn phòng | Thành công, `IsActive = False` |

### SCR-A03 / A04 — Phòng
| Kịch bản | Kết quả |
|---|---|
| Phân trang | Trang 1/2, tổng 31 phòng |
| Lọc tầng 4 | 1 phòng |
| Lọc trạng thái Trống | 20 phòng |
| Chế độ lưới | 4 tầng |
| Phân trang giữ bộ lọc | `/Rooms?status=1&page=1` — giữ nguyên `status` |
| Lễ tân | Không thấy nút Thêm, **vẫn thấy** nút Đổi trạng thái |
| Thêm phòng hợp lệ | 30 → 31 phòng |
| Trùng số phòng | Chặn — *Số phòng "401" đã tồn tại.* |
| Không chọn loại phòng | Chặn — *Vui lòng chọn loại phòng.* |
| Lễ tân POST Create | **403** |

### SCR-A05 — Máy trạng thái
| Chuyển | Kết quả |
|---|---|
| Trống → Chờ dọn | ✅ |
| Chờ dọn → Trống (**lễ tân**) | ✅ |
| Trống → Bảo trì, **không** lý do | ❌ *Vui lòng nhập lý do khi chuyển sang "Bảo trì".* |
| Trống → Bảo trì, có lý do | ✅ lý do lưu vào `StatusNote` |
| Bảo trì → Trống | ❌ *Không thể chuyển từ "Bảo trì" sang "Trống".* (phải qua Chờ dọn) |
| Bảo trì → Chờ dọn | ✅ |
| Đang ở → Trống | ❌ *Phòng đang có khách. Trạng thái chỉ thay đổi qua check-out hoặc đổi phòng.* |
| Phòng còn đơn đặt tương lai | Hộp thoại liệt kê đơn; sau khi chuyển hiện cảnh báo *"còn 1 đơn đặt trong tương lai (RSV-TEST-0001 (23/09–25/09)). Cần xếp lại phòng"* |

Audit log ghi đủ 4 lần đổi trạng thái với `OldValue → NewValue`, lý do và người thực hiện
(có cả dòng do `letan` thực hiện).

---

## 5. Cách kiểm thử lại

Xem hướng dẫn chung ở [`01-authentication-authorization.md`](01-authentication-authorization.md) mục 5.

> ⚠️ **Bài học từ lần này:** biến và session PowerShell **không** giữ được giữa các lệnh riêng lẻ —
> phải gộp cả kịch bản (đăng nhập → đổi mật khẩu → thao tác → kiểm tra DB) vào **một script duy nhất**.
> Chạy rải rác sẽ khiến mật khẩu và trạng thái lệch nhau, mất thời gian dò lỗi không có thật.

> ⚠️ Antiforgery: action chỉ có POST (như `RoomTypes/Deactivate`) không có trang GET để lấy token.
> Phải lấy token từ trang danh sách rồi POST sang — token gắn theo **phiên**, không theo từng form.

---

## 6. Nợ kỹ thuật

| # | Món nợ | Ghi chú |
|---|---|---|
| 1 | **Xóa mềm phòng** (`IsActive = false`) chưa có nút | Docs SCR-A04 nói "nút Xóa chỉ hiện với phòng chưa từng dùng". Service đã lọc `IsActive` ở danh sách, chỉ thiếu nút và hàm |
| 2 | **Cảnh báo đổi loại phòng khi có đơn tương lai** chưa chạy thật | Logic có trong `RoomService.UpdateAsync` + ô xác nhận trong `_Form.cshtml`, nhưng chưa test được vì cần dữ liệu đặt phòng; nhánh `GetFutureReservationsAsync` **đã** được kiểm qua SCR-A05 |
| 3 | **Chặn đổi loại phòng khi đang có khách** chưa chạy thật | Cần có `Stay` thật; sẽ kiểm được sau khi làm check-in (SCR-D02) |
| 4 | Cột "Khách đang ở" ở SCR-A03 luôn rỗng | Đúng như mong đợi — chưa có `Stay` nào. Link trỏ tới `FrontDesk/Stay` chưa tồn tại, sẽ sống khi làm SCR-D04 |
| 5 | Chưa có màn hình **Dịch vụ** (SCR-A06/A07), **Tồn kho** (A08/A09), **Nhân viên** (A10/A11), **Cấu hình** (A12) | Nhân bản khuôn mẫu từ `RoomTypeService` là nhanh nhất |

---

## 7. Làm tiếp từ đâu

Theo thứ tự ở [`README.md`](README.md):
1. **SCR-A06/A07 — Dịch vụ**: gần như bản sao của Loại phòng, thêm phần tồn kho.
2. **SCR-B01…B03 — Hồ sơ khách**: cần trước khi làm đặt phòng.
3. **Lát cắt dọc luồng chính**: SCR-C02 tra phòng trống → SCR-C04 đặt phòng → SCR-D02 check-in →
   SCR-F02 folio → SCR-D08 check-out → SCR-F05 thanh toán. Đây mới là phần khó và quan trọng nhất.
