namespace HotelManagement.Web.Models;

/// <summary>
/// Nhãn tiếng Việt và màu hiển thị cho các enum nghiệp vụ.
/// Bảng màu trạng thái phòng theo docs/screens/00-conventions.md mục 6.
/// </summary>
public static class EnumDisplay
{
    public static string ToDisplayName(this RoomStatus status) => status switch
    {
        RoomStatus.Available => "Trống",
        RoomStatus.Reserved => "Đã đặt",
        RoomStatus.Occupied => "Đang ở",
        RoomStatus.Dirty => "Chờ dọn",
        RoomStatus.Maintenance => "Bảo trì",
        RoomStatus.OutOfService => "Ngừng khai thác",
        _ => status.ToString()
    };

    /// <summary>Lớp badge Bootstrap tương ứng màu quy ước.</summary>
    public static string ToBadgeClass(this RoomStatus status) => status switch
    {
        RoomStatus.Available => "text-bg-success",
        RoomStatus.Reserved => "text-bg-primary",
        RoomStatus.Occupied => "text-bg-warning",
        RoomStatus.Dirty => "text-bg-secondary",
        RoomStatus.Maintenance => "text-bg-danger",
        RoomStatus.OutOfService => "text-bg-dark",
        _ => "text-bg-light"
    };

    public static string ToDisplayName(this ServiceCategory category) => category switch
    {
        ServiceCategory.FoodAndBeverage => "Ăn uống",
        ServiceCategory.Minibar => "Minibar",
        ServiceCategory.Laundry => "Giặt ủi",
        ServiceCategory.Transport => "Thuê xe / Đưa đón",
        ServiceCategory.Other => "Khác",
        _ => category.ToString()
    };

    public static string ToDisplayName(this GuestIdType type) => type switch
    {
        GuestIdType.CitizenId => "CCCD",
        GuestIdType.IdCard => "CMND",
        GuestIdType.Passport => "Hộ chiếu",
        _ => type.ToString()
    };

    public static string ToDisplayName(this Gender gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        Gender.Other => "Khác",
        _ => gender.ToString()
    };

    public static string ToDisplayName(this EmployeeRole role) => role switch
    {
        EmployeeRole.Admin => "Quản lý",
        EmployeeRole.Receptionist => "Lễ tân",
        _ => role.ToString()
    };

    public static string ToDisplayName(this EmployeeStatus status) => status switch
    {
        EmployeeStatus.Active => "Đang làm",
        EmployeeStatus.Resigned => "Đã nghỉ",
        EmployeeStatus.Locked => "Bị khóa",
        _ => status.ToString()
    };

    public static string ToBadgeClass(this EmployeeStatus status) => status switch
    {
        EmployeeStatus.Active => "text-bg-success",
        EmployeeStatus.Resigned => "text-bg-secondary",
        EmployeeStatus.Locked => "text-bg-danger",
        _ => "text-bg-light"
    };

    public static string ToDisplayName(this InventoryTransactionType type) => type switch
    {
        InventoryTransactionType.Receive => "Nhập",
        InventoryTransactionType.Sale => "Bán",
        InventoryTransactionType.Adjust => "Điều chỉnh",
        InventoryTransactionType.Return => "Hoàn",
        _ => type.ToString()
    };

    public static string ToBadgeClass(this InventoryTransactionType type) => type switch
    {
        InventoryTransactionType.Receive => "text-bg-success",
        InventoryTransactionType.Sale => "text-bg-primary",
        InventoryTransactionType.Adjust => "text-bg-warning",
        InventoryTransactionType.Return => "text-bg-info",
        _ => "text-bg-light"
    };

    public static string ToDisplayName(this ReservationStatus status) => status switch
    {
        ReservationStatus.Draft => "Nháp",
        ReservationStatus.Confirmed => "Đã xác nhận",
        ReservationStatus.CheckedIn => "Đã nhận phòng",
        ReservationStatus.CheckedOut => "Đã trả phòng",
        ReservationStatus.Cancelled => "Đã hủy",
        ReservationStatus.NoShow => "Không đến",
        _ => status.ToString()
    };

    public static string ToBadgeClass(this ReservationStatus status) => status switch
    {
        ReservationStatus.Draft => "text-bg-secondary",
        ReservationStatus.Confirmed => "text-bg-primary",
        ReservationStatus.CheckedIn => "text-bg-warning",
        ReservationStatus.CheckedOut => "text-bg-success",
        ReservationStatus.Cancelled => "text-bg-secondary",
        ReservationStatus.NoShow => "text-bg-danger",
        _ => "text-bg-light"
    };

    public static string ToDisplayName(this ReservationSource source) => source switch
    {
        ReservationSource.Phone => "Điện thoại",
        ReservationSource.WalkIn => "Trực tiếp tại quầy",
        ReservationSource.Referral => "Giới thiệu",
        ReservationSource.Other => "Khác",
        _ => source.ToString()
    };

    public static string ToDisplayName(this FolioItemType type) => type switch
    {
        FolioItemType.Room => "Phòng",
        FolioItemType.Service => "Dịch vụ",
        FolioItemType.Surcharge => "Phụ thu",
        FolioItemType.Discount => "Giảm giá",
        _ => type.ToString()
    };

    public static string ToDisplayName(this RequestType type) => type switch
    {
        RequestType.Maintenance => "Báo hỏng / Bảo trì",
        RequestType.GuestService => "Yêu cầu phục vụ",
        _ => type.ToString()
    };

    public static string ToDisplayName(this RequestPriority priority) => priority switch
    {
        RequestPriority.Low => "Thấp",
        RequestPriority.Medium => "Trung bình",
        RequestPriority.Urgent => "Khẩn cấp",
        _ => priority.ToString()
    };

    public static string ToBadgeClass(this RequestPriority priority) => priority switch
    {
        RequestPriority.Low => "text-bg-secondary",
        RequestPriority.Medium => "text-bg-info",
        RequestPriority.Urgent => "text-bg-danger",
        _ => "text-bg-light"
    };

    public static string ToDisplayName(this RequestStatus status) => status switch
    {
        RequestStatus.New => "Mới",
        RequestStatus.InProgress => "Đang xử lý",
        RequestStatus.Completed => "Hoàn thành",
        RequestStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    public static string ToBadgeClass(this RequestStatus status) => status switch
    {
        RequestStatus.New => "text-bg-primary",
        RequestStatus.InProgress => "text-bg-warning",
        RequestStatus.Completed => "text-bg-success",
        RequestStatus.Cancelled => "text-bg-secondary",
        _ => "text-bg-light"
    };

    public static string ToDisplayName(this ShiftStatus status) => status switch
    {
        ShiftStatus.Open => "Đang mở",
        ShiftStatus.Closed => "Đã đóng",
        _ => status.ToString()
    };

    public static string ToBadgeClass(this ShiftStatus status) => status switch
    {
        ShiftStatus.Open => "text-bg-success",
        ShiftStatus.Closed => "text-bg-secondary",
        _ => "text-bg-light"
    };

    public static string ToDisplayName(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.Card => "Thẻ",
        _ => method.ToString()
    };

    public static string ToDisplayName(this InvoiceStatus status) => status switch
    {
        InvoiceStatus.Settled => "Đã thanh toán",
        InvoiceStatus.Void => "Đã hủy",
        InvoiceStatus.Debt => "Còn nợ",
        _ => status.ToString()
    };

    public static string ToBadgeClass(this InvoiceStatus status) => status switch
    {
        InvoiceStatus.Settled => "text-bg-success",
        InvoiceStatus.Void => "text-bg-danger",
        InvoiceStatus.Debt => "text-bg-warning",
        _ => "text-bg-light"
    };

    /// <summary>Màu nhãn nhóm dịch vụ trên SCR-A06 — chỉ để đọc bảng cho nhanh.</summary>
    public static string ToBadgeClass(this ServiceCategory category) => category switch
    {
        ServiceCategory.FoodAndBeverage => "text-bg-success",
        ServiceCategory.Minibar => "text-bg-primary",
        ServiceCategory.Laundry => "text-bg-info",
        ServiceCategory.Transport => "text-bg-warning",
        ServiceCategory.Other => "text-bg-secondary",
        _ => "text-bg-light"
    };
}
