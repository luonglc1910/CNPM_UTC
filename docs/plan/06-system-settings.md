# 06 — Cấu hình hệ thống (SCR-A12)

| | |
|---|---|
| **Trạng thái** | ✅ Hoàn thành, đã kiểm thử end-to-end trên app chạy thật |
| **Cập nhật lần cuối** | 20/09/2026 |
| **Màn hình** | SCR-A12 (`docs/screens/02-catalog.md`) |
| **Yêu cầu** | FR-A07, BR-01, BR-03, BR-04, BR-05, BR-11, BR-12 |

> Màn hình này **đóng lại toàn bộ nhóm A** — danh mục & cấu hình hệ thống đã xong hết.

---

## 1. Đã làm gì

`GET/POST /Settings` — chỉ Admin, lễ tân vào là 403. Sáu nhóm tham số, mỗi nhóm là một form
độc lập có nút **Lưu** và nút **Khôi phục mặc định** riêng:

| # | Nhóm | Tham số |
|---|---|---|
| 1 | Thông tin khách sạn | Tên · địa chỉ · điện thoại · mã số thuế · **logo** (tải file thật, in trên hóa đơn) |
| 2 | Giờ chuẩn (BR-01) | Giờ nhận phòng · giờ trả phòng |
| 3 | Thuế & làm tròn (BR-04) | VAT (%) · đơn vị làm tròn |
| 4 | Phụ thu (BR-03) | Bảy dòng của bảng BR-03: ba mốc giờ + bốn tỷ lệ % giá đêm |
| 5 | Cọc & hủy (BR-05) | Mức cọc đề xuất (số đêm) · giờ hết hạn giữ chỗ · ba mức phí hủy |
| 6 | Hạn mức nghiệp vụ | Hạn mức giảm giá của lễ tân (₫ và %) · cho phép bán khi hết tồn · ngưỡng khách quen · tuổi trẻ em |

### File thêm mới

| File | Nội dung |
|---|---|
| `Data/SystemSettingDefaults.cs` | Bảng giá trị mặc định + tên nhóm — một nguồn sự thật duy nhất (mục 2.1) |
| `Models/ViewModels/SettingsViewModels.cs` | `SettingsViewModel` + sáu lớp con, mỗi nhóm một lớp |
| `Services/SettingsService.cs` | Đọc/ghi, chuyển đổi kiểu, audit log theo từng khóa, khôi phục mặc định |
| `Controllers/SettingsController.cs` | Sáu action lưu + `ResetGroup` + xử lý tải logo |
| `Views/Settings/Index.cshtml` | Sáu thẻ cấu hình |

### File sửa

| File | Sửa gì |
|---|---|
| `Models/Entities/SystemSetting.cs` | Thêm 6 khóa: logo, 3 mốc giờ phụ thu, số đêm cọc, công tắc bán khi hết tồn |
| `Data/DbInitializer.cs` | Bỏ bảng mặc định chép tay, dùng `SystemSettingDefaults` |
| `Program.cs` | Đăng ký `ISettingsService`; thêm middleware tĩnh cho `wwwroot/uploads` (mục 2.6) |
| `Views/Shared/_Sidebar.cshtml` | Thêm mục "Cấu hình hệ thống" |
| `.gitignore` | Bỏ qua `wwwroot/uploads/` — file người dùng tải lên không phải mã nguồn |

Không cần migration: `SystemSetting` vốn là bảng khóa–giá trị, thêm tham số mới chỉ là thêm dòng.

---

## 2. Quyết định thiết kế

### 2.1 Giá trị mặc định chỉ có một nơi khai báo
Trước đây bảng mặc định nằm trong `DbInitializer`. Nút "Khôi phục mặc định" cần đúng bảng đó,
mà chép sang chỗ thứ hai thì sớm muộn hai bên lệch nhau — lúc ấy "khôi phục mặc định" sẽ ra giá
trị khác với lúc cài mới, và không ai phát hiện ra. Nên bảng được tách hẳn thành
`SystemSettingDefaults.All`, cả seeder lẫn màn hình cấu hình đều đọc từ đó.

Seeder vẫn **chỉ thêm khóa còn thiếu**, không đụng khóa đã có — nhờ vậy bản mới thêm 6 tham số
vẫn chạy thẳng trên DB cũ, không cần migration và không đè mất giá trị người dùng đã chỉnh.

### 2.2 Mỗi nhóm một form riêng
SCR-A12 đòi nút "Khôi phục mặc định" cho **từng nhóm**, nên nhóm là đơn vị thao tác tự nhiên.
Một form lớn cho cả trang sẽ có hai điều dở: sửa một ô ở nhóm 6 mà nhóm 1 sai định dạng thì
không lưu được gì cả, và audit log không biết người dùng thực sự định đổi nhóm nào.

Vì các form nằm chung một trang, mỗi action nhận dữ liệu bằng `[Bind(Prefix = "Tax")]` để tên
trường gửi lên (`Tax.VatPercent`) khớp đúng khóa `ModelState` mà view dùng khi hiện lại lỗi.
Không có prefix thì thông báo lỗi sẽ không gắn được vào ô nhập.

### 2.3 Hiện lỗi mà không mất dữ liệu đang nhập
Khi một nhóm lỗi, controller nạp lại năm nhóm kia từ DB rồi **ghi đè nhóm bị lỗi bằng đúng giá
trị vừa gõ** (`Reload(group, vm => vm.Tax = form)`). Nếu nạp lại cả sáu nhóm từ DB thì người dùng
gõ sai một ô là mất sạch những gì vừa nhập ở nhóm đó.

### 2.4 DB lưu tỷ lệ, giao diện hiển thị phần trăm
`Tax.VatRate = 0.08` trong DB nhưng ô nhập hiện `8`. Lưu tỷ lệ để công thức tính tiền sau này
nhân thẳng, không phải nhớ chia 100 ở mười chỗ khác nhau; hiện phần trăm vì người dùng nghĩ
theo phần trăm. Toàn bộ việc đổi qua lại gom trong `SettingsService`.

Một chi tiết nhỏ nhưng đáng nhớ: `Math.Round(0.08m * 100, 2)` ra `8.00`, và ô nhập sẽ hiện
"8.00" — trông như người dùng gõ sai. Phải đọc lại qua chuỗi `"0.##"` để bỏ phần thập phân thừa.

### 2.5 Chỉ ghi nhật ký khóa **thực sự đổi**
`ApplyAsync` so từng khóa với giá trị đang có; bấm Lưu mà không sửa gì thì không sinh dòng nhật
ký nào và báo "không có thay đổi nào". Nhật ký cấu hình mà đầy dòng "A → A" thì sau này không
tra ra được gì. Ghi log **theo từng khóa** (`EntityId = khóa`) chứ không gộp cả nhóm, để tra
lịch sử một tham số là ra đúng tham số đó.

### 2.6 Logo tải lên phải có middleware tĩnh riêng
`MapStaticAssets` của .NET 9+ phục vụ theo **manifest dựng lúc build**, nên file tải lên lúc chạy
sẽ trả 404 dù nằm đúng trong `wwwroot`. Phải thêm `UseStaticFiles` trỏ riêng vào
`wwwroot/uploads` với `RequestPath = "/uploads"`. Đây là loại lỗi rất dễ mất thời gian vì file
nhìn thấy trên đĩa mà trình duyệt vẫn báo không có.

An toàn khi nhận file: kiểm `ContentType` theo danh sách trắng (PNG/JPG/WEBP), giới hạn 1 MB, và
**không bao giờ dùng tên file người dùng gửi lên** — luôn ghi thành `hotel-logo.<ext>` cố định.
Như vậy vừa tránh path traversal vừa không tích rác qua mỗi lần đổi logo. Đổi sang định dạng khác
hoặc bỏ logo thì xóa luôn file cũ.

### 2.7 Hai dòng cuối của bảng BR-03 không nằm ở đây
"Thêm người" và "thêm giường phụ" là phí **theo từng loại phòng** (đã có ở SCR-A02), không phải
tham số toàn hệ thống. Bảng phụ thu vẫn hiện đủ 7 dòng cho khớp BR-03, nhưng hai dòng đó ở dạng
chữ mờ kèm link sang màn hình Loại phòng — nhân đôi dữ liệu ra đây thì chỉ tạo thêm một chỗ nữa
để sai.

### 2.8 Ba mốc giờ trả phòng phải tăng dần
Kiểm liên trường trong service: mốc "tính thêm 1 đêm" phải muộn hơn mốc kết thúc bậc 1. Không
kiểm thì có khoảng giờ không rơi vào bậc nào, hoặc rơi vào hai bậc cùng lúc — và lỗi đó chỉ lộ
ra lúc tính tiền cho khách.

### 2.9 "Cho phép bán khi hết tồn" là công tắc tổng
Tham số này (BR-12) và cờ `AllowNegativeStock` của từng dịch vụ (SCR-A07) là **và** chứ không
phải **hoặc**: dịch vụ chỉ bán được khi hết tồn nếu bật cả hai. Màn hình ghi rõ điều này ngay
dưới ô chọn để không ai hiểu nhầm là bật một chỗ đã đủ.

---

## 3. Kết quả kiểm thử (20/09/2026)

Chạy thật trên `http://localhost:5265` bằng một script PowerShell duy nhất. **33/33 kiểm tra PASS.**

### Mở màn hình
| Kịch bản | Kết quả |
|---|---|
| Lễ tân mở `/Settings` | **403** |
| Admin mở | 200, đủ 6 nhóm, mỗi nhóm có nút Khôi phục mặc định |
| Dòng nhắc "chỉ áp cho lượt lưu trú mới" | Có ở nhóm Giờ chuẩn và Phụ thu |
| Hai dòng phí theo loại phòng | Hiện dạng chỉ dẫn sang màn hình Loại phòng |
| Seed sau khi thêm 6 khóa mới | 26 khóa, DB cũ tự bổ sung khóa thiếu, không mất giá trị cũ |

### Nhóm 1 — Thông tin khách sạn & logo
| Kịch bản | Kết quả |
|---|---|
| Đổi tên + điện thoại | Lưu đúng; audit `ChangeSetting`: `Khách sạn UTC -> Khách sạn UTC Test` |
| Bấm Lưu mà không sửa gì | *"không có thay đổi nào"*, **không** sinh dòng nhật ký |
| Thiếu tên khách sạn | Chặn |
| Mã số thuế `abc` | Chặn, và ô nhập **vẫn giữ** giá trị vừa gõ |
| Tải logo PNG 1×1 | Lưu `/uploads/hotel-logo.png`, `GET /uploads/hotel-logo.png` = **200**, trang hiện preview |
| Tải file `.txt` (content-type `text/plain`) | Chặn — *Logo phải là ảnh PNG, JPG hoặc WEBP* |
| Tải file > 1 MB | Chặn — *Logo tối đa 1 MB* |
| Bấm "Xóa logo hiện tại" | Giá trị về rỗng **và** file bị xóa khỏi đĩa |

### Nhóm 2–6 — Tham số nghiệp vụ
| Kịch bản | Kết quả |
|---|---|
| Giờ chuẩn 14:00/12:00 → 15:00/11:00 | Lưu đúng |
| Giờ `25:99` | Chặn, DB không đổi |
| VAT 10% | DB lưu `0.1`; mở lại ô nhập hiện đúng `10` |
| VAT 150% · đơn vị làm tròn 0 | Chặn |
| Phụ thu: mốc 8 giờ, 60% / 35% / 30% / 55%, mốc thêm đêm 19 giờ | DB lưu `8`, `0.6`, `0.55`, `19` |
| Mốc "thêm 1 đêm" = 14 ≤ mốc bậc 1 = 15 | Chặn — *phải muộn hơn mốc kết thúc bậc 1*, DB không đổi |
| Mốc giờ 30 | Chặn — *Mốc giờ nằm trong khoảng 0–23* |
| Cọc 2 đêm, giữ chỗ tới 20h, phí hủy 10/60/100% | Lưu đúng (`0.1`, `0.6`, `1`) |
| Hạn mức 300.000 ₫ / 15%, cho bán khi hết tồn = bật | Lưu đúng (`300000`, `0.15`, `true`) |

### Khôi phục mặc định
| Kịch bản | Kết quả |
|---|---|
| Khôi phục nhóm Thuế | VAT về `0.08`, làm tròn về `1000`; 2 dòng audit `ResetSetting` |
| Các nhóm khác | **Không bị đụng tới** |
| Nhóm không tồn tại (`KhongTonTai`) | Chặn — *Nhóm cấu hình không hợp lệ* |
| Khôi phục lần lượt cả 6 nhóm | Mọi tham số về đúng giá trị cài mới |

Script tự xóa audit log cấu hình sau khi chạy; 26 khóa ở trạng thái mặc định.

---

## 4. Cách kiểm thử lại

```bash
dotnet build HotelManagement.Web.csproj
dotnet run --project HotelManagement.Web.csproj --launch-profile http
```

Tài khoản test: `admin` / `letan`, mật khẩu `matkhau123`.

> ⚠️ **Dừng app trước khi build lại.** `dotnet build` khi app đang chạy sẽ lỗi MSB3027
> (`HotelManagement.Web.exe` bị khóa). Lỗi này trông như lỗi biên dịch nhưng không phải.

> ⚠️ **PowerShell 5.1 không có `Invoke-WebRequest -Form`**, nên muốn kiểm thử tải file phải tự
> dựng thân `multipart/form-data` bằng `MemoryStream` (hàm `PostFile` trong script).

> ⚠️ Dấu `&` trong markup Razor được xuất nguyên văn, không tự thành `&amp;`. Trình duyệt vẫn
> hiện đúng, nhưng script kiểm thử khớp chuỗi thì trượt — và HTML hợp lệ thì nên viết `&amp;`.

---

## 5. Nợ kỹ thuật

| # | Món nợ | Ghi chú |
|---|---|---|
| 1 | **Chưa ai đọc các tham số này** | Màn hình đã lưu đủ, nhưng giờ chuẩn / VAT / phụ thu / cọc / hạn mức sẽ chỉ thực sự có tác dụng khi làm nhóm C–F. Cần một lớp đọc cấu hình có cache (đọc DB mỗi lần tính tiền là phí) — nên làm ngay ở màn hình đầu tiên dùng tới |
| 2 | Lượt lưu trú chưa **chốt tham số lúc check-in** | Màn hình nhắc đúng luật "chỉ áp cho lượt mới", nhưng chỗ thực thi nằm ở `Stay` khi làm SCR-D02: phải chép giờ chuẩn và mức phụ thu vào lượt lưu trú, không đọc cấu hình hiện hành lúc tính tiền |
| 3 | Logo chưa được in ở đâu | Sẽ dùng khi làm hóa đơn (SCR-F06) |
| 4 | Không có kiểm tra ảnh thật | Chỉ tin `ContentType` do trình duyệt gửi; file `.exe` đổi tên kèm content-type `image/png` vẫn qua được. Muốn chắc thì đọc vài byte đầu (magic number) |
| 5 | Chưa có màn hình xem nhật ký thay đổi cấu hình | Audit đã ghi đủ `ChangeSetting` / `ResetSetting` kèm giá trị cũ → mới, nhưng chỉ đọc được bằng SQL — thuộc SCR-G06/G07 |
| 6 | `LoyalGuestStayThreshold` và `ChildAgeLimit` không có trong đặc tả SCR-A12 | Hai khóa này đã tồn tại từ lúc dựng DB, thuộc nhóm "Hạn mức nghiệp vụ" nên để luôn vào nhóm 6 cho sửa được, thay vì bỏ mồ côi không ai đụng tới |

---

## 6. Làm tiếp từ đâu

**Nhóm A đã xong hết** (SCR-A01…A12). Chặng tiếp theo:

1. **SCR-B01…B03 — Hồ sơ khách**: cần có trước khi làm đặt phòng.
2. **Lát cắt dọc luồng chính**: tra phòng trống (C01) → đặt phòng (C03) → check-in (D02) →
   folio (F01) → check-out (D07) → thanh toán (F05).

Đây mới là phần khó và quan trọng nhất: chống trùng phòng (BR-06), tính tiền theo BR-02/BR-03,
và transaction lúc check-out. Các màn hình danh mục vừa làm là nền — từ đây trở đi mỗi màn hình
đều phải đọc đúng tham số ở SCR-A12 thay vì viết số cứng.
