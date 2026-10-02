using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

public class HotelGalleryImage : BaseEntity
{
    [Required]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Title { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
