namespace HotelManagement.Web.Models;

/// <summary>Vai trò nhân viên. Ma trận phân quyền: docs/screens/README.md.</summary>
public enum EmployeeRole
{
    Admin = 1,
    Receptionist = 2,
    Housekeeping = 3
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
    Other = 9
}

/// <summary>Trạng thái hóa đơn — REQUIREMENTS mục 4.3.</summary>
public enum InvoiceStatus
{
    Settled = 1,
    Void = 2
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
