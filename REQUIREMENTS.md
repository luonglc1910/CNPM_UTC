# Đặc tả yêu cầu phần mềm — Hệ thống Quản lý Khách sạn

| | |
|---|---|
| **Dự án** | Hotel Management System (CNPM_UTC) |
| **Công nghệ** | ASP.NET Core MVC (.NET 10), EF Core, SQL Server |
| **Loại hệ thống** | Ứng dụng web nội bộ cho nhân viên khách sạn (không có cổng đặt phòng cho khách) |
| **Phiên bản tài liệu** | 1.0 — 20/09/2026 |

---

## 1. Tổng quan

### 1.1 Mục tiêu
Số hóa toàn bộ chu trình vận hành của một khách sạn quy mô vừa và nhỏ, từ lúc khách đặt phòng
đến khi thanh toán rời đi, nhằm:

- Loại bỏ ghi chép sổ sách thủ công, tránh trùng phòng (double-booking).
- Kiểm soát chặt doanh thu: mọi khoản thu đều gắn với một hóa đơn và một ca làm việc.
- Cung cấp số liệu công suất phòng và doanh thu để ban quản lý ra quyết định giá.

### 1.2 Phạm vi (Scope)

**Nằm trong phạm vi:**
- Quản lý danh mục: phòng, loại phòng, dịch vụ, kho hàng, nhân viên.
- Quản lý hồ sơ khách và lịch sử lưu trú.
- Đặt phòng, check-in, check-out, đổi phòng, gia hạn, hủy, no-show.
- Ghi nhận dịch vụ, minibar và trừ tồn kho.
- Quản lý buồng phòng (dọn phòng, báo hỏng).
- Thu ngân: đặt cọc, hoàn cọc, hóa đơn, thanh toán, ca làm việc.
- Báo cáo doanh thu và công suất phòng.
- Nhật ký thao tác (audit log).

**Ngoài phạm vi (phiên bản 1.0):**
- Cổng đặt phòng trực tuyến cho khách / kết nối OTA (Booking, Agoda).
- Giá theo giờ, giá qua đêm, bảng giá theo mùa — v1.0 **chỉ tính tiền theo đêm**.
- Tách / gộp hóa đơn giữa nhiều phòng, công nợ khách hàng công ty.
- Tích điểm khách hàng thân thiết, hợp đồng doanh nghiệp.
- Kế toán tổng hợp, hóa đơn điện tử cơ quan thuế, POS nhà hàng độc lập.
- Ứng dụng di động, thông báo đẩy / SMS / email tự động.

### 1.3 Thuật ngữ

| Thuật ngữ | Ý nghĩa |
|---|---|
| **Reservation** | Đơn đặt phòng — khách đặt trước, chưa đến. |
| **Stay** | Lượt lưu trú — phát sinh khi khách check-in vào một phòng cụ thể. |
| **Folio** | Sổ chi phí của một lượt lưu trú: tiền phòng + dịch vụ + phụ thu. |
| **Invoice** | Hóa đơn chốt từ Folio khi check-out. |
| **Walk-in** | Khách vãng lai, đến trực tiếp không đặt trước. |
| **No-show** | Khách đặt phòng nhưng không đến trong thời hạn giữ chỗ. |
| **Đêm (night)** | Đơn vị tính tiền phòng, từ giờ nhận phòng chuẩn đến giờ trả phòng chuẩn hôm sau. |
| **Công suất phòng** | Tỷ lệ số phòng có khách / tổng số phòng khai thác được. |

---

## 2. Tác nhân và phân quyền

| Tác nhân | Mô tả | Quyền chính |
|---|---|---|
| **Quản lý (Admin)** | Chủ / quản lý khách sạn | Toàn quyền: danh mục, giá, nhân viên, báo cáo, audit log, duyệt giảm giá và hủy hóa đơn |
| **Lễ tân (Receptionist)** | Trực quầy | Đặt phòng, check-in/out, đổi phòng, ghi dịch vụ, thu tiền, mở/đóng ca, cập nhật buồng phòng |

> **Nhân viên dọn phòng không phải là một vai trò của hệ thống.** Họ vẫn làm việc ngoài thực tế
> nhưng không có tài khoản đăng nhập: dọn xong thì báo cho quầy (miệng hoặc bộ đàm), lễ tân bấm
> cập nhật hộ. Vì vậy toàn bộ nghiệp vụ buồng phòng ở nhóm E vẫn được giữ, chỉ đổi người thao tác.

### 2.1 Ma trận phân quyền

| Chức năng | Admin | Lễ tân |
|---|:---:|:---:|
| Danh mục phòng / loại phòng / giá | CRUD | Xem |
| Danh mục dịch vụ & kho | CRUD | Xem |
| Quản lý nhân viên, phân quyền | CRUD | — |
| Đặt phòng (tạo / sửa / hủy) | ✔ | ✔ |
| Check-in / Check-out / Đổi phòng | ✔ | ✔ |
| Ghi nhận dịch vụ vào folio | ✔ | ✔ |
| Giảm giá trên hóa đơn | ✔ | Trong hạn mức |
| Hủy / điều chỉnh hóa đơn đã chốt | ✔ | — |
| Cập nhật trạng thái dọn phòng, kiểm minibar, báo hỏng | ✔ | ✔ |
| Mở / đóng ca thu ngân | ✔ | ✔ (ca của mình) |
| Báo cáo doanh thu, công suất | ✔ | — |
| Nhật ký thao tác (audit log) | ✔ | — |

**Quy tắc chung:** mọi trang đều bắt buộc đăng nhập; truy cập sai quyền trả về trang 403.

---

## 3. Quy tắc nghiệp vụ (Business Rules)

Phần này **bổ sung so với requirement ban đầu** — nếu không chốt thì không thể lập trình được phần tính tiền.

### BR-01 — Giờ chuẩn
- Giờ nhận phòng chuẩn: **14:00**. Giờ trả phòng chuẩn: **12:00** hôm sau.
- Hai mốc này là tham số cấu hình hệ thống, Admin sửa được.

### BR-02 — Cách tính số đêm
- Số đêm = số ngày lịch giữa `ngày đi` và `ngày đến`, tối thiểu **1 đêm**.
- Tiền phòng = giá đêm của loại phòng × số đêm; giá được **chốt tại thời điểm check-in** và
  không đổi nếu sau đó Admin sửa bảng giá.

### BR-03 — Phụ thu

| Loại phụ thu | Điều kiện | Cách tính (mặc định, cấu hình được) |
|---|---|---|
| Nhận phòng sớm | Check-in trước 09:00 | 50% giá đêm |
| Nhận phòng sớm | Check-in 09:00–14:00 | 30% giá đêm |
| Trả phòng trễ | Trả 12:00–15:00 | 30% giá đêm |
| Trả phòng trễ | Trả 15:00–18:00 | 50% giá đêm |
| Trả phòng trễ | Trả sau 18:00 | Tính thêm **1 đêm** |
| Thêm người | Vượt sức chứa chuẩn của loại phòng | Phí / người / đêm theo loại phòng |
| Thêm giường phụ | Theo yêu cầu | Phí cố định / đêm |

### BR-04 — Thuế và làm tròn
- VAT mặc định **8%**, áp trên (tiền phòng + dịch vụ + phụ thu − giảm giá).
- Mọi số tiền làm tròn đến **1.000 VNĐ** ở dòng tổng cuối cùng.

### BR-05 — Chính sách đặt cọc, hủy, no-show
- Đơn đặt phòng có thể yêu cầu cọc (mặc định: tiền 1 đêm). Đơn chưa cọc chỉ được giữ chỗ
  đến **18:00 ngày đến**; quá hạn cho phép đánh dấu **No-show**.
- Phí hủy theo thời điểm hủy so với ngày đến:
  - Hủy trước ≥ 48 giờ: hoàn 100% tiền cọc.
  - Hủy trong 24–48 giờ: thu 50% tiền cọc.
  - Hủy dưới 24 giờ hoặc No-show: thu 100% tiền cọc.
- Phí hủy và no-show vẫn phải sinh chứng từ thu và được tính vào doanh thu.

### BR-06 — Chống trùng phòng
- Một phòng không được có hai Reservation hoặc Stay chồng lấn khoảng thời gian.
- Hệ thống kiểm tra xung đột ngay khi lưu; nếu trùng thì từ chối và báo lỗi rõ ràng.
- Không hỗ trợ overbooking ở v1.0.

### BR-07 — Điều kiện check-in
Chỉ check-in được khi: đơn ở trạng thái `Confirmed`, phòng ở trạng thái `Available`
(đã dọn sạch), và đã ghi nhận đủ giấy tờ tùy thân của khách đứng tên.

### BR-08 — Điều kiện check-out
Chỉ check-out được khi: folio đã khóa thêm chi phí, **lễ tân đã xác nhận kiểm phòng**
(minibar + tình trạng phòng), và hóa đơn đã thanh toán đủ (số dư = 0).

Bước kiểm phòng vẫn **bắt buộc** dù không còn tài khoản buồng phòng riêng: nhân viên dọn phòng
báo kết quả về quầy, lễ tân nhập vào SCR-E02 rồi mới check-out được. Đây là chốt chặn duy nhất
ngăn việc quên thu tiền minibar.

### BR-09 — Đổi phòng
Khi đổi phòng giữa kỳ lưu trú: chi phí phòng cũ tính đến thời điểm đổi được chuyển nguyên
sang folio hiện hành; phòng cũ chuyển `Dirty`; các đêm còn lại tính theo giá phòng mới;
**lý do đổi phòng là bắt buộc**.

### BR-10 — Ca làm việc thu ngân
- Mọi giao dịch thu/chi tiền phải thuộc một ca **đang mở** của người thực hiện.
- Đóng ca yêu cầu nhập số tiền mặt đếm thực tế; hệ thống tính chênh lệch thừa/thiếu so với
  sổ sách và lưu lại. **Không được xóa ca đã đóng.**

### BR-11 — Nhật ký thao tác
Bắt buộc ghi log cho: sửa bảng giá, giảm giá, hủy đơn, hủy/điều chỉnh hóa đơn, hoàn tiền,
đổi phòng, điều chỉnh tồn kho, thay đổi quyền người dùng. Log chỉ đọc, không sửa/xóa.

### BR-12 — Tồn kho
Bán dịch vụ có quản lý kho (minibar, đồ uống) sẽ trừ tồn kho tương ứng; không cho bán khi
tồn = 0 (trừ khi Admin cho phép bán âm). Hủy dòng dịch vụ thì hoàn lại tồn.

---

## 4. Vòng đời trạng thái

### 4.1 Trạng thái phòng (RoomStatus)

```
Available (Trống - sạch)
   │ check-in
   ▼
Occupied (Đang ở) ──── check-out ───► Dirty (Chờ dọn)
   │                                     │ dọn xong
   │                                     ▼
   │                                 Available
   │ báo hỏng                            │ báo hỏng
   ▼                                     ▼
Maintenance (Bảo trì) ── sửa xong ──► Dirty
```

Ngoài ra: `Reserved` (đã có đơn đặt cho hôm nay), `OutOfService` (ngừng khai thác dài hạn,
không tính vào mẫu số công suất phòng).

### 4.2 Trạng thái đơn đặt phòng (ReservationStatus)

```
Draft ──► Confirmed ──► CheckedIn ──► CheckedOut
            │   │
            │   └──► NoShow     (quá hạn giữ chỗ)
            └──► Cancelled      (thu phí hủy theo BR-05)
```

### 4.3 Trạng thái hóa đơn (InvoiceStatus)

```
Open (đang mở, còn thêm được chi phí)
  ├──► Settled (đã thanh toán đủ)
  └──► Void    (bị hủy — chỉ Admin, bắt buộc lý do, có log)
```

---

## 5. Yêu cầu chức năng

### 5.1 Nhóm A — Danh mục & Cấu hình hệ thống

| Mã | Yêu cầu | Ưu tiên |
|---|---|:---:|
| FR-A01 | Quản lý **loại phòng**: mã, tên, sức chứa chuẩn, sức chứa tối đa, giá/đêm, phí thêm người, mô tả, tiện nghi. | Bắt buộc |
| FR-A02 | Quản lý **phòng**: số phòng, tầng, loại phòng, trạng thái, ghi chú. Không xóa phòng đã phát sinh lưu trú — chỉ ngừng khai thác. | Bắt buộc |
| FR-A03 | Cập nhật trạng thái phòng thủ công (đưa vào bảo trì / ngừng khai thác) kèm lý do. | Bắt buộc |
| FR-A04 | Quản lý **dịch vụ**: mã, tên, nhóm (ăn uống, minibar, giặt ủi, thuê xe...), đơn giá, đơn vị tính, cờ *có quản lý kho*. | Bắt buộc |
| FR-A05 | Quản lý **tồn kho** cho dịch vụ có quản lý kho: nhập hàng, xem tồn, điều chỉnh kho kèm lý do, cảnh báo dưới định mức. | Bắt buộc |
| FR-A06 | Quản lý **nhân viên & tài khoản**: thông tin cá nhân, vai trò, trạng thái làm việc; khóa/mở tài khoản; đặt lại mật khẩu. | Bắt buộc |
| FR-A07 | **Cấu hình hệ thống**: tên khách sạn, địa chỉ, MST, giờ nhận/trả phòng chuẩn, VAT, các mức phụ thu, chính sách hủy, hạn mức giảm giá của lễ tân. | Bắt buộc |
| FR-A08 | Đăng nhập / đăng xuất / đổi mật khẩu. Khóa tài khoản sau 5 lần sai liên tiếp. | Bắt buộc |

### 5.2 Nhóm B — Khách hàng

| Mã | Yêu cầu | Ưu tiên |
|---|---|:---:|
| FR-B01 | Quản lý **hồ sơ khách**: họ tên, ngày sinh, giới tính, số CCCD/Passport, quốc tịch, SĐT, email, địa chỉ, ghi chú. | Bắt buộc |
| FR-B02 | Tìm khách nhanh theo CCCD / SĐT / họ tên; gợi ý hồ sơ đã có khi tạo đơn để tránh trùng. | Bắt buộc |
| FR-B03 | Xem **lịch sử lưu trú** của một khách: các lần ở, phòng, số tiền đã chi tiêu. | Bắt buộc |
| FR-B04 | Ghi nhận **danh sách khách ở cùng phòng** (không chỉ người đứng tên), phục vụ kiểm soát sức chứa và khai báo tạm trú. | Bắt buộc |
| FR-B05 | Xuất **danh sách khai báo tạm trú** theo ngày (họ tên, giấy tờ, quốc tịch, thời gian lưu trú) ra file để nộp công an khu vực. | Nên có |
| FR-B06 | Đánh dấu khách vào **danh sách hạn chế** (blacklist) kèm lý do; cảnh báo khi đặt phòng cho khách này. | Nên có |

### 5.3 Nhóm C — Đặt phòng (Reservation)

| Mã | Yêu cầu | Ưu tiên |
|---|---|:---:|
| FR-C01 | **Tra cứu phòng trống** theo khoảng ngày đến/đi, số khách, loại phòng; hiển thị số phòng còn lại và giá dự kiến. | Bắt buộc |
| FR-C02 | **Sơ đồ phòng theo ngày** (ma trận phòng × ngày) để nhìn tổng thể tình trạng phòng. | Nên có |
| FR-C03 | **Tạo đơn đặt phòng**: chọn khách (có sẵn hoặc tạo mới), khoảng ngày, một hoặc **nhiều phòng** trong cùng một đơn, số khách, nguồn đặt (điện thoại / trực tiếp / giới thiệu), yêu cầu đặc biệt. | Bắt buộc |
| FR-C04 | Hệ thống tự sinh **mã đặt phòng** duy nhất và tính tổng tiền dự kiến. | Bắt buộc |
| FR-C05 | **Ghi nhận tiền cọc** (tiền mặt / chuyển khoản / thẻ), in phiếu thu cọc. | Bắt buộc |
| FR-C06 | **Sửa đơn**: đổi phòng đã đặt, đổi ngày, gia hạn ngày ở — có kiểm tra xung đột theo BR-06. | Bắt buộc |
| FR-C07 | **Hủy đơn**: bắt buộc nhập lý do, tự tính phí hủy theo BR-05, xử lý hoàn cọc. | Bắt buộc |
| FR-C08 | **Đánh dấu No-show** khi quá hạn giữ chỗ; hệ thống gợi ý danh sách đơn quá hạn hằng ngày. | Bắt buộc |
| FR-C09 | Xem danh sách đơn theo bộ lọc: ngày đến, ngày đi, trạng thái, tên khách, mã đơn. | Bắt buộc |
| FR-C10 | In / xuất **phiếu xác nhận đặt phòng** để gửi cho khách. | Nên có |

### 5.4 Nhóm D — Lễ tân (Front Desk)

| Mã | Yêu cầu | Ưu tiên |
|---|---|:---:|
| FR-D01 | Bảng điều khiển lễ tân: **khách dự kiến đến hôm nay**, **dự kiến đi hôm nay**, **đang lưu trú**, **phòng trống**. | Bắt buộc |
| FR-D02 | **Check-in khách có đặt trước**: tra mã đơn, đối chiếu giấy tờ, gán phòng cụ thể, nhập danh sách khách ở cùng, tạo Stay + Folio, phòng chuyển `Occupied`. | Bắt buộc |
| FR-D03 | **Check-in khách vãng lai (Walk-in)**: tạo hồ sơ khách nhanh, chọn phòng trống, nhận cọc, tạo Stay + Folio trong một luồng. | Bắt buộc |
| FR-D04 | **Thêm khách** vào lượt lưu trú đang diễn ra; cảnh báo và tính phụ thu khi vượt sức chứa chuẩn. | Bắt buộc |
| FR-D05 | **Đổi phòng** giữa kỳ lưu trú theo BR-09, bắt buộc nhập lý do. | Bắt buộc |
| FR-D06 | **Gia hạn lưu trú**: kéo dài ngày đi nếu phòng còn trống ở các đêm tiếp theo. | Bắt buộc |
| FR-D07 | **Check-out**: khóa folio, yêu cầu xác nhận đã kiểm phòng, chốt giờ trả thực tế, tự tính phụ thu trễ giờ, chuyển sang thanh toán, phòng chuyển `Dirty`. | Bắt buộc |
| FR-D08 | Xem chi tiết một lượt lưu trú: thông tin khách, phòng, các khoản chi phí, số dư còn phải trả. | Bắt buộc |

### 5.5 Nhóm E — Dịch vụ & Buồng phòng

| Mã | Yêu cầu | Ưu tiên |
|---|---|:---:|
| FR-E01 | **Ghi nhận dịch vụ** vào folio của phòng: chọn dịch vụ, số lượng, đơn giá (mặc định lấy từ danh mục), người ghi nhận, thời điểm. | Bắt buộc |
| FR-E02 | Bán dịch vụ có kho sẽ **tự trừ tồn kho**; chặn bán khi hết hàng (BR-12). | Bắt buộc |
| FR-E03 | **Hủy dòng dịch vụ** ghi nhầm khi folio còn mở — có log và hoàn tồn kho. | Bắt buộc |
| FR-E04 | **Bảng trạng thái buồng phòng**: danh sách phòng theo tầng kèm trạng thái dọn, ưu tiên phòng vừa check-out. | Bắt buộc |
| FR-E05 | Lễ tân **cập nhật trạng thái dọn**: bắt đầu dọn → dọn xong (`Available`); ghi người xác nhận và thời gian. | Bắt buộc |
| FR-E06 | **Kiểm minibar khi check-out**: nhập các mặt hàng khách đã dùng, hệ thống đẩy thẳng vào folio. | Bắt buộc |
| FR-E07 | **Báo hỏng hóc / yêu cầu bảo trì**: mô tả sự cố, mức độ, chuyển phòng sang `Maintenance`; đánh dấu đã sửa xong. | Bắt buộc |
| FR-E08 | **Yêu cầu phục vụ từ khách** (thêm khăn, gọi đồ): tạo yêu cầu, gán người xử lý, đánh dấu hoàn thành. | Nên có |

### 5.6 Nhóm F — Thu ngân & Thanh toán

| Mã | Yêu cầu | Ưu tiên |
|---|---|:---:|
| FR-F01 | **Folio** tự tổng hợp: tiền phòng theo đêm + dịch vụ + phụ thu − giảm giá − tiền cọc = số phải thu. | Bắt buộc |
| FR-F02 | **Giảm giá** theo số tiền hoặc theo %, bắt buộc nhập lý do; vượt hạn mức của lễ tân thì cần Admin. | Bắt buộc |
| FR-F03 | **Thanh toán đa phương thức**: tiền mặt, chuyển khoản, thẻ; ghi mã giao dịch với chuyển khoản/thẻ. | Bắt buộc |
| FR-F04 | **Đối trừ tiền cọc** vào hóa đơn; nếu cọc thừa thì lập **phiếu hoàn tiền** cho khách. | Bắt buộc |
| FR-F05 | **Chốt hóa đơn** khi số dư = 0, sinh số hóa đơn liên tục không trùng, in / xuất PDF hóa đơn chi tiết. | Bắt buộc |
| FR-F06 | **Hủy hóa đơn (Void)**: chỉ Admin, bắt buộc lý do, không xóa dữ liệu, có log. | Bắt buộc |
| FR-F07 | **Thu phí hủy / no-show** và lập chứng từ thu tương ứng. | Bắt buộc |
| FR-F08 | **Ca làm việc**: mở ca (nhập quỹ đầu ca) → giao dịch trong ca → đóng ca (đếm tiền thực tế, hệ thống tính chênh lệch). | Bắt buộc |
| FR-F09 | **Báo cáo cuối ca**: tổng thu theo phương thức, số hóa đơn, tiền mặt phải nộp, chênh lệch thừa/thiếu. | Bắt buộc |

### 5.7 Nhóm G — Báo cáo & Quản trị

| Mã | Yêu cầu | Ưu tiên |
|---|---|:---:|
| FR-G01 | **Doanh thu theo thời gian** (ngày / tháng / năm) kèm biểu đồ xu hướng. | Bắt buộc |
| FR-G02 | **Doanh thu theo nguồn thu**: tiền phòng, dịch vụ, phụ thu, phí hủy. | Bắt buộc |
| FR-G03 | **Công suất phòng**: tỷ lệ lấp đầy theo ngày/tháng; loại phòng `OutOfService` khỏi mẫu số. | Bắt buộc |
| FR-G04 | **Giá phòng bình quân (ADR)** và **doanh thu trên mỗi phòng khả dụng (RevPAR)**. | Nên có |
| FR-G05 | **Báo cáo dịch vụ bán chạy** và **tồn kho hiện tại**. | Nên có |
| FR-G06 | **Báo cáo theo nhân viên / theo ca**: doanh thu thu được, số giao dịch, chênh lệch quỹ. | Nên có |
| FR-G07 | **Nhật ký thao tác**: xem và lọc theo người dùng / hành động / khoảng thời gian (BR-11). | Bắt buộc |
| FR-G08 | Xuất mọi báo cáo ra **Excel / CSV**. | Nên có |
| FR-G09 | **Dashboard tổng quan**: phòng trống / đang ở / chờ dọn, khách đến–đi hôm nay, doanh thu hôm nay. | Bắt buộc |

---

## 6. Luồng nghiệp vụ chính

### 6.1 Luồng chuẩn: đặt trước → rời đi

```
Khách gọi đặt phòng
   └─► Lễ tân tra phòng trống (FR-C01)
        └─► Tạo đơn + hồ sơ khách (FR-C03) ──► nhận cọc (FR-C05) ──► Confirmed
             └─► Đến ngày: Check-in (FR-D02) ──► Stay + Folio, phòng = Occupied
                  └─► Trong kỳ ở: ghi dịch vụ (FR-E01), đổi phòng (FR-D05), gia hạn (FR-D06)
                       └─► Check-out (FR-D07): khóa folio + kiểm minibar (FR-E06)
                            └─► Thanh toán (FR-F03) ──► Hóa đơn Settled ──► phòng = Dirty
                                 └─► Đánh dấu đã dọn (FR-E05) ──► phòng = Available
```

### 6.2 Luồng khách vãng lai (Walk-in)
Chọn phòng trống → tạo hồ sơ khách nhanh → nhận cọc → tạo Stay ngay (bỏ bước Reservation),
các bước sau giống luồng chuẩn.

### 6.3 Luồng ngoại lệ

| Tình huống | Xử lý |
|---|---|
| Khách không đến | Đánh dấu No-show, thu 100% cọc, giải phóng phòng (FR-C08) |
| Khách hủy đơn | Tính phí hủy theo BR-05, hoàn phần cọc còn lại (FR-C07) |
| Phòng hỏng khi khách đang ở | Đổi phòng lý do "sự cố", không thu chênh lệch phụ thu (BR-09) |
| Khách trả phòng sớm hơn dự kiến | Tính lại theo số đêm thực ở, hoàn phần chênh nếu đã thu trước |
| Khách không đủ tiền trả | Không cho check-out; ghi nhận công nợ cần Admin duyệt |
| Lỗi hệ thống khi đang thanh toán | Giao dịch chạy trong transaction — hoặc hoàn tất trọn vẹn, hoặc không ghi nhận gì |

---

## 7. Mô hình dữ liệu đề xuất

Các thực thể chính cần khai báo trong `Models/` và đăng ký DbSet trong `Data/HotelDbContext.cs`:

| Thực thể | Mô tả tóm tắt |
|---|---|
| `RoomType` | Loại phòng, sức chứa, giá/đêm, phí thêm người |
| `Room` | Phòng, tầng, loại phòng, trạng thái |
| `Guest` | Hồ sơ khách, giấy tờ tùy thân, cờ blacklist |
| `Reservation` | Đơn đặt: mã đơn, khách đứng tên, ngày đến/đi, trạng thái, nguồn đặt |
| `ReservationRoom` | Dòng phòng trong đơn (một đơn nhiều phòng), giá chốt |
| `Stay` | Lượt lưu trú: phòng, ngày giờ vào/ra thực tế, giá/đêm đã chốt |
| `StayGuest` | Khách ở cùng trong một Stay |
| `RoomChangeLog` | Lịch sử đổi phòng: phòng cũ, phòng mới, lý do, thời điểm |
| `Folio` | Sổ chi phí gắn với Stay |
| `FolioItem` | Dòng chi phí: loại (phòng / dịch vụ / phụ thu / giảm giá), số lượng, đơn giá |
| `Service` | Danh mục dịch vụ, cờ quản lý kho |
| `InventoryItem`, `InventoryTransaction` | Tồn kho và phiếu nhập / xuất / điều chỉnh |
| `HousekeepingTask` | Nhiệm vụ dọn phòng: phòng, người xác nhận đã dọn, trạng thái, thời gian |
| `MaintenanceRequest` | Báo hỏng: phòng, mô tả, mức độ, trạng thái xử lý |
| `Invoice` | Hóa đơn: số hóa đơn, tổng tiền, VAT, trạng thái |
| `Payment` | Giao dịch thu / hoàn: phương thức, số tiền, ca làm việc |
| `Deposit` | Tiền cọc gắn với Reservation, trạng thái đã đối trừ / đã hoàn |
| `CashierShift` | Ca làm việc: người mở, quỹ đầu ca, tiền đếm cuối ca, chênh lệch |
| `Employee`, `Role` | Nhân viên, tài khoản đăng nhập, vai trò |
| `AuditLog` | Nhật ký: người dùng, hành động, đối tượng, giá trị cũ/mới, thời điểm |
| `SystemSetting` | Tham số cấu hình (giờ chuẩn, VAT, mức phụ thu, chính sách hủy) |

**Ràng buộc dữ liệu quan trọng:**
- `Room.RoomNumber` duy nhất; `Guest.IdNumber` duy nhất (khi có nhập); `Invoice.InvoiceNo` duy nhất.
- Không xóa cứng bản ghi đã phát sinh giao dịch — dùng cờ ngừng sử dụng (soft delete).
- Mọi bản ghi có `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.
- Check-in / check-out / thanh toán phải chạy trong một database transaction.

---

## 8. Yêu cầu phi chức năng

| Mã | Yêu cầu |
|---|---|
| NFR-01 | **Hiệu năng**: thao tác thường dùng phản hồi < 2 giây với dữ liệu 3 năm vận hành; hỗ trợ ≥ 10 người dùng đồng thời. |
| NFR-02 | **Bảo mật**: mật khẩu băm (BCrypt/PBKDF2), phân quyền theo vai trò ở cả controller và giao diện, chống CSRF / XSS / SQL Injection, cookie xác thực HttpOnly. |
| NFR-03 | **Toàn vẹn dữ liệu**: nghiệp vụ nhiều bước dùng transaction; kiểm tra xung đột phòng ở cả tầng database chứ không chỉ tầng ứng dụng. |
| NFR-04 | **Khả dụng & sao lưu**: sao lưu database hằng ngày, có quy trình khôi phục. |
| NFR-05 | **Giao diện**: tiếng Việt, hoạt động tốt trên màn hình quầy lễ tân (≥ 1366×768), thao tác chính hoàn thành trong ≤ 3 cú nhấp. |
| NFR-06 | **In ấn**: hóa đơn và phiếu thu in được trên khổ A4 và máy in nhiệt K80. |
| NFR-07 | **Khả năng truy vết**: mọi thay đổi ảnh hưởng tiền bạc đều truy được người thực hiện và thời điểm. |
| NFR-08 | **Bảo trì**: kiến trúc phân tầng (Controller → Service → EF Core), quy ước đặt tên thống nhất, có dữ liệu mẫu (seed) để chạy thử. |
| NFR-09 | **Tương thích**: Chrome / Edge / Firefox bản mới; máy chủ Windows + SQL Server 2019 trở lên. |

---

## 9. Mức độ ưu tiên triển khai

| Giai đoạn | Nội dung |
|---|---|
| **1 — Nền tảng** | Entity + migration, đăng nhập/phân quyền, danh mục loại phòng/phòng/dịch vụ/nhân viên, cấu hình hệ thống |
| **2 — Lõi vận hành** | Hồ sơ khách, tra phòng trống, đặt phòng + cọc, check-in, check-out, folio, thanh toán, hóa đơn |
| **3 — Hỗ trợ** | Buồng phòng, dịch vụ + tồn kho, đổi phòng, gia hạn, hủy / no-show, ca làm việc |
| **4 — Quản trị** | Báo cáo doanh thu / công suất, dashboard, audit log, xuất Excel, in hóa đơn |

---

## 10. Tiêu chí nghiệm thu

1. Tạo được đơn đặt phòng và hệ thống **từ chối** đơn thứ hai trùng phòng, trùng ngày.
2. Check-in → ghi 2 dịch vụ → check-out trễ giờ → hóa đơn tính đúng: tiền phòng theo đêm +
   dịch vụ + phụ thu trễ − cọc + VAT.
3. Check-out xong, phòng tự chuyển `Dirty`; sau khi đánh dấu đã dọn thì thành `Available` và phòng
   đó lập tức xuất hiện trong kết quả tra phòng trống.
4. Hủy đơn trước 12 giờ so với ngày đến thì hệ thống thu đúng 100% cọc và ghi vào doanh thu.
5. Bán 1 lon nước minibar thì tồn kho giảm đúng 1 đơn vị; hủy dòng đó thì tồn hoàn lại.
6. Đóng ca: tổng tiền mặt hệ thống khớp với các phiếu thu tiền mặt phát sinh trong ca.
7. Tài khoản lễ tân không mở được trang quản lý nhân viên và trang nhật ký thao tác.
8. Báo cáo doanh thu tháng bằng đúng tổng các hóa đơn `Settled` trong tháng đó.
