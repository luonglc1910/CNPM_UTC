# 07 — Hồ sơ khách (SCR-B01, SCR-B02, SCR-B03)

| | |
|---|---|
| **Trạng thái** | ✅ Hoàn thành, đã kiểm thử end-to-end trên app chạy thật |
| **Phạm vi** | SCR-B01, SCR-B02, SCR-B03. **Không làm** SCR-B04 (khai báo tạm trú) và SCR-B05 (danh sách hạn chế) — theo yêu cầu |
| **Cập nhật lần cuối** | 20/09/2026 |
| **Màn hình** | `docs/screens/03-guests.md` |
| **Yêu cầu** | FR-B01, FR-B02, FR-B03, BR-11 |

---

## 1. Đã làm gì

| Màn hình | URL | Nội dung |
|---|---|---|
| SCR-B01 | `GET /Guests` | Danh sách khách: tìm theo tên/SĐT/số giấy tờ, lọc quốc tịch · danh sách hạn chế · khoảng thời gian ở gần nhất; số giấy tờ **che một phần**; nhãn Khách quen / Hạn chế |
| SCR-B02 | `GET/POST /Guests/Create`, `/Edit/{id}` | Form thêm/sửa với kiểm tra theo loại giấy tờ, **tra trùng ngay khi đang nhập** (AJAX), chặn trùng số giấy tờ, cảnh báo trùng SĐT |
| SCR-B03 | `GET /Guests/Details/{id}` | Thẻ thông tin (số giấy tờ đầy đủ), 4 ô thống kê, 3 tab: lịch sử lưu trú · đơn đặt phòng · ghi chú |

Cả Admin và Lễ tân đều dùng được toàn bộ nhóm này.

### File thêm mới

| File | Nội dung |
|---|---|
| `Models/ViewModels/GuestViewModels.cs` | List/index/form/details + `IdNumberMask` + kiểu trả về cho AJAX tra trùng |
| `Services/GuestService.cs` | Search, GetForEdit, Create, Update, Details (thống kê + lịch sử), CheckDuplicate |
| `Controllers/GuestsController.cs` | Index, Create, Edit, Details, CheckDuplicate (JSON) |
| `Views/Guests/Index.cshtml`, `_Form.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml` | |

### File sửa

| File | Sửa gì |
|---|---|
| `Models/EnumDisplay.cs` | Nhãn tiếng Việt cho `GuestIdType`, `Gender` |
| `Program.cs` | Đăng ký `IGuestService` |
| `Views/Shared/_Sidebar.cshtml` | Thêm nhóm "Khách hàng" → Hồ sơ khách |

Không cần migration: `Guest` đã có đủ trường, kể cả `IsBlacklisted` / `BlacklistReason`.

---

## 2. Quyết định thiết kế

### 2.1 Che số giấy tờ ở tầng ViewModel, không ở view
`IdNumberMask.Mask` giữ 4 ký tự đầu và 4 ký tự cuối (`0123****8901`), số ngắn thì che mạnh hơn
vì giữ 8 ký tự sẽ lộ gần hết. `GuestListItemViewModel` chỉ phơi ra `MaskedIdNumber`; view danh
sách **không có cách nào** in ra số đầy đủ dù có muốn. Số đầy đủ chỉ nằm trong
`GuestDetailsViewModel` — đúng hai nơi docs cho phép (SCR-B03 và màn hình check-in sau này).

Audit log cũng chỉ ghi số dạng che: nhật ký là bảng dùng chung, nhân bản dữ liệu cá nhân sang
đó là tạo thêm một chỗ nữa phải bảo vệ.

### 2.2 Ghi nhật ký ở mức **đọc**
`Details` gọi `LogAndSaveAsync("ViewGuestProfile", ...)`. Đây là yêu cầu riêng của nhóm B mà
các nhóm trước không có, vì màn hình này hiện số giấy tờ đầy đủ — hệ thống phải trả lời được
"ai đã xem hồ sơ của khách nào, lúc nào". Danh sách thì không ghi log: dữ liệu ở đó đã bị che.

### 2.3 Chặn cứng trùng số giấy tờ, chỉ cảnh báo trùng SĐT
Đúng theo SCR-B02, và lý do nghiệp vụ rõ ràng: một người chỉ có một số giấy tờ, nhưng cả nhà
dùng chung một số điện thoại là chuyện bình thường. Thông báo chặn **chỉ luôn ra hồ sơ đang giữ
số đó** (tên + mã) để người dùng biết đường gộp thủ công, thay vì chỉ nói "đã tồn tại".

### 2.4 Tra trùng hai lớp: AJAX lúc nhập, service lúc lưu
`GET /Guests/CheckDuplicate` trả JSON cho JavaScript gọi khi rời ô Số giấy tờ / SĐT, hiện khối
gợi ý kèm link "Dùng hồ sơ có sẵn". Nhưng quyết định chặn vẫn nằm ở `GuestService.ValidateAsync`
lúc lưu — AJAX chỉ để đỡ mất công gõ hết form rồi mới bị chặn, không phải lớp phòng thủ.
Kết quả JSON cũng trả số giấy tờ dạng che.

### 2.5 Định dạng số giấy tờ phụ thuộc loại giấy tờ nên kiểm ở service
CCCD đúng 12 số · CMND 9 hoặc 12 số · hộ chiếu 6–20 ký tự chữ-số. DataAnnotations không diễn
đạt được ràng buộc "trường này phụ thuộc giá trị trường kia", giống hệt trường hợp sức chứa
loại phòng ở `02-catalog-rooms.md` mục 2.

Tương tự, "địa chỉ bắt buộc với khách Việt Nam" cũng là ràng buộc liên trường — khách nước ngoài
không có địa chỉ thường trú tại Việt Nam để khai.

### 2.6 Phân biệt rõ **chặn** và **cảnh báo**
Chặn: trùng số giấy tờ · sai định dạng · thiếu địa chỉ (khách VN) · ngày sinh tương lai.
Cảnh báo (vẫn lưu): khách dưới 18 tuổi · trùng SĐT với hồ sơ khác. Dùng lại đúng cơ chế
`ServiceResult.Warning` đã dựng từ nhóm A, hiện ở kênh `TempData["Warning"]`.

### 2.7 Thống kê: ai ở cùng, ai trả tiền
- **Lịch sử lưu trú** lấy theo `StayGuest`, tức là gồm cả những lượt khách này chỉ **ở cùng**
  chứ không đứng tên. Cột "Vai trò" phân biệt hai loại.
- **Số lần lưu trú** và **tổng số đêm** chỉ tính lượt đã trả phòng (`CheckedOut`).
- **Tổng chi tiêu** chỉ cộng hóa đơn `Settled` (bỏ `Void` — theo SCR-B03) **và** chỉ của những
  lượt khách này đứng tên. Hóa đơn thuộc về phòng và người đứng tên trả; cộng cho cả người ở
  cùng là tính trùng tiền của cùng một hóa đơn cho nhiều người.

### 2.8 Nhãn "Khách quen" đọc từ cấu hình hệ thống
Ngưỡng lấy từ `Limit.LoyalGuestStayThreshold` (SCR-A12) chứ không viết cứng số 3.
**Đây là nơi đầu tiên trong dự án thật sự đọc tham số cấu hình** — trả một phần món nợ số 1 của
[`06-system-settings.md`](06-system-settings.md). Đã kiểm thử bằng cách hạ ngưỡng xuống 2 rồi xem
nhãn xuất hiện.

Hiện mỗi lần cần ngưỡng là gọi `ISettingsService.GetAsync()` (nạp cả 26 tham số). Ở màn hình
danh sách thì một lần mỗi request, chấp nhận được; nhưng khi luồng tính tiền dùng tới thì phải
có cache — ghi ở mục nợ kỹ thuật.

### 2.9 Không có nút Xóa, và tạm thời không có nút danh sách hạn chế
Hồ sơ khách **không bao giờ xóa cứng** (SCR-B01) nên không có action `Delete` ở bất kỳ đâu.
Thao tác đưa vào / gỡ khỏi danh sách hạn chế thuộc SCR-B05 — nằm ngoài phạm vi đợt này, nên
màn hình **chỉ hiển thị** nhãn và cảnh báo dựa trên dữ liệu sẵn có (`IsBlacklisted`), kèm đúng
luật phân quyền của SCR-B03: Admin xem được lý do, lễ tân chỉ thấy nhãn.

---

## 3. Kết quả kiểm thử (20/09/2026)

Chạy thật trên `http://localhost:5265` bằng một script PowerShell duy nhất. **41/41 kiểm tra PASS.**

### SCR-B01 — Danh sách
| Kịch bản | Kết quả |
|---|---|
| Admin và Lễ tân mở `/Guests` | 200 cả hai |
| Nút Xóa | Không tồn tại |
| Nút danh sách hạn chế | Không tồn tại (SCR-B05 ngoài phạm vi) |
| Tìm theo SĐT `0912345678` | 2 hồ sơ dùng chung số |
| Tìm theo số giấy tờ `0123` | Đúng 1 hồ sơ |
| Lọc quốc tịch Hàn Quốc · lọc danh sách hạn chế · lọc lần ở gần nhất từ 01/09 | Đúng trong cả ba trường hợp |

### SCR-B02 — Thêm / sửa
| Kịch bản | Kết quả |
|---|---|
| Thêm khách hợp lệ | Lưu đúng |
| Audit log | `CreateGuest`: `Nguyễn Văn An — CCCD 0123****8901` — **không** chứa số đầy đủ |
| CCCD 9 số | Chặn — *Số CCCD phải gồm đúng 12 chữ số* |
| Hộ chiếu `ab!` | Chặn — *6–20 ký tự chữ in hoa và số* |
| Trùng số giấy tờ | Chặn — *đã thuộc hồ sơ "…"* + gợi ý gộp thủ công |
| Khách Việt Nam không có địa chỉ | Chặn |
| Khách Hàn Quốc không có địa chỉ | **Cho lưu** |
| SĐT `12345` · email sai · ngày sinh tương lai | Chặn |
| Sau 7 lần bị chặn | Không có bản ghi rác trong DB |
| Trùng SĐT | **Cảnh báo**, vẫn lưu |
| Khách 15 tuổi | **Cảnh báo** *chưa đủ 18 tuổi*, vẫn lưu |
| Sửa họ tên | Lưu đúng |
| Sửa sang số giấy tờ của hồ sơ khác | Chặn, DB không đổi |
| Đổi số giấy tờ khi khách **đang lưu trú** | Cho sửa + audit `ChangeGuestIdNumber` (`0123****8901 -> 0123****8999`) kèm lý do *Khách đang lưu trú* |

### Che số giấy tờ & nhật ký đọc
| Kịch bản | Kết quả |
|---|---|
| Danh sách | Chỉ hiện `0123****8901`, không có số đầy đủ trong HTML |
| Chi tiết | Hiện `012345678901` |
| Lễ tân mở chi tiết | Audit `ViewGuestProfile` ghi đúng `letan` |

### Tra trùng (AJAX)
| Kịch bản | Kết quả |
|---|---|
| Số giấy tờ đã tồn tại | Trả `idMatch` đúng hồ sơ, số ở dạng che |
| SĐT dùng chung | Trả 2 hồ sơ |
| Có `excludeId` (đang sửa chính hồ sơ đó) | Không tự báo trùng với chính mình |

### SCR-B03 — Thống kê (dữ liệu dựng sẵn: 2 lượt đã trả phòng + 1 lượt đang ở)
| Kịch bản | Kết quả |
|---|---|
| Số lần lưu trú | **2** — lượt đang ở không được tính |
| Tổng số đêm | **5** (2 + 3) |
| Tổng chi tiêu | **1.000.000 ₫** — hóa đơn `Void` 500.000 ₫ bị loại |
| Hóa đơn đã hủy | Vẫn hiện trong lịch sử, gạch ngang kèm nhãn "Đã hủy" |
| Khách đang là **người ở cùng** của một lượt đang mở | Hiện nhãn "Đang lưu trú", cột vai trò ghi "Ở cùng" |
| Khách đứng tên lượt đang ở, chưa có hóa đơn | Tổng chi tiêu **0 ₫** (không hưởng ké tiền của người khác) |
| Ngưỡng khách quen = 3 (mặc định) | Chưa có nhãn |
| Hạ ngưỡng xuống 2 ở SCR-A12 | Hiện **"Khách quen — 2 lần ở"** |
| Admin xem hồ sơ bị hạn chế | Thấy lý do |
| Lễ tân xem hồ sơ đó | Chỉ thấy nhãn, **không** thấy lý do |

Script tự dọn toàn bộ khách, lượt lưu trú, folio, hóa đơn, ca thu ngân và audit log đã tạo.

---

## 4. Cách kiểm thử lại

```bash
dotnet build HotelManagement.Web.csproj
dotnet run --project HotelManagement.Web.csproj --launch-profile http
```

Tài khoản test: `admin` / `letan`, mật khẩu `matkhau123`.

> ⚠️ **Chuỗi tiếng Việt trong câu lệnh SQL phải có tiền tố `N`.** `'Gây rối trật tự'` không có
> `N` sẽ bị SQL Server chuyển về codepage không Unicode, dữ liệu vào cột `nvarchar` thành dấu
> hỏi — và test thất bại trong khi ứng dụng hoàn toàn đúng. Mất một lượt chạy vì chuyện này.

> ⚠️ **Literal ngày giờ ISO phải đủ giây**: `'2026-08-01T14:00'` bị SQL Server từ chối
> (*Conversion failed when converting date and/or time*), phải viết `'2026-08-01T14:00:00'`.

> ⚠️ Dừng app trước khi build lại (lỗi MSB3027 khóa file .exe) — xem
> [`06-system-settings.md`](06-system-settings.md) mục 4.

---

## 5. Nợ kỹ thuật

| # | Món nợ | Ghi chú |
|---|---|---|
| 1 | **SCR-B04 — Khai báo tạm trú** chưa làm | Bỏ ngoài phạm vi theo yêu cầu. Dữ liệu đã đủ (`StayGuest` có mặt trong mô hình), chủ yếu là màn hình lọc theo ngày + xuất CSV |
| 2 | **SCR-B05 — Danh sách hạn chế** chưa làm | Cột `IsBlacklisted` / `BlacklistReason` đã có và màn hình **đã hiển thị đúng** theo quyền; chỉ thiếu hộp thoại thêm/gỡ kèm lý do và audit log. Cảnh báo khi đặt phòng/check-in cho khách bị hạn chế sẽ làm cùng nhóm C/D |
| 3 | Lịch sử lưu trú chưa **bấm được sang hóa đơn** | SCR-B03 nói nhấp vào dòng thì mở hóa đơn (SCR-F06) ở chế độ chỉ đọc; màn hình đó chưa tồn tại nên hiện số hóa đơn dạng chữ. Nối link khi làm nhóm F |
| 4 | Đọc cấu hình chưa có cache | `GetLoyalThresholdAsync` nạp cả 26 tham số mỗi lần. Chấp nhận được ở đây, nhưng luồng tính tiền sẽ gọi dày hơn nhiều — xem nợ số 1 của [`06-system-settings.md`](06-system-settings.md) |
| 5 | Chưa có chức năng **gộp hồ sơ trùng** | Hiện chỉ báo "hãy gộp hồ sơ thủ công" chứ chưa có công cụ gộp. Docs cũng không yêu cầu ở v1.0 |
| 6 | Tab "Đơn đặt phòng" chỉ liệt kê đơn khách **đứng tên** | Đúng với mô hình hiện tại (`Reservation.PrimaryGuestId`); khi có khái niệm đồng hành trong đơn thì xem lại |

---

## 6. Làm tiếp từ đâu

Xong phần hồ sơ khách thì hết phần "dữ liệu nền". Chặng tiếp theo là **lát cắt dọc luồng chính**,
cũng là phần khó và quan trọng nhất của đồ án:

1. **SCR-C01/C02 — Tra cứu phòng trống** (FR-C01): nền tảng cho mọi thao tác đặt phòng.
2. **SCR-C03/C04 — Tạo đơn đặt phòng** (FR-C03, BR-06 chống trùng phòng).
3. **SCR-D02 — Check-in**: tạo `Stay` + `Folio`, **chốt** giá và tham số cấu hình vào lượt lưu
   trú (BR-02, và món nợ số 2 của `06-system-settings.md`).
4. **SCR-F02 — Folio**, **SCR-D07 — Check-out**, **SCR-F05 — Thanh toán**.

Từ đây trở đi mỗi màn hình đều phải đọc tham số ở SCR-A12 thay vì viết số cứng, và phải chạy
trong transaction khi động tới tiền.
