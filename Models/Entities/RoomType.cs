using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>Loại phòng và bảng giá theo đêm — FR-A01, BR-02.</summary>
public class RoomType : BaseEntity
{
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Sức chứa chuẩn — vượt mức này thì tính phụ thu thêm người (BR-03).</summary>
    public int StandardCapacity { get; set; }

    /// <summary>Sức chứa tối đa — vượt mức này thì chặn không cho nhận phòng.</summary>
    public int MaxCapacity { get; set; }

    public decimal BasePricePerNight { get; set; }
    public decimal ExtraGuestFeePerNight { get; set; }
    public decimal ExtraBedFeePerNight { get; set; }

    [MaxLength(500)]
    public string? Amenities { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
