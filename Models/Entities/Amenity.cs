using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

public class Amenity : BaseEntity
{
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? IconSvg { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
}

public class RoomTypeAmenity
{
    public int RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }

    public int AmenityId { get; set; }
    public Amenity? Amenity { get; set; }
}
