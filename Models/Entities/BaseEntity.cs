namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Trường kiểm toán dùng chung cho mọi bảng — NFR-07.
/// Được gán tự động trong HotelDbContext.SaveChanges.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
