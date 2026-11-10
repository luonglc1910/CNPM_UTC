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

    [MaxLength(50)]
    public string PromoCode { get; set; } = string.Empty;
    public decimal DiscountDailyPercent { get; set; }
    public decimal DiscountHourlyPercent { get; set; }
    public decimal DiscountOvernightPercent { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsHeroOffer { get; set; }

    [MaxLength(200)]
    public string? ApplicableRoomTypeIds { get; set; }

    public bool IsActive { get; set; } = true;
}
