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
