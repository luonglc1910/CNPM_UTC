namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Khách ở cùng trong một phòng — FR-B04, FR-D04.
/// Dùng cho kiểm soát sức chứa (BR-03) và khai báo tạm trú (FR-B05).
/// </summary>
public class StayGuest : BaseEntity
{
    public int StayId { get; set; }
    public Stay Stay { get; set; } = null!;

    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    /// <summary>Người đứng tên lượt lưu trú.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Trẻ em không tính phụ thu thêm người.</summary>
    public bool IsChild { get; set; }
}
