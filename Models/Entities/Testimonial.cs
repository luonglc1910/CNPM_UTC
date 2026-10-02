using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

public class Testimonial : BaseEntity
{
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [MaxLength(50)]
    public string Source { get; set; } = string.Empty; // e.g. Google, TripAdvisor

    public bool IsActive { get; set; } = true;
}
