# BÁO CÁO ĐỒ ÁN CNPM - PHẦN 2.5: ĐẶC TẢ CHI TIẾT CƠ SỞ DỮ LIỆU & MÔ HÌNH UML
**Hệ thống Quản lý Khách sạn (AURA Boutique Hotel & Resort Management System)**

---

## 1. TỔNG QUAN PHÂN HỆ VÀ SƠ ĐỒ THỰC THỂ LIÊN KẾT PHÂN HỆ (SUB-ERD)

Phân hệ Cơ sở dữ liệu của Hệ thống Quản lý Khách sạn được thiết kế chuẩn hóa bậc 3 (3NF), phản ánh toàn bộ quy trình vận hành thực tế từ Đặt phòng, Quản lý Lưu trú, Dọn phòng/Bảo trì cho đến Thanh toán Hóa đơn & Công nợ.

Sơ đồ Sub-ERD trọng tâm bao gồm 9 thực thể chính kết nối chặt chẽ:
1. **`Reservations`** (Đơn đặt phòng)
2. **`Stays`** (Lượt lưu trú thực tế)
3. **`Rooms`** (Phòng vật lý)
4. **`Guests`** (Hồ sơ khách hàng)
5. **`Invoices`** (Hóa đơn thanh toán)
6. **`HousekeepingTasks`** (Nhiệm vụ dọn phòng)
7. **`RoomChangeLogs`** (Nhật ký đổi phòng)
8. **`ServiceRequests`** (Yêu cầu dịch vụ / Bảo trì)
9. **`StayGuests`** (Khách ở cùng phòng)

---

## 2. TỪ ĐIỂN DỮ LIỆU CHI TIẾT (DATA DICTIONARY)

### 2.1 Bảng `Reservations` (Đơn đặt phòng)
- **Mô tả vai trò**: Lưu trữ các đơn đặt phòng từ các nguồn (Trực tiếp, Điện thoại, Website, OTA). Quản lý vòng đời đơn từ nháp, xác nhận, check-in, hủy hoặc no-show.

| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh đơn đặt phòng (Identity Auto Increment) |
| `Code` | NVARCHAR(20) | Unique | No | Mã đơn hệ thống sinh tự động dạng `RSV-yyMMdd-####` |
| `PrimaryGuestId` | INT | FK | No | Liên kết `Guests.Id` — Khách hàng đứng tên đại diện đơn |
| `RentalType` | INT | Enum | No | Hình thức thuê: `0: Daily` (Theo ngày), `1: Hourly` (Theo giờ), `2: Overnight` (Qua đêm) (BR-13) |
| `CheckInDate` | DATETIME | - | No | Ngày & giờ dự kiến nhận phòng (chứa giờ chuẩn cấu hình hệ thống) |
| `CheckOutDate` | DATETIME | - | No | Ngày & giờ dự kiến trả phòng |
| `Nights` | INT | - | No | Số đêm lưu trú dự tính (Thuê giờ = 0) (BR-02) |
| `Hours` | INT | - | No | Số giờ thuê dự tính (Thuê theo giờ, các hình thức khác = 0) |
| `Status` | INT | Enum | No | Trạng thái đơn: `0: Draft`, `1: Confirmed`, `2: CheckedIn`, `3: Completed`, `4: Cancelled`, `5: NoShow` |
| `Source` | INT | Enum | No | Nguồn đặt: `0: Direct`, `1: Phone`, `2: Website`, `3: OTA` |
| `HoldUntil` | DATETIME | - | Yes | Hạn giữ chỗ cho đơn chưa đặt cọc (Mặc định 18:00 ngày nhận phòng) (BR-05) |
| `EstimatedTotal` | DECIMAL(18,2) | - | No | Tổng giá trị dự kiến của đơn đặt |
| `SpecialRequests` | NVARCHAR(500) | - | Yes | Yêu cầu đặc biệt của khách hàng (phòng tầng cao, hỗ trợ đón...) |
| `InternalNotes` | NVARCHAR(500) | - | Yes | Ghi chú nội bộ dành cho nhân viên lễ tân |
| `CancelledAt` | DATETIME | - | Yes | Thời điểm thực hiện hủy đơn hoặc ghi nhận No-show |
| `CancelledBy` | INT | - | Yes | ID người dùng thực hiện hủy đơn |
| `CancellationReason` | NVARCHAR(500) | - | Yes | Lý do hủy đơn hoặc quá hạn giữ chỗ |
| `CancellationFee` | DECIMAL(18,2) | - | No | Phí hủy đơn hoặc phạt No-show đã thu (BR-05) |
| `CreatedAt` | DATETIME | - | No | Thời điểm tạo bản ghi (Audit log NFR-07) |
| `CreatedBy` | INT | - | Yes | ID người tạo bản ghi |
| `UpdatedAt` | DATETIME | - | Yes | Thời điểm cập nhật cuối cùng |
| `UpdatedBy` | INT | - | Yes | ID người cập nhật cuối cùng |

---

### 2.2 Bảng `Stays` (Lượt lưu trú thực tế)
- **Mô tả vai trò**: Lưu vết lượt ở thực tế của phòng. Gắn liền với đúng 1 Hồ sơ thanh toán (`Folio`). Chốt đơn giá tại thời điểm check-in để bảo đảm tính toàn vẹn tài chính.

| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh lượt lưu trú (Identity) |
| `ReservationId` | INT | FK | Yes | Liên kết `Reservations.Id` — Đơn đặt gốc (Cho phép Null để hỗ trợ dữ liệu legacy) |
| `RoomId` | INT | FK | No | Liên kết `Rooms.Id` — Phòng thực tế đang/đã ở |
| `PrimaryGuestId` | INT | FK | No | Liên kết `Guests.Id` — Khách chính nhận phòng |
| `ActualCheckIn` | DATETIME | - | No | Thời điểm bấm nhận phòng thực tế |
| `ExpectedCheckOut` | DATETIME | - | No | Thời điểm dự kiến trả phòng |
| `ActualCheckOut` | DATETIME | - | Yes | Thời điểm thực tế trả phòng & chốt bill |
| `RentalType` | INT | Enum | No | Hình thức thuê sao chép từ đơn lúc check-in (BR-13) |
| `PricePerNight` | DECIMAL(18,2) | - | No | Đơn giá/đêm chốt cứng lúc check-in (BR-02) |
| `PriceFirstHour` | DECIMAL(18,2) | - | No | Đơn giá giờ đầu chốt cứng lúc check-in |
| `PriceExtraHour` | DECIMAL(18,2) | - | No | Đơn giá các giờ tiếp theo chốt cứng lúc check-in |
| `PriceOvernight` | DECIMAL(18,2) | - | No | Đơn giá gói qua đêm chốt cứng lúc check-in |
| `Nights` | INT | - | No | Số đêm thực tế tính tiền (Chốt lúc check-out, thuê giờ = 0) |
| `BilledHours` | INT | - | No | Số giờ tính tiền phụ trội hoặc thuê giờ (BR-13) |
| `Status` | INT | Enum | No | Trạng thái: `0: CheckedIn`, `1: CheckedOut`, `2: ChangedRoom` |
| `IsInspected` | BIT | - | No | Đã kiểm tra phòng & minibar (Bắt buộc True mới cho check-out - BR-08) |

---

### 2.3 Bảng `Invoices` (Hóa đơn thanh toán)
- **Mô tả vai trò**: Hóa đơn tài chính được sinh ra khi chốt Folio lúc Check-out (FR-F05). Hóa đơn không thể chỉnh sửa sau khi xuất, đảm bảo tính chống gian lận.

| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh hóa đơn |
| `InvoiceNo` | NVARCHAR(20) | Unique | No | Mã số hóa đơn liên tục, không trùng: `HD-yyyyMM-#####` |
| `FolioId` | INT | FK | No | Liên kết `Folios.Id` — Hồ sơ thanh toán được chốt |
| `CashierShiftId` | INT | FK | No | Liên kết `CashierShifts.Id` — Ca làm việc mở tại thời điểm xuất HD (BR-10) |
| `IssuedAt` | DATETIME | - | No | Thời điểm xuất hóa đơn |
| `IssuedBy` | INT | - | No | ID Nhân viên/Thu ngân xuất hóa đơn |
| `CompanyName` | NVARCHAR(200) | - | Yes | Tên đơn vị nhận hóa đơn đỏ (VAT) |
| `TaxCode` | NVARCHAR(20) | - | Yes | Mã số thuế doanh nghiệp |
| `CompanyAddress` | NVARCHAR(500) | - | Yes | Địa chỉ doanh nghiệp |
| `InvoiceEmail` | NVARCHAR(100) | - | Yes | Email nhận hóa đơn điện tử |
| `RoomCharge` | DECIMAL(18,2) | - | No | Tổng tiền phòng chốt cứng tại thời điểm xuất (BR-04) |
| `ServiceCharge` | DECIMAL(18,2) | - | No | Tổng tiền dịch vụ & minibar chốt cứng |
| `SurchargeAmount` | DECIMAL(18,2) | - | No | Phụ thu quá giờ, thêm người |
| `DiscountAmount` | DECIMAL(18,2) | - | No | Số tiền được giảm giá / promotion |
| `DepositAmount` | DECIMAL(18,2) | - | No | Số tiền đặt cọc đã khấu trừ |
| `SubTotal` | DECIMAL(18,2) | - | No | Tiền phòng + Dịch vụ + Phụ thu − Giảm giá − Cọc |
| `TaxRate` | DECIMAL(5,2) | - | No | Tỷ lệ thuế VAT (%) (Ví dụ: 8.00 hoặc 10.00) |
| `TaxAmount` | DECIMAL(18,2) | - | No | Tiền thuế VAT = SubTotal * TaxRate |
| `TotalAmount` | DECIMAL(18,2) | - | No | Tổng tiền thanh toán cuối cùng (làm tròn theo BR-04) |
| `AmountPaid` | DECIMAL(18,2) | - | No | Thực thu tiền mặt/chuyển khoản từ khách |
| `DebtAmount` | DECIMAL(18,2) | - | No | Tiền nợ đọng nếu khách chưa thanh toán đủ (Status = Debt) |
| `Status` | INT | Enum | No | Trạng thái: `0: Settled` (Đã thanh toán), `1: Debt` (Ghi nợ), `2: Voided` (Hủy HD) |
| `VoidedAt` | DATETIME | - | Yes | Thời điểm hủy hóa đơn (Chỉ Admin) |
| `VoidedBy` | INT | - | Yes | ID Admin thực hiện hủy hóa đơn |
| `VoidReason` | NVARCHAR(500) | - | Yes | Lý do hủy hóa đơn |
| `CreatedAt` | DATETIME | - | No | Thời điểm tạo bản ghi |
| `CreatedBy` | INT | - | Yes | ID người tạo bản ghi |
| `UpdatedAt` | DATETIME | - | Yes | Thời điểm cập nhật |
| `UpdatedBy` | INT | - | Yes | ID người cập nhật |

---

### 2.4 Bảng `Rooms` (Phòng vật lý)
- **Mô tả vai trò**: Quản lý thông tin từng phòng vật lý trong khách sạn, trạng thái phục vụ thực tế và liên kết loại phòng.

| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh phòng vật lý |
| `RoomNumber` | NVARCHAR(10) | Unique | No | Số phòng (Ví dụ: "101", "202", "VIP-01") |
| `Floor` | INT | - | No | Số tầng (1, 2, 3...) |
| `RoomTypeId` | INT | FK | No | Liên kết `RoomTypes.Id` — Loại phòng |
| `Status` | INT | Enum | No | Trạng thái phòng: `0: Available` (Trống sạch), `1: Occupied` (Có khách), `2: Dirty` (Bẩn chờ dọn), `3: Cleaning` (Đang dọn), `4: Maintenance` (Bảo trì), `5: OutOfService` (Ngừng kinh doanh) |
| `StatusNote` | NVARCHAR(255) | - | Yes | Lý do khi chuyển phòng sang Maintenance hoặc OutOfService |
| `Notes` | NVARCHAR(255) | - | Yes | Ghi chú đặc điểm phòng (gần thang máy, hướng biển...) |
| `IsActive` | BIT | - | No | Trạng thái hoạt động (mặc định True) |
| `CreatedAt` | DATETIME | - | No | Thời điểm tạo bản ghi |
| `CreatedBy` | INT | - | Yes | ID người tạo bản ghi |
| `UpdatedAt` | DATETIME | - | Yes | Thời điểm cập nhật |
| `UpdatedBy` | INT | - | Yes | ID người cập nhật |

---

### 2.5 Bảng `Guests` (Hồ sơ khách hàng)
- **Mô tả vai trò**: Quản lý hồ sơ định danh duy nhất của khách hàng, tích điểm CRM, hỗ trợ đăng nhập portal và quản lý danh sách chú ý (Blacklist).

| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh khách hàng |
| `FullName` | NVARCHAR(100) | - | No | Họ và tên đầy đủ của khách hàng |
| `IdType` | INT | Enum | No | Loại giấy tờ: `0: CitizenId` (CCCD/CMND), `1: Passport`, `2: DriverLicense` |
| `IdNumber` | NVARCHAR(20) | Unique | No | Số giấy tờ định danh (Duy nhất toàn hệ thống) |
| `DateOfBirth` | DATETIME | - | Yes | Ngày tháng năm sinh |
| `Gender` | INT | Enum | Yes | Giới tính: `0: Male`, `1: Female`, `2: Other` |
| `Nationality` | NVARCHAR(50) | - | No | Quốc tịch (Mặc định: "Việt Nam") |
| `PhoneNumber` | NVARCHAR(20) | - | No | Số điện thoại liên hệ |
| `Email` | NVARCHAR(100) | - | Yes | Địa chỉ Email |
| `PasswordHash` | NVARCHAR(200) | - | Yes | Chuỗi băm mật khẩu PBKDF2 (Dành cho tài khoản portal online) |
| `IsActive` | BIT | - | No | Trạng thái tài khoản (True: Hoạt động) |
| `LastLoginAt` | DATETIME | - | Yes | Thời điểm đăng nhập portal gần nhất |
| `Address` | NVARCHAR(255) | - | Yes | Địa chỉ thường trú (dùng cho khai báo tạm trú FR-B05) |
| `Notes` | NVARCHAR(500) | - | Yes | Ghi chú thói quen/sở thích của khách |
| `Tier` | INT | Enum | No | Hạng hội viên: `0: Standard`, `1: Silver`, `2: Gold`, `3: Diamond` |
| `RewardPoints` | INT | - | No | Điểm tích lũy loyalty |
| `IsBlacklisted` | BIT | - | No | Đánh dấu danh sách hạn chế/chú ý đặc biệt (FR-B06) |
| `BlacklistReason` | NVARCHAR(500) | - | Yes | Lý do đưa vào danh sách chú ý |
| `CreatedAt` | DATETIME | - | No | Thời điểm tạo bản ghi |
| `CreatedBy` | INT | - | Yes | ID người tạo bản ghi |
| `UpdatedAt` | DATETIME | - | Yes | Thời điểm cập nhật |
| `UpdatedBy` | INT | - | Yes | ID người cập nhật |

---

### 2.6 Các bảng hỗ trợ vận hành Sub-ERD (`HousekeepingTasks`, `RoomChangeLogs`, `ServiceRequests`, `StayGuests`)

#### 2.6.1 Bảng `HousekeepingTasks` (Nhiệm vụ dọn phòng)
| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh nhiệm vụ dọn phòng |
| `RoomId` | INT | FK | No | Liên kết `Rooms.Id` — Phòng cần dọn |
| `Status` | INT | Enum | No | `0: Pending` (Chờ dọn), `1: InProgress` (Đang dọn), `2: Completed` (Đã dọn), `3: Inspected` (Đã kiểm tra) |
| `AssignedTo` | INT | FK | Yes | Liên kết `Employees.Id` — Nhân viên/Lễ tân tiếp nhận/xác nhận nhiệm vụ |
| `StartedAt` | DATETIME | - | Yes | Thời điểm bắt đầu dọn phòng |
| `CompletedAt` | DATETIME | - | Yes | Thời điểm hoàn thành dọn phòng |
| `StayId` | INT | FK | Yes | Liên kết `Stays.Id` — Lượt lưu trú vừa trả phòng sinh ra nhiệm vụ dọn |
| `Notes` | NVARCHAR(500) | - | Yes | Ghi chú trong quá trình dọn dẹp |
| `CreatedAt` | DATETIME | - | No | Thời điểm tạo nhiệm vụ |
| `CreatedBy` | INT | - | Yes | ID người tạo |
| `UpdatedAt` | DATETIME | - | Yes | Thời điểm cập nhật cuối |
| `UpdatedBy` | INT | - | Yes | ID người cập nhật |

#### 2.6.2 Bảng `RoomChangeLogs` (Nhật ký đổi phòng)
| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh nhật ký đổi phòng |
| `StayId` | INT | FK | No | Liên kết `Stays.Id` — Lượt lưu trú được đổi phòng |
| `FromRoomId` | INT | FK | No | Liên kết `Rooms.Id` — Phòng cũ chuyển đi |
| `ToRoomId` | INT | FK | No | Liên kết `Rooms.Id` — Phòng mới chuyển đến |
| `ChangedAt` | DATETIME | - | No | Thời điểm thực hiện đổi phòng |
| `Reason` | NVARCHAR(500) | - | No | Lý do đổi phòng (bắt buộc nhập) |
| `OldPricePerNight` | DECIMAL(18,2) | - | No | Giá đêm của phòng cũ tại thời điểm đổi |
| `NewPricePerNight` | DECIMAL(18,2) | - | No | Giá đêm áp dụng cho phòng mới |
| `PriceDifferenceWaived` | BIT | - | No | True nếu Admin duyệt miễn phí chênh lệch giá |
| `CreatedAt` | DATETIME | - | No | Thời điểm tạo bản ghi nhật ký |
| `CreatedBy` | INT | - | Yes | ID người thực hiện đổi |
| `UpdatedAt` | DATETIME | - | Yes | Thời điểm cập nhật |
| `UpdatedBy` | INT | - | Yes | ID người cập nhật |

#### 2.6.3 Bảng `ServiceRequests` (Yêu cầu phục vụ & bảo trì)
| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh yêu cầu |
| `Code` | NVARCHAR(20) | Unique | No | Mã yêu cầu hệ thống sinh tự động dạng `REQ-yyMMdd-####` |
| `Type` | INT | Enum | No | Loại yêu cầu: `0: Maintenance` (Bảo trì hỏng hóc), `1: RoomService` (Phục vụ tại phòng) |
| `RoomId` | INT | FK | No | Liên kết `Rooms.Id` — Phòng gửi yêu cầu |
| `Description` | NVARCHAR(1000) | - | No | Nội dung mô tả chi tiết sự cố / yêu cầu |
| `Priority` | INT | Enum | No | Mức ưu tiên: `0: Low`, `1: Medium`, `2: High`, `3: Urgent` |
| `Status` | INT | Enum | No | Trạng thái: `0: New`, `1: Assigned`, `2: InProgress`, `3: Resolved`, `4: Cancelled` |
| `AssignedTo` | INT | FK | Yes | Liên kết `Employees.Id` — Nhân viên được phân công xử lý |
| `CompletedAt` | DATETIME | - | Yes | Thời điểm xử lý xong sự cố |
| `Resolution` | NVARCHAR(500) | - | Yes | Ghi chú nguyên nhân & biện pháp khắc phục |
| `CreatedAt` | DATETIME | - | No | Thời điểm tạo yêu cầu |
| `CreatedBy` | INT | - | Yes | ID người tạo |
| `UpdatedAt` | DATETIME | - | Yes | Thời điểm cập nhật |
| `UpdatedBy` | INT | - | Yes | ID người cập nhật |

#### 2.6.4 Bảng `StayGuests` (Khách ở cùng phòng)
| Tên trường | Kiểu dữ liệu | Loại khóa | Null | Mô tả & Ràng buộc kinh doanh |
|---|---|---|---|---|
| `Id` | INT | PK | No | Định danh bản ghi khách ở cùng |
| `StayId` | INT | FK | No | Liên kết `Stays.Id` — Lượt lưu trú tương ứng |
| `GuestId` | INT | FK | No | Liên kết `Guests.Id` — Khách hàng lưu trú |
| `IsPrimary` | BIT | - | No | True nếu là khách chính đại diện đăng ký |
| `IsChild` | BIT | - | No | True nếu là trẻ em (không tính phụ thu thêm người) |
| `CreatedAt` | DATETIME | - | No | Thời điểm ghi nhận |
| `CreatedBy` | INT | - | Yes | ID người tạo |
| `UpdatedAt` | DATETIME | - | Yes | Thời điểm cập nhật |
| `UpdatedBy` | INT | - | Yes | ID người cập nhật |

---

## 3. ĐẶC TẢ CHI TIẾT MÔ HÌNH UML (CHAPTER 2 UML SPECIFICATIONS)

### 3.1 Đặc tả Sơ đồ Use Case (Use Case Specifications)

#### 3.1.1 Danh sách Tác nhân (Actors)
1. **Lễ tân (FrontDesk)**: Thực hiện nhận/trả phòng, đổi phòng, thu cọc, lập hóa đơn, kiểm tra sơ đồ phòng.
2. **Thu ngân (Cashier)**: Quản lý ca làm việc, thu tiền hóa đơn, xử lý công nợ, nhận tiền cọc.
3. **Nhân viên Buồng phòng & Bảo trì (Housekeeping & Maintenance)**: Tiếp nhận nhiệm vụ dọn phòng, báo cáo tình trạng kiểm phòng, xử lý sự cố thiết bị.
4. **Quản lý / Quản trị viên (Manager / Admin)**: Thiết lập sơ đồ phòng, bảng giá, duyệt miễn phí chênh lệch đổi phòng, hủy hóa đơn, xem báo cáo doanh thu & công nợ.
5. **Khách hàng (Client / Guest)**: Tra cứu phòng trống online, tạo đơn đặt phòng trực tuyến, theo dõi lịch sử lưu trú và tích điểm.

#### 3.1.2 Bảng Tóm tắt Các Use Case Trọng tâm
| Mã Use Case | Tên Use Case | Tác nhân chính | Include / Extend |
|---|---|---|---|
| **UC-01** | Đặt phòng & Thu cọc | Lễ tân / Khách hàng | **Include**: Kiểm tra phòng trống, Tích hợp sinh mã `RSV-` |
| **UC-02** | Check-in Nhận phòng | Lễ tân | **Include**: Khai báo thông tin khách (`StayGuests`), Khóa đơn đặt |
| **UC-03** | Đổi phòng lưu trú | Lễ tân | **Extend**: Miễn phí chênh lệch (Admin duyệt) |
| **UC-04** | Dọn phòng & Kiểm phòng | Buồng phòng / Lễ tân | **Include**: Chuyển trạng thái phòng `Dirty` $\rightarrow$ `Available` |
| **UC-05** | Check-out & Xuất hóa đơn | Lễ tân / Thu ngân | **Include**: Kiểm tra phòng (BR-08), Chốt Folio, Kiểm tra Ca làm việc (BR-10) |

---

### 3.2 Đặc tả Luồng Công việc (Activity Diagrams)

#### 3.2.1 Quy trình Check-in & Nhận phòng (Check-in Workflow)
1. **Bước 1**: Lễ tân tra cứu Đơn đặt phòng theo Mã đơn (`RSV-`), Tên khách hoặc Số điện thoại.
2. **Bước 2**: Hệ thống kiểm tra trạng thái đơn đặt (`Status == Confirmed`).
3. **Bước 3**: Lễ tân chọn phòng cụ thể khả dụng trong danh sách loại phòng tương ứng.
4. **Bước 4**: Lễ tân nhập thông tin tờ khai lưu trú của Khách chính & Khách đi cùng (`StayGuests`).
5. **Bước 5**: Hệ thống khởi tạo đối tượng `Stay` thực tế, chốt giá phòng theo bảng giá tại thời điểm Check-in (BR-02).
6. **Bước 6**: Hệ thống tự động khởi tạo Hồ sơ thanh toán `Folio` tương ứng với `Stay`.
7. **Bước 7**: Hệ thống cập nhật trạng thái phòng `Rooms.Status = Occupied` và chuyển trạng thái đơn đặt `Reservations.Status = CheckedIn`.

#### 3.2.2 Quy trình Đổi phòng giữa kỳ lưu trú (Room Change Workflow)
1. **Bước 1**: Lễ tân tiếp nhận yêu cầu đổi phòng từ khách hàng và chọn lượt lưu trú `Stay` đang hoạt động.
2. **Bước 2**: Lễ tân chọn Phòng mới khả dụng (`Status == Available`).
3. **Bước 3**: Hệ thống tính toán chênh lệch đơn giá phòng giữa Phòng cũ và Phòng mới.
4. **Bước 4**: Trường hợp phòng mới có giá cao hơn hoặc thấp hơn:
   - Nếu đổi phòng do lỗi dịch vụ khách sạn: Admin thực hiện duyệt `PriceDifferenceWaived = True`.
   - Nếu đổi theo nhu cầu khách: Hệ thống cập nhật đơn giá lưu trú mới vào `Stay`.
5. **Bước 5**: Hệ thống ghi nhật ký vào bảng `RoomChangeLogs` (lưu `FromRoomId`, `ToRoomId`, lý do đổi).
6. **Bước 6**: Hệ thống giải phóng phòng cũ: chuyển trạng thái phòng cũ thành `Dirty` và tự động sinh bản ghi `HousekeepingTasks`.
7. **Bước 7**: Hệ thống chuyển phòng mới sang trạng thái `Occupied` và cập nhật `Stay.RoomId = ToRoomId`.

#### 3.2.3 Quy trình Check-out, Kiểm phòng & Xuất Hóa đơn (Check-out & Billing Workflow)
1. **Bước 1**: Lễ tân chọn phòng cần làm thủ tục trả phòng.
2. **Bước 2**: Hệ thống yêu cầu bước **Kiểm tra phòng & Minibar** (`IsInspected == True` - BR-08).
3. **Bước 3**: Nhân viên kiểm tra nhập số lượng dịch vụ/nước uống đã sử dụng từ Minibar. Hệ thống thêm các phát sinh vào `FolioItems`.
4. **Bước 4**: Lễ tân xác nhận hoàn tất kiểm phòng (`Stay.IsInspected = True`).
5. **Bước 5**: Hệ thống tính toán tổng chi phí trên Folio:
   - Giá phòng = Đơn giá chốt $\times$ Số đêm / Số giờ thực tế + Phụ thu quá giờ (BR-13).
   - Tổng dịch vụ phát sinh + Thuế VAT.
   - Khấu trừ các khoản tiền cọc đã thu từ trước (`Deposits`).
6. **Bước 6**: Hệ thống kiểm tra Ca làm việc của Thu ngân/Lễ tân (`CashierShifts.Status == Open` - BR-10).
7. **Bước 7**: Lễ tân nhận tiền thanh toán còn lại từ khách (Tiền mặt / Chuyển khoản).
8. **Bước 8**: Hệ thống sinh Hóa đơn tài chính `Invoices` với mã `HD-yyyyMM-#####`, đóng `Folio`, cập nhật `Stay.Status = CheckedOut`, và chuyển phòng sang `Dirty`.


---

### 3.3 Đặc tả Sơ đồ Tuần tự (Sequence Diagrams)

#### 3.3.1 Sequence Diagram: `CheckInReservation`
- **Tác nhân**: Lễ tân (FrontDesk UI)
- **Đối tượng tham gia**: `ReservationsController`, `ReservationService`, `StayService`, `HotelDbContext`, `Database Transaction`.
- **Trình tự tương tác**:
  1. `FrontDesk` $\rightarrow$ `ReservationsController.CheckIn(reservationId, roomId, guestDtos)`
  2. `ReservationsController` $\rightarrow$ `StayService.CheckInAsync(reservationId, roomId, guestDtos)`
  3. `StayService` bắt đầu `IDbContextTransaction`.
  4. `StayService` $\rightarrow$ `HotelDbContext.Reservations.GetByIdAsync(reservationId)`
  5. `StayService` kiểm tra ràng buộc phòng khả dụng: `RoomService.IsAvailable(roomId, checkIn, checkOut)`.
  6. `StayService` khởi tạo bản ghi `Stay`:
     - Gán `PricePerNight`, `PriceFirstHour`, `PriceExtraHour` lấy từ `RoomType`.
     - Gán `ActualCheckIn = DateTime.Now`.
  7. `StayService` khởi tạo `Folio` mới gắn liền với `Stay`.
  8. `StayService` thêm danh sách khách lưu trú vào `StayGuests`.
  9. `StayService` cập nhật `Room.Status = RoomStatus.Occupied`.
  10. `StayService` cập nhật `Reservation.Status = ReservationStatus.CheckedIn`.
  11. `StayService` thực hiện `HotelDbContext.SaveChangesAsync()` và `Transaction.CommitAsync()`.
  12. `StayService` trả kết quả `Success` về `ReservationsController` và hiển thị giao diện thành công.

#### 3.3.2 Sequence Diagram: `CheckoutAndGenerateInvoice`
- **Tác nhân**: Thu ngân / Lễ tân (Cashier UI)
- **Đối tượng tham gia**: `BillingController`, `InvoiceService`, `CashierShiftService`, `HotelDbContext`, `Database Transaction`.
- **Trình tự tương tác**:
  1. `Cashier` $\rightarrow$ `BillingController.Checkout(stayId, paymentModel)`
  2. `BillingController` $\rightarrow$ `CashierShiftService.GetCurrentShiftAsync(employeeId)`
     - *Kiểm tra BR-10*: Nếu không có ca làm việc mở, ném ngoại lệ `InvalidOperationException("Chưa mở ca làm việc")`.
  3. `BillingController` $\rightarrow$ `InvoiceService.GenerateInvoiceAsync(stayId, paymentModel)`
  4. `InvoiceService` bắt đầu `IDbContextTransaction`.
  5. `InvoiceService` kiểm tra điều kiện kiểm phòng: `Stay.IsInspected == true` (BR-08). Nếu `false` $\rightarrow$ Chặn trả phòng.
  6. `InvoiceService` tính toán tổng số tiền phòng & dịch vụ từ `Folio` + tính thuế VAT + trừ tiền cọc.
  7. `InvoiceService` gọi `NumberSequenceService.GetNextInvoiceNumberAsync()` để lấy mã `HD-yyyyMM-#####`.
  8. `InvoiceService` tạo đối tượng `Invoice` và lưu thông tin chi tiết các khoản chốt cứng (BR-04).
  9. `InvoiceService` tạo bản ghi `Payment` ghi nhận giao dịch thu tiền gắn với `CashierShiftId`.
  10. `InvoiceService` cập nhật `Stay.Status = StayStatus.CheckedOut` và `Room.Status = RoomStatus.Dirty`.
  11. `InvoiceService` tự động sinh bản ghi `HousekeepingTask` cho phòng vừa trả.
  12. `InvoiceService` thực hiện `HotelDbContext.SaveChangesAsync()` và `Transaction.CommitAsync()`.
  13. Trả về kết quả khởi tạo Hóa đơn thành công và xuất phiếu in Hóa đơn cho khách hàng.

---

## 4. TÓM TẮT & HƯỚNG DẪN TÍCH HỢP VÀO BÁO CÁO DOCX (`BaoCao_CNPM_UTC_QuanLyKhachSan.docx`)

1. **Từ điển Dữ liệu (Section 2)**:
   - Các bảng trong tài liệu này phản ánh chính xác cấu trúc CSDL và mã nguồn C# EF Core trong thư mục `Models/Entities/`.
   - Có thể copy trực tiếp các bảng Markdown vào MS Word (Word sẽ tự chuyển thành bảng chuẩn với định dạng hàng/cột rõ ràng).

2. **Mô hình UML (Section 3)**:
   - Cung cấp toàn bộ mô tả văn bản, kịch bản chính, luồng ngoại lệ và chuỗi gọi hàm để hoàn thiện các biểu đồ Use Case, Activity Diagram và Sequence Diagram cho Báo cáo Đồ án CNPM.


