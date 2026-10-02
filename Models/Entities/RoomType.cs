using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>Loại phòng và bảng giá theo đêm — FR-A01, BR-02.</summary>
public class RoomType : BaseEntity
{
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Sức chứa chuẩn — vượt mức này thì tính phụ thu thêm người (BR-03).</summary>
    public int StandardCapacity { get; set; }

    /// <summary>Sức chứa tối đa — vượt mức này thì chặn không cho nhận phòng.</summary>
    public int MaxCapacity { get; set; }

    public decimal BasePricePerNight { get; set; }
    public decimal ExtraGuestFeePerNight { get; set; }
    public decimal ExtraBedFeePerNight { get; set; }

    /// <summary>Giá giờ đầu tiên khi thuê theo giờ — BR-13.</summary>
    public decimal PriceFirstHour { get; set; }

    /// <summary>Giá mỗi giờ từ giờ thứ hai trở đi; cũng là đơn giá phụ thu quá giờ gói qua đêm — BR-13.</summary>
    public decimal PriceExtraHour { get; set; }

    /// <summary>Giá trọn gói qua đêm — BR-13.</summary>
    public decimal PriceOvernight { get; set; }

    [MaxLength(500)]
    public string? Amenities { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; } // Short Description

    public string? DetailDescription { get; set; } // Detailed HTML description

    [MaxLength(1000)]
    public string? ImageUrl { get; set; } // Thumbnail Image

    [MaxLength(2000)]
    public string? AdditionalImageUrls { get; set; } // Gallery images

    [MaxLength(50)]
    public string? BedType { get; set; }

    [MaxLength(50)]
    public string? ViewType { get; set; }

    public int SizeSqm { get; set; }

    public bool IsFreeCancellation { get; set; } = true;
    public bool IsPayAtHotel { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
    public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
}
