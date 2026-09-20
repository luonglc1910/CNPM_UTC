using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Dịch vụ bán cho khách — FR-A04. Tồn kho gắn luôn vào đây (FR-A05)
/// nên không cần bảng InventoryItem riêng; biến động kho nằm ở InventoryTransaction.
/// Đặt tên HotelService để tránh trùng với Microsoft.Extensions IService.
/// </summary>
public class HotelService : BaseEntity
{
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ServiceCategory Category { get; set; } = ServiceCategory.Other;

    public decimal UnitPrice { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>Bật thì mỗi lần bán sẽ trừ tồn kho — BR-12.</summary>
    public bool IsStockManaged { get; set; }

    public int StockQuantity { get; set; }

    /// <summary>Dưới mức này thì cảnh báo trên màn hình danh sách và dashboard.</summary>
    public int MinStockLevel { get; set; }

    /// <summary>Chỉ Admin bật; cho phép bán cả khi tồn bằng 0 — BR-12.</summary>
    public bool AllowNegativeStock { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
