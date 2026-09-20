# 05 — Nhân viên & Tài khoản (SCR-A10, SCR-A11)

| | |
|---|---|
| **Trạng thái** | ✅ Hoàn thành, đã kiểm thử end-to-end trên app chạy thật |
| **Cập nhật lần cuối** | 20/09/2026 |
| **Màn hình** | SCR-A10, SCR-A11 (`docs/screens/02-catalog.md`) |
| **Yêu cầu** | FR-A06, FR-A08, BR-10, BR-11 |

---

## 1. Đã làm gì

| Màn hình | URL | Nội dung |
|---|---|---|
| SCR-A10 | `GET /Employees` | Danh sách nhân viên (mã · họ tên · vai trò · SĐT · tên đăng nhập · trạng thái · đăng nhập gần nhất), lọc theo từ khóa / vai trò / trạng thái, thao tác **Sửa · Khóa/Mở khóa · Đặt lại mật khẩu** |
| SCR-A11 | `GET/POST /Employees/Create`, `/Edit/{id}` | Form thêm/sửa với đầy đủ kiểm tra, ba luật tự bảo vệ hệ thống, audit log khi đổi vai trò và đổi trạng thái |

Cả màn hình chỉ dành cho Admin — `[Authorize(Roles = Roles.Admin)]` ở cấp controller, lễ tân vào là 403.

### File thêm mới

| File | Nội dung |
|---|---|
| `Models/ViewModels/EmployeeViewModels.cs` | List item, index (bộ lọc), form |
| `Services/EmployeeService.cs` | Search, GetForEdit, Create, Update, ToggleLock, ResetPassword + bộ sinh mật khẩu tạm |
| `Security/EmployeeCookieEvents.cs` | Đối chiếu cookie đăng nhập với bản ghi nhân viên ở mỗi request (mục 2.1) |
| `Views/Employees/_Form.cshtml` | Form dùng chung Thêm/Sửa |

### File sửa

| File | Sửa gì |
|---|---|
| `Controllers/EmployeesController.cs` | Viết lại toàn bộ: bỏ `Pending()`, thêm `ToggleLock`, `ResetPassword` |
| `Views/Employees/Index.cshtml`, `Create.cshtml`, `Edit.cshtml` | Thay `_Placeholder` bằng màn hình thật |
| `Program.cs` | Đăng ký `IEmployeeService`, `EmployeeCookieEvents`; chuyển cấu hình sự kiện cookie sang `options.EventsType` |
| `Models/EnumDisplay.cs` | Nhãn + màu cho `EmployeeRole`, `EmployeeStatus` |

Không phải sửa entity hay migration: `Employee` đã có đủ trường (`Code`, `Role`, `Status`,
`FailedLoginCount`, `MustChangePassword`, `LastLoginAt`) và chỉ mục duy nhất trên `UserName`, `Code`.

---

## 2. Quyết định thiết kế

### 2.1 "Đổi vai trò thì đăng xuất khỏi mọi phiên" làm ở `CookieAuthenticationEvents.ValidatePrincipal`
Cookie đăng nhập là **bản chụp quyền tại thời điểm đăng nhập** — sửa DB không làm nó tự đổi,
nên nếu không làm gì thì người vừa bị hạ quyền vẫn dùng quyền cũ cho tới khi cookie hết hạn
(8 giờ). `ValidatePrincipal` chạy ở mỗi request đã xác thực: đọc lại `Role` và `Status` từ DB,
lệch thì `RejectPrincipal()` + `SignOutAsync()`.

Một chỗ này giải quyết luôn ba yêu cầu: đổi vai trò (SCR-A11), khóa tài khoản có hiệu lực ngay
(SCR-A10), và nhân viên nghỉ việc mất quyền ngay lập tức. Phương án khác là thêm cột
`SecurityStamp` vào `Employee` — tốn một migration mà không làm được gì thêm ở quy mô này.

Vì lớp này cần `DbContext` (tức là phải lấy theo từng request từ DI), phải khai báo bằng
`options.EventsType = typeof(EmployeeCookieEvents)` chứ không gán `options.Events = new ...`.
Lúc chuyển sang cách này thì đoạn `OnRedirectToLogin` cũ ở `Program.cs` cũng phải dọn vào trong
lớp — `EventsType` thay thế toàn bộ `Events`, để lại một nửa ở ngoài là mất đoạn kia.

### 2.2 Trạng thái "Bị khóa" chỉ có một cửa: nút Khóa/Mở khóa
Ô trạng thái trong form Sửa chỉ nhận **Đang làm / Đã nghỉ**; khi tài khoản đang bị khóa thì ô đó
hiện chữ, không cho sửa. Lý do giống hệt việc trạng thái phòng không sửa được ở form Sửa phòng
(`02-catalog-rooms.md` mục 3.2): hai cửa cùng đổi một thứ thì cửa nào không ghi nhật ký sẽ thành
đường vòng. `UpdateAsync` cũng bỏ qua `form.Status` khi bản ghi đang ở trạng thái `Locked`.

### 2.3 Ba luật tự bảo vệ nằm ở service, kiểm theo **Id người đang đăng nhập**
- Không tự đổi vai trò của chính mình
- Không tự cho chính mình nghỉ việc
- Không tự khóa tài khoản của chính mình

Giao diện có ẩn/khóa các nút tương ứng, nhưng POST tay vẫn tới được action nên server phải kiểm
lại. `EmployeeService` lấy Id người đang đăng nhập qua `IHttpContextAccessor` (giống `AuditService`)
thay vì nhận từ tham số, để controller không thể "quên" truyền.

### 2.4 Luật "luôn còn ít nhất một Admin đang hoạt động" là lớp phòng thủ dự phòng
`HasOtherActiveAdminAsync` được gọi trước khi hạ quyền, khóa hoặc cho nghỉ một Admin.
**Thành thật mà nói: hiện chưa có đường nào từ giao diện chạm tới được luật này**, vì người thao
tác luôn là một Admin đang hoạt động — nên với mọi mục tiêu khác chính mình, "Admin khác đang
hoạt động" luôn tồn tại (chính người đang thao tác). Bất biến của hệ thống thực ra được bảo đảm
bởi ba luật ở 2.3.

Vẫn giữ lại vì: nếu sau này có thao tác hàng loạt, tác vụ nền, hoặc lệnh chạy bằng tài khoản hệ
thống thì luật này là thứ duy nhất chặn được. Muốn kiểm thử nó phải gọi thẳng service ngoài ngữ
cảnh web — chưa làm, ghi ở mục nợ kỹ thuật.

### 2.5 Mã nhân viên do hệ thống sinh
SCR-A11 không liệt kê "Mã NV" trong các trường nhập, nhưng SCR-A10 lại có cột đó — nên mã được
sinh tự động dạng `NV001`, `NV002`... bằng cách lấy số lớn nhất hiện có cộng một. Người dùng không
phải nghĩ ra mã, và không có cách nào nhập trùng.

### 2.6 Mật khẩu tạm 10 ký tự, bỏ ký tự dễ đọc nhầm
Sinh bằng `RandomNumberGenerator`, luôn có cả chữ và số để qua được chính luật đổi mật khẩu ở
SCR-S02, và bỏ `0/O`, `1/l/I` vì mật khẩu này thường được đọc bằng miệng cho nhau. Mật khẩu cũ
không hiển thị được (hệ thống chỉ lưu hash PBKDF2) — đúng yêu cầu SCR-A11. Mật khẩu tạm hiện
**đúng một lần** trong thông báo sau khi đặt lại, và **không bao giờ** được ghi vào audit log.

### 2.7 Mở khóa thì xóa luôn bộ đếm đăng nhập sai
`FailedLoginCount = 0`. Không xóa thì tài khoản vừa mở khóa sẽ bị khóa lại ngay ở lần sai kế tiếp
khi luật tự khóa sau 5 lần (FR-A08) được bật.

### 2.8 Không có nút Xóa ở bất kỳ đâu
Mọi giao dịch đều ghi lại người thực hiện (`CreatedBy`), xóa nhân viên là làm hỏng vết đó.
Nghỉ việc = `Status = Resigned`; bản ghi ở lại vĩnh viễn. Danh sách mặc định ẩn người đã nghỉ,
có hộp kiểm để hiện lại.

---

## 3. Kết quả kiểm thử (20/09/2026)

Chạy thật trên `http://localhost:5265` bằng một script PowerShell duy nhất. **44/44 kiểm tra PASS.**

### SCR-A10 — Danh sách & phân quyền
| Kịch bản | Kết quả |
|---|---|
| Lễ tân mở `/Employees` | **403** |
| Admin xem danh sách | 200, `NV001`, `NV002` |
| Nút Xóa | Không tồn tại ở bất kỳ dòng nào |
| Nút khóa ở dòng của chính mình | `disabled`, tooltip *Không thể tự khóa tài khoản của chính mình* |
| Dòng của chính mình | Có nhãn "Bạn" |
| Lọc `letan` · lọc vai trò Quản lý | Đúng 1 dòng mỗi trường hợp |

### SCR-A11 — Thêm nhân viên
| Kịch bản | Kết quả |
|---|---|
| Thêm hợp lệ | Sinh mã `NV003`, `Status = Đang làm` |
| Mật khẩu | Lưu hash `100000.…` (PBKDF2), không có mật khẩu gốc trong DB |
| Cờ đổi mật khẩu | `MustChangePassword = true` |
| Audit log | `CreateEmployee`: `NV003 — Trần Văn Nam — Lễ tân`, không chứa mật khẩu |
| Trùng tên đăng nhập | Chặn — *đã tồn tại* |
| Tên đăng nhập `Nam TV` | Chặn — *không dấu, không khoảng trắng* |
| SĐT `12345` | Chặn — *không đúng định dạng Việt Nam* |
| Email sai định dạng | Chặn |
| Trùng CCCD | Chặn — *đã được dùng cho nhân viên khác* |
| Mật khẩu `abc12` | Chặn — *ít nhất 8 ký tự* |
| Mật khẩu `khongcoso` | Chặn — *phải có cả chữ và số* |
| Sau 7 lần bị chặn | DB vẫn đúng 3 nhân viên, không có bản ghi rác |

### SCR-A11 — Sửa & ba luật tự bảo vệ
| Kịch bản | Kết quả |
|---|---|
| Sửa họ tên + SĐT | Lưu đúng |
| POST tên đăng nhập khác | Bị bỏ qua, DB vẫn `namtv` |
| Admin tự hạ quyền mình | Chặn — *Không thể tự đổi vai trò của chính mình*, `Role` không đổi |
| Admin tự cho mình nghỉ | Chặn — *Không thể tự cho chính mình nghỉ việc* |
| Admin tự khóa mình | Chặn — *Không thể tự khóa tài khoản của chính mình* |

### BR-10 — Ca làm việc đang mở
| Kịch bản | Kết quả |
|---|---|
| Cho nghỉ khi còn ca mở | Chặn — *còn ca làm việc đang mở... (BR-10)*, `Status` không đổi |
| Danh sách | Hiện biểu tượng cảnh báo "Còn ca thu ngân đang mở" |
| Đóng ca rồi cho nghỉ | Thành công, audit `ChangeEmployeeStatus`: `Đang làm -> Đã nghỉ` |
| Cho đi làm lại | `Status = Đang làm` |

### FR-A08 — Đặt lại mật khẩu
| Kịch bản | Kết quả |
|---|---|
| Đặt lại | Sinh mật khẩu tạm 10 ký tự (VD `q7bh73n3h6`), hiện một lần kèm dòng nhắc |
| Cờ | `MustChangePassword = true`, `FailedLoginCount = 0` |
| Audit log | `ResetEmployeePassword`, lý do *Admin đặt lại mật khẩu*, **không** chứa mật khẩu |
| Đăng nhập bằng mật khẩu tạm | Vào được, và bị kéo thẳng sang màn hình đổi mật khẩu |
| Sau khi đổi, mở `/Employees` với vai trò Lễ tân | **403** |

### SCR-A11 — Đổi vai trò đăng xuất mọi phiên
| Kịch bản | Kết quả |
|---|---|
| Đổi `namtv` Lễ tân → Quản lý | `Role = Admin`; audit `ChangeEmployeeRole`: `Lễ tân -> Quản lý` |
| Thông báo cho Admin | *"...bị đăng xuất khỏi mọi phiên đang mở và phải đăng nhập lại..."* |
| Phiên đang mở của `namtv` | Request kế tiếp bị đá về `/Account/Login` |
| Đăng nhập lại | Vào được `/Employees` với quyền Quản lý mới |

### SCR-A10 — Khóa / mở khóa
| Kịch bản | Kết quả |
|---|---|
| Khóa `namtv` | `Status = Bị khóa`; audit `LockEmployee`: `Đang làm -> Bị khóa` |
| Phiên đang mở của `namtv` | Bị đá về `/Account/Login` ngay request kế tiếp |
| Đăng nhập lại khi bị khóa | *Tài khoản đã bị khóa. Liên hệ quản lý.* |
| Mở khóa (đang có `FailedLoginCount = 4`) | `Status = Đang làm`, `FailedLoginCount = 0` |

Script tự xóa `NV003` cùng audit log liên quan — DB quay lại đúng 2 tài khoản seed.

---

## 4. Cách kiểm thử lại

```bash
dotnet build HotelManagement.Web.csproj
dotnet run --project HotelManagement.Web.csproj --launch-profile http
```

Tài khoản test: `admin` / `letan`, mật khẩu `matkhau123` (xem ghi chú ở
[`03-catalog-services.md`](03-catalog-services.md) mục 4). Các bẫy PowerShell đã ghi ở hai hồ sơ
trước vẫn đúng.

> ⚠️ Thêm một bẫy nữa: `@(Q "...")` với hàm trả về mảng rỗng bằng `return ,@($out)` sẽ ra
> **Count = 1** (mảng lồng mảng rỗng) chứ không phải 0. Phải gán ra biến trước rồi mới
> `@($bien).Count`. Đã tốn một lượt chạy vì tưởng có bản ghi rác trong DB.

---

## 5. Nợ kỹ thuật

| # | Món nợ | Ghi chú |
|---|---|---|
| 1 | **Tự khóa sau 5 lần đăng nhập sai** (FR-A08) chưa bật | `FailedLoginCount` đã đếm đúng từ đợt đăng nhập; chỉ thiếu đoạn so ngưỡng rồi đặt `Status = Locked` trong `AccountController.Login`. Nút Mở khóa ở SCR-A10 đã sẵn sàng phục vụ nó |
| 2 | Luật "luôn còn một Admin hoạt động" chưa kiểm thử được | Xem mục 2.4 — hiện không tới được từ giao diện; cần unit test gọi thẳng `EmployeeService` |
| 3 | `ValidatePrincipal` tốn **một truy vấn DB mỗi request** đã đăng nhập | Kể cả request tài nguyên tĩnh. Chấp nhận được ở quy mô một khách sạn; muốn giảm thì cache theo `Id` trong `IMemoryCache` vài giây, hoặc chỉ kiểm lại mỗi N phút bằng cách gắn mốc thời gian vào `AuthenticationProperties` |
| 4 | Chưa có màn hình xem nhật ký thao tác | Audit log đã ghi đủ (`CreateEmployee`, `ChangeEmployeeRole`, `ChangeEmployeeStatus`, `LockEmployee`, `UnlockEmployee`, `ResetEmployeePassword`) nhưng chỉ đọc được bằng SQL — thuộc SCR-G06/G07 nhóm G |
| 5 | Chưa chặn nghỉ việc khi còn **công việc dở khác** ngoài ca thu ngân | Docs chỉ nêu ca làm việc (BR-10); nếu sau này có "yêu cầu phục vụ đang xử lý" thì thêm vào `CheckCanResignAsync` |
| 6 | Sinh mã `NV###` không an toàn khi hai Admin tạo cùng lúc | Cùng loại vấn đề với món nợ tồn kho: đọc-rồi-ghi không có khóa. Chỉ mục duy nhất trên `Code` sẽ ném lỗi chứ không tạo trùng, nhưng người dùng thấy lỗi kỹ thuật thay vì thông báo tử tế |

---

## 6. Làm tiếp từ đâu

Nhóm A chỉ còn **SCR-A12 — Cấu hình hệ thống** (giờ chuẩn BR-01, VAT và làm tròn BR-04, phụ thu
BR-03, chính sách cọc/hủy BR-05, hạn mức nghiệp vụ). Nên làm ngay vì toàn bộ luồng chính đọc các
tham số này — làm sau sẽ phải quay lại sửa chỗ đã tính tiền bằng số cứng.

Sau đó: **SCR-B01…B03 — Hồ sơ khách**, rồi lát cắt dọc luồng chính
(tra phòng trống → đặt phòng → check-in → folio → check-out → thanh toán).
