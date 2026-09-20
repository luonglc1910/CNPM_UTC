using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Một dòng trên bảng tồn kho — SCR-A08.</summary>
public class InventoryStockItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Thời điểm của phiếu nhập gần nhất, rỗng khi chưa từng nhập.</summary>
    public DateTime? LastReceivedAt { get; set; }

    public bool IsLowStock => StockQuantity <= MinStockLevel;
}

/// <summary>Một dòng trong tab lịch sử giao dịch kho — SCR-A08.</summary>
public class InventoryTransactionListItemViewModel
{
    public DateTime CreatedAt { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public int StockAfter { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    /// <summary>Chứng từ liên quan: mã folio của dòng chi phí sinh ra giao dịch bán.</summary>
    public string? FolioCode { get; set; }

    public string? PerformedBy { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Màn hình tồn kho — hai tab dùng chung một URL, phân biệt bằng tham số <see cref="Tab"/>
/// để bộ lọc và phân trang của mỗi tab đi thẳng trong query string.
/// </summary>
public class InventoryIndexViewModel
{
    public const string StockTab = "stock";
    public const string HistoryTab = "history";

    public string Tab { get; set; } = StockTab;

    public bool IsHistoryTab => string.Equals(Tab, HistoryTab, StringComparison.OrdinalIgnoreCase);

    [Display(Name = "Tìm theo mã hoặc tên dịch vụ")]
    public string? Keyword { get; set; }

    [Display(Name = "Dịch vụ")]
    public int? HotelServiceId { get; set; }

    [Display(Name = "Loại giao dịch")]
    public InventoryTransactionType? Type { get; set; }

    [Display(Name = "Chỉ dịch vụ dưới định mức")]
    public bool LowStockOnly { get; set; }

    public PagedList<InventoryStockItemViewModel> Stocks { get; set; } = new();
    public PagedList<InventoryTransactionListItemViewModel> History { get; set; } = new();

    /// <summary>Số dịch vụ dưới định mức trên toàn danh mục, không theo bộ lọc.</summary>
    public int LowStockCount { get; set; }

    public IReadOnlyList<SelectListItem> ServiceOptions { get; set; } = Array.Empty<SelectListItem>();

    public bool HasFilter => !string.IsNullOrWhiteSpace(Keyword)
                             || HotelServiceId is not null
                             || Type is not null
                             || LowStockOnly;
}

/// <summary>Form nhập kho — SCR-A09.</summary>
public class InventoryReceiveViewModel
{
    [Display(Name = "Dịch vụ")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ cần nhập kho.")]
    public int HotelServiceId { get; set; }

    [Display(Name = "Số lượng nhập")]
    [Range(1, 1_000_000, ErrorMessage = "Số lượng nhập phải lớn hơn 0.")]
    public int Quantity { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    public string? Note { get; set; }

    /// <summary>Chỉ những dịch vụ đang dùng và có quản lý kho.</summary>
    public IReadOnlyList<SelectListItem> ServiceOptions { get; set; } = Array.Empty<SelectListItem>();
}

/// <summary>Form điều chỉnh kho (kiểm kê) — SCR-A09.</summary>
public class InventoryAdjustViewModel
{
    [Display(Name = "Dịch vụ")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ cần điều chỉnh.")]
    public int HotelServiceId { get; set; }

    [Display(Name = "Tồn thực tế đếm được")]
    [Range(0, 1_000_000, ErrorMessage = "Tồn thực tế không được âm.")]
    public int CountedQuantity { get; set; }

    [Display(Name = "Lý do")]
    [Required(ErrorMessage = "Vui lòng nhập lý do điều chỉnh (hỏng, mất, kiểm kê...).")]
    [MaxLength(500, ErrorMessage = "Lý do tối đa 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> ServiceOptions { get; set; } = Array.Empty<SelectListItem>();
}
