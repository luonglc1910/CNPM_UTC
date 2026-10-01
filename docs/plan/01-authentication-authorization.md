# 01 — Xác thực & Phân quyền

| | |
|---|---|
| **Trạng thái** | ✅ Hoàn thành, đã kiểm thử end-to-end trên app chạy thật |
| **Cập nhật lần cuối** | 20/09/2026 |
| **Yêu cầu liên quan** | SCR-S01, SCR-S02, SCR-S03 (`docs/screens/01-auth-dashboard.md`), ma trận quyền (`docs/screens/README.md`), FR-A08, BR-11, NFR-02, NFR-07 |

---

## 1. Đã làm gì

1. **Đăng nhập / đăng xuất** bằng cookie authentication, chống dò tài khoản.
2. **Phân quyền theo vai trò** trên toàn bộ controller, mặc định khóa tất cả.
3. **Đổi mật khẩu**, kèm ép đổi ở lần đăng nhập đầu.
4. **Trang 403** riêng, không im lặng đá về trang chủ.
5. **Nhật ký thao tác** cho các sự kiện đăng nhập.
6. **Menu trái tự ẩn** mục không có quyền, suy ra trực tiếp từ attribute `[Authorize]`.
7. **Rút từ 3 vai trò xuống 2** (bỏ Buồng phòng) — xem mục 6.

---

## 2. Bản đồ file

### File mới

| File | Vai trò |
|---|---|
| `Security/Roles.cs` | Hằng chuỗi vai trò cho `[Authorize(Roles = ...)]` + nhãn tiếng Việt |
| `Security/AppClaimTypes.cs` | Tên claim tự định nghĩa (`must_change_password`) |
| `Security/CurrentUserFilter.cs` | Gán `HotelDbContext.CurrentUserId`; kèm các extension đọc claim |
| `Security/MustChangePasswordFilter.cs` | Ép đổi mật khẩu lần đầu |
| `Security/ScreenAccess.cs` | **Trả lời "vai trò này vào được màn hình kia không"** — menu dùng để tự ẩn |
| `Services/IAuditService.cs`, `Services/AuditService.cs` | Ghi `AuditLog` |
| `Services/PasswordHasher.cs` | PBKDF2-SHA256 (class static, không cần DI) |
| `Models/ViewModels/LoginViewModel.cs`, `ChangePasswordViewModel.cs` | |
| `Views/Account/ChangePassword.cshtml`, `Views/Home/Forbidden.cshtml` | |

### File sửa

| File | Sửa gì |
|---|---|
| `Program.cs` | Cookie options, `FallbackPolicy`, đăng ký 2 filter + `IAuditService` + `IScreenAccess`, `MapStaticAssets().AllowAnonymous()` |
| `Controllers/AccountController.cs` | Viết lại toàn bộ: Login, Logout, ChangePassword |
| `Controllers/HomeController.cs` | Thêm `Forbidden()`, `[AllowAnonymous]` cho `Error()` |
| 9 controller nghiệp vụ | Chỉ thêm `[Authorize(...)]`; thân action vẫn `Pending()` |
| `Views/Shared/_Sidebar.cshtml` | Menu khai báo dạng danh sách, tự lọc theo quyền |
| `Views/Shared/_Layout.cshtml` | Tên + vai trò trên thanh trên, link đổi mật khẩu, 4 kênh thông báo |
| `Views/Account/Login.cshtml` | Dùng ViewModel, validation, ghi nhớ đăng nhập |
| `Views/_ViewImports.cshtml` | Thêm `@using HotelManagement.Web.Security` |

---

## 3. Bảy quyết định thiết kế (và lý do)

### 3.1 Dùng `FallbackPolicy`, không dùng `[Authorize]` trên `AdminControllerBase`
```csharp
options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
```
`HomeController` và `AccountController` **không** kế thừa `AdminControllerBase`. Đặt `[Authorize]`
ở lớp cơ sở sẽ bỏ sót đúng hai chỗ đó, và bất kỳ controller mới nào quên kế thừa cũng thành công
khai âm thầm. `FallbackPolicy` mặc định khóa mọi endpoint, muốn mở phải khai `[AllowAnonymous]`.

### 3.2 `MapStaticAssets().AllowAnonymous()` là bắt buộc
`MapStaticAssets()` đăng ký **endpoint thật**, nên `FallbackPolicy` chặn luôn cả CSS/JS.
Bỏ dòng này thì trang đăng nhập hiện ra không có định dạng. Đây là cái bẫy dễ mất thời gian nhất.

### 3.3 Kiểm mật khẩu **trước**, kiểm trạng thái tài khoản **sau**
SCR-S01 vừa đòi "thông báo lỗi giống hệt nhau", vừa đòi "báo riêng khi tài khoản bị khóa".
Hai điều đó chỉ dung hòa được khi thông báo riêng chỉ hiện cho người **đã nhập đúng mật khẩu**.
Kiểm trạng thái trước sẽ để lộ tài khoản nào tồn tại.

### 3.4 Chạy băm giả khi không tìm thấy tài khoản
```csharp
var passwordOk = employee is null
    ? PasswordHasher.Verify(model.Password, DummyHash) && false
    : PasswordHasher.Verify(model.Password, employee.PasswordHash);
```
Bỏ bước này thì phản hồi nhanh hơn hẳn (~100k vòng PBKDF2) và vẫn lộ tài khoản nào có thật
qua thời gian đáp ứng. Đã đo: 28–38ms cho cả hai trường hợp.

### 3.5 Cờ `MustChangePassword` mang trong claim, không truy vấn DB mỗi request
Sau khi đổi mật khẩu thành công phải **phát hành lại cookie** (`SignInAsync` lần nữa với bộ claim
mới, đã bỏ claim này) — nếu không, filter sẽ tiếp tục chuyển hướng dù mật khẩu đã đổi.

### 3.6 `CurrentUserFilter` là action filter, không phải middleware
Filter chạy trong đúng DI scope của request và **không đụng tới đường chạy seed lúc khởi động**
(lúc đó không có HttpContext). Đặt `Order = int.MinValue` để chạy trước mọi filter khác.

### 3.7 Menu trái suy ra quyền từ `[Authorize]`, không chép tay
`Security/ScreenAccess.cs` hỏi thẳng bộ máy phân quyền của ASP.NET Core cho từng cặp
controller/action. Trước đây `_Sidebar.cshtml` chép tay các cờ `isAdmin || isReceptionist` —
chỉ cần sửa `[Authorize]` trên controller mà quên sửa sidebar là menu lệch với thực tế.
Giờ **không thể lệch**: đã kiểm chứng bằng cách đổi `BillingController` sang Admin-only,
menu của lễ tân tự mất mục "Hóa đơn" và tiêu đề nhóm rỗng cũng tự biến mất.

---

## 4. Cách hoạt động (tóm tắt để đọc nhanh)

### Claim phát hành khi đăng nhập
| Claim | Giá trị |
|---|---|
| `NameIdentifier` | `Employee.Id` — dùng cho `CreatedBy`/`UpdatedBy` và audit |
| `Name` | `UserName` |
| `GivenName` | `FullName` — hiển thị trên thanh trên |
| `Role` | **Tên enum** (`"Admin"` / `"Receptionist"`), vì `[Authorize(Roles=...)]` so chuỗi |
| `must_change_password` | `"true"`, chỉ khi cần |

### Ma trận phân quyền hiện tại
| Controller | Attribute |
|---|---|
| RoomTypes, Rooms, HotelServices | class `Roles.All` + action `Roles.Admin` trên Create/Edit |
| Housekeeping, Reservations, FrontDesk, Billing | `Roles.All` |
| Employees, Reports | `Roles.Admin` |
| Home | không có → rơi vào FallbackPolicy; `Error()` là `[AllowAnonymous]` |
| Account | `Login` GET/POST và `Logout` là `[AllowAnonymous]` |

`Roles.All` = `"Admin,Receptionist"`. Nhiều `[Authorize]` chồng nhau được **AND** lại, nên
attribute cấp action chỉ có thể **thu hẹp**, không mở rộng được quyền của cấp class.

### Cookie
8 giờ, trượt hạn; tích *Ghi nhớ* → 7 ngày. `HttpOnly`, `SameSite=Lax`.
`AccessDeniedPath = /Home/Forbidden`.

---

## 5. Kiểm thử lại

```bash
dotnet build HotelManagement.Web.csproj
dotnet run --project HotelManagement.Web.csproj --launch-profile http
# http://localhost:5265
```

**Tài khoản sau khi seed:** `admin` / `letan`, mật khẩu đều `123456`, đều bị **ép đổi ở lần
đăng nhập đầu** (mật khẩu mới ≥ 8 ký tự, phải có cả chữ và số).

> ⚠️ Khi viết script kiểm thử bằng PowerShell, **đừng dùng ký tự `@` trong mật khẩu**.
> `Invoke-WebRequest -Body` mã hóa URL khiến chuỗi gửi đi lệch giữa lúc đổi và lúc đăng nhập —
> đã mất thời gian vì lỗi này một lần. Dùng dạng `matkhau2026` là an toàn.

Làm lại từ đầu: xóa DB rồi chạy app (tự migrate + seed):
```sql
ALTER DATABASE HotelManagementDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE HotelManagementDb;
```

### Danh sách kiểm tra

| # | Việc | Kỳ vọng |
|---|---|---|
| 1 | Chưa đăng nhập, mở `/Rooms` | 302 về `/Account/Login?returnUrl=%2FRooms` |
| 2 | `GET /css/site.css` khi chưa đăng nhập | **200** (nếu 302 thì thiếu `MapStaticAssets().AllowAnonymous()`) |
| 3 | Sai mật khẩu vs tài khoản không tồn tại | Thông báo **giống hệt**, thời gian đáp ứng tương đương |
| 4 | Đăng nhập lần đầu | Bị ép sang `/Account/ChangePassword`, mọi URL khác đều bị kéo về đây, không lặp vô hạn |
| 5 | Sau khi đổi mật khẩu | Admin → `/`, Lễ tân → `/FrontDesk`, không phải đăng nhập lại |
| 6 | Lễ tân mở `/Employees`, `/Reports/Revenue`, `/Rooms/Create` | **403**, trả đúng mã HTTP 403 |
| 7 | Lễ tân `POST /Rooms/Create` bằng tay | 403 — ẩn nút không phải lớp phòng thủ duy nhất |
| 8 | Sidebar: bấm thử **mọi** link | Không link nào trả 403 (xem script bên dưới) |
| 9 | Tài khoản `Status = Locked` + mật khẩu đúng | "Tài khoản đã bị khóa"; mật khẩu **sai** → thông báo chung |
| 10 | Bảng `AuditLogs` | Có `LoginSucceeded`, `LoginFailed`, `Logout`, `PasswordChanged`, `AccessDenied` |

### Script kiểm tra menu khớp quyền (quan trọng nhất)

Lấy mọi link trong sidebar rồi bấm thử từng cái — không link nào được trả 403:

```powershell
$base='http://localhost:5265'
# ... đăng nhập lấy $s (WebSession) ...
$html=(Invoke-WebRequest -Uri "$base/Home/Index" -WebSession $s -UseBasicParsing).Content
$nav=[regex]::Match($html,'<nav class="sidebar-nav">(.*?)</nav>',
     [Text.RegularExpressions.RegexOptions]::Singleline).Groups[1].Value
[regex]::Matches($nav,'<a[^>]*href="([^"]+)"[^>]*>\s*<i[^>]*></i>\s*([^<]+?)\s*</a>',
     [Text.RegularExpressions.RegexOptions]::Singleline)
```
Lưu ý: thẻ `<a>` trong sidebar trải trên nhiều dòng nên **bắt buộc** dùng `Singleline`.

### Kết quả lần kiểm thử gần nhất (20/09/2026)
- Admin: 15 mục menu, tất cả 200.
- Lễ tân: 12 mục menu, tất cả 200 (thiếu đúng *Nhân viên*, *Doanh thu*, *Công suất phòng*).
- Thử đổi `BillingController` sang Admin-only → menu lễ tân còn 11 mục, mục *Hóa đơn* và tiêu đề
  nhóm *Thu ngân & Báo cáo* tự biến mất. Đã hoàn tác.

---

## 6. Lịch sử thay đổi lớn

### Bỏ vai trò Buồng phòng (còn 2 vai trò)
Ban đầu có 3 vai trò. Đã rút còn **Admin + Lễ tân**. Nguyên tắc: **bỏ vai trò không phải bỏ
nghiệp vụ** — nhân viên dọn phòng vẫn làm việc nhưng không có tài khoản, báo về quầy và lễ tân
bấm cập nhật hộ. 4 màn hình nhóm E giữ nguyên.

Kèm theo:
- `BR-08` (điều kiện check-out) giữ **bắt buộc**, đổi chủ thể sang lễ tân.
- `HousekeepingTask.AssignedTo` giờ là **người bấm xác nhận**, không phải người dọn thật →
  đã bỏ tab "Năng suất nhân viên buồng phòng" trong SCR-G04 vì đo ra sẽ sai lệch.
- SCR-A05: ma trận trạng thái phòng theo vai trò biến mất, cả hai vai trò làm được mọi chuyển
  đổi; luật duy nhất còn lại là `Occupied` không ai được sửa tay.
- Gộp `Roles.FrontOffice` vào `Roles.All` vì sau khi bỏ vai trò thứ 3 hai hằng này trùng giá trị —
  để cả hai là cái bẫy sửa một quên một.
- **Không cần migration**: cột `Employees.Role` và `AuditLogs.UserRole` là `int` trơn, không có
  CHECK constraint. Chỉ cần xóa DB seed lại.

### Menu tự ẩn theo quyền
Thay `_Sidebar.cshtml` từ chép tay điều kiện sang khai báo danh sách + lọc qua `IScreenAccess`.

---

## 7. Nợ kỹ thuật — việc cần làm khi quay lại

| # | Món nợ | Vì sao chưa làm | Gợi ý làm |
|---|---|---|---|
| 1 | **Tự khóa tài khoản sau 5 lần sai (FR-A08)** | Chủ động loại khỏi phạm vi đợt này | `FailedLoginCount` đã đếm và reset đúng, tài khoản `Locked` đã bị từ chối → chỉ cần thêm một câu `if` trong `AccountController.Login` |
| 2 | **Đăng xuất mọi phiên khác khi đổi mật khẩu (SCR-S02)** | Cookie auth không giữ sổ phiên | Cần `SecurityStamp` + `CookieAuthenticationEvents.OnValidatePrincipal`, hoặc `ITicketStore`. Đánh đổi: thêm một truy vấn DB mỗi request |
| 3 | **Thông báo "Phiên làm việc đã hết hạn"** | Chỉ làm được một nửa | Đã có `OnRedirectToLogin` gắn `?expired=1` khi còn cookie cũ; chưa xử lý cookie ghi nhớ hết hạn (trình duyệt đã xóa nên không phân biệt được) |
| 4 | **Ẩn nút Thêm/Sửa trên màn hình danh mục** | View còn là placeholder, chưa có nút nào để ẩn | Khi dựng bảng thật: bọc nút trong `@if (User.IsInRole(Roles.Admin))`. Server đã chặn rồi, đây chỉ là phần trải nghiệm |
| 5 | **Dashboard: lễ tân chỉ thấy doanh thu ca mình (SCR-S04)** | Dashboard chưa đọc số liệu thật | Phải lọc ở **tầng truy vấn** theo `CashierShiftId`, không chỉ ẩn trên view |
| 6 | **Luật chuyển trạng thái phòng `Occupied` (SCR-A05)** | `RoomsController.UpdateStatus` còn là `Pending()` | Có `// TODO SCR-A05` ngay tại chỗ |
| 7 | **Nghiệp vụ thật cho 9 controller** | Ngoài phạm vi đợt xác thực | Thay `Pending()` bằng logic thật, theo `docs/screens/` |

---

## 8. Bẫy đã gặp — đọc trước khi sửa

1. **`MapStaticAssets()` phải `AllowAnonymous()`** — nếu không, trang đăng nhập mất hết CSS/JS.
2. **Ký tự `@` trong mật khẩu khi test bằng PowerShell** — xem cảnh báo ở mục 5.
3. **Tiến trình `HotelManagement.Web.exe` còn chạy sẽ khóa file build.** Lỗi `MSB3027`.
   Dừng bằng `Get-Process HotelManagement.Web | Stop-Process -Force` rồi build lại.
   `timeout 45 dotnet run` **không** giết được tiến trình con.
4. **404 khi chưa đăng nhập sẽ chuyển về trang đăng nhập**, không trả 404 — đó là hành vi đúng
   của `FallbackPolicy` (URL không khớp endpoint nào thì không có metadata authorization),
   đừng tưởng là lỗi.
5. **Regex đọc sidebar phải bật `Singleline`** vì thẻ `<a>` trải nhiều dòng.
6. **Cookie cũ mang vai trò đã bị xóa** (ví dụ `Housekeeping`) sẽ bị 403 ở mọi trang và thanh trên
   hiện "Không xác định". Không phải lỗi — đăng xuất hoặc xóa cookie. App không crash vì
   `AuditService` dùng `Enum.TryParse`.
