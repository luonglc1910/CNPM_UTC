namespace HotelManagement.Web.Models;

/// <summary>
/// Vai trò nhân viên. Ma trận phân quyền: docs/screens/README.md.
/// Nhân viên dọn phòng không có tài khoản đăng nhập — lễ tân cập nhật hộ (xem nhóm E).
/// </summary>
public enum EmployeeRole
{
    Admin = 1,
    Receptionist = 2
}

public enum EmployeeStatus
{
    Active = 1,
    Resigned = 2,
    Locked = 3
}

/// <summary>Trạng thái phòng — REQUIREMENTS mục 4.1.</summary>
public enum RoomStatus
{
    Available = 1,
    Reserved = 2,
    Occupied = 3,
    Dirty = 4,
    Maintenance = 5,
    OutOfService = 6
}

/// <summary>Trạng thái đơn đặt phòng — REQUIREMENTS mục 4.2.</summary>
public enum ReservationStatus
{
    Draft = 1,
    Confirmed = 2,
    CheckedIn = 3,
    CheckedOut = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum ReservationSource
{
    Phone = 1,
    WalkIn = 2,
    Referral = 3,
    Other = 9
}

public enum StayStatus
{
    CheckedIn = 1,
    CheckedOut = 2
}

public enum GuestIdType
{
    CitizenId = 1,
    IdCard = 2,
    Passport = 3
}

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 9
}

public enum ServiceCategory
{
    FoodAndBeverage = 1,
    Minibar = 2,
    Laundry = 3,
    Transport = 4,
    Other = 9
}

public enum InventoryTransactionType
{
    Receive = 1,
    Sale = 2,
    Adjust = 3,
    Return = 4
}

/// <summary>Loại dòng chi phí trên folio.</summary>
public enum FolioItemType
{
    Room = 1,
    Service = 2,
    Surcharge = 3,
    Discount = 4
}

/// <summary>Loại phụ thu — BR-03.</summary>
public enum SurchargeType
{
    None = 0,
    EarlyCheckIn = 1,
    LateCheckOut = 2,
    ExtraGuest = 3,
    ExtraBed = 4,

    /// <summary>Gói qua đêm trả sau giờ kết thúc gói — tính theo giờ, BR-13.</summary>
    OvernightOverstay = 5,

    Other = 9
}

/// <summary>
/// Hình thức thuê phòng — BR-13. Quyết định cách tính tiền phòng và ý nghĩa của
/// cặp giờ đến/đi, nên phải chốt ngay từ lúc lập đơn chứ không đổi được giữa chừng.
/// </summary>
public enum RentalType
{
    /// <summary>Theo ngày: nhận 14:00, trả 12:00 hôm sau; ra sớm vẫn tính đủ số đêm đã đặt.</summary>
    Daily = 1,

    /// <summary>Theo giờ: tính giờ đầu + các giờ tiếp theo, chốt lúc trả phòng.</summary>
    Hourly = 2,

    /// <summary>Qua đêm: gói phẳng 22:00 hôm nay → 10:00 hôm sau.</summary>
    Overnight = 3
}

/// <summary>Trạng thái hóa đơn — REQUIREMENTS mục 4.3.</summary>
public enum InvoiceStatus
{
    Settled = 1,
    Void = 2,

    /// <summary>
    /// Đã chốt nhưng khách chưa trả đủ, phần còn thiếu ghi công nợ — REQUIREMENTS mục 6.3, SCR-D08.
    /// Chỉ Admin được ghi nhận, bắt buộc lý do và có audit log.
    /// </summary>
    Debt = 3
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3
}

/// <summary>Bản chất khoản thu/chi. Refund và VoidAdjustment mang số tiền âm.</summary>
public enum PaymentType
{
    Deposit = 1,
    InvoiceSettlement = 2,
    Refund = 3,
    CancellationFee = 4,
    VoidAdjustment = 5
}

/// <summary>Vòng đời tiền cọc — BR-05.</summary>
public enum DepositStatus
{
    Held = 1,
    Applied = 2,
    Refunded = 3,
    Forfeited = 4
}

public enum HousekeepingTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Done = 3
}

public enum RequestType
{
    Maintenance = 1,
    GuestService = 2
}

public enum RequestPriority
{
    Low = 1,
    Medium = 2,
    Urgent = 3
}

public enum RequestStatus
{
    New = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ShiftStatus
{
    Open = 1,
    Closed = 2
}

/// <summary>
/// Ca theo khung giờ cố định trong ngày.
///   Ca 1: 08:00 – 16:00
///   Ca 2: 16:00 – 00:00
///   Ca 3: 00:00 – 08:00
/// </summary>
public enum ShiftSlot
{
    Ca1 = 1,  // 08:00 – 16:00
    Ca2 = 2,  // 16:00 – 00:00
    Ca3 = 3   // 00:00 – 08:00
}

public static class ShiftSlotExtensions
{
    public static ShiftSlot Resolve(DateTime time)
    {
        var h = time.Hour;
        if (h >= 8 && h < 16) return ShiftSlot.Ca1;
        if (h >= 16)           return ShiftSlot.Ca2;
        return ShiftSlot.Ca3;  // 00:00 – 07:59
    }

    public static string ToDisplayName(this ShiftSlot slot) => slot switch
    {
        ShiftSlot.Ca1 => "Ca 1 (08:00–16:00)",
        ShiftSlot.Ca2 => "Ca 2 (16:00–00:00)",
        ShiftSlot.Ca3 => "Ca 3 (00:00–08:00)",
        _             => slot.ToString()
    };

    public static string ToBadgeClass(this ShiftSlot slot) => slot switch
    {
        ShiftSlot.Ca1 => "bg-primary",
        ShiftSlot.Ca2 => "bg-warning text-dark",
        ShiftSlot.Ca3 => "bg-secondary",
        _             => "bg-light text-dark"
    };
}
