using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

public class Promotion : BaseEntity
{
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public decimal DisplayPrice { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsHeroOffer { get; set; }

    public int? RelatedRoomTypeId { get; set; }
    public RoomType? RelatedRoomType { get; set; }

    public bool IsActive { get; set; } = true;
}
