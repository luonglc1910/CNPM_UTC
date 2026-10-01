using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Biến động kho — FR-A05, BR-12. Bảng chỉ ghi thêm, không sửa, không xóa:
/// sai sót được chỉnh bằng một phiếu điều chỉnh mới có lý do.
/// </summary>
public class InventoryTransaction : BaseEntity
{
    public int HotelServiceId { get; set; }
    public HotelService HotelService { get; set; } = null!;

    public InventoryTransactionType Type { get; set; }

    /// <summary>Dương khi nhập/hoàn, âm khi bán/xuất.</summary>
    public int Quantity { get; set; }

    /// <summary>Tồn sau giao dịch — chốt lại để đối chiếu báo cáo (FR-G05).</summary>
    public int StockAfter { get; set; }

    /// <summary>Dòng folio sinh ra giao dịch bán, nếu có.</summary>
    public int? FolioItemId { get; set; }
    public FolioItem? FolioItem { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}
