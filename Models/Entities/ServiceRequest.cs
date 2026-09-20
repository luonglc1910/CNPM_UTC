using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Yêu cầu bảo trì hoặc yêu cầu phục vụ — FR-E07, FR-E08.
/// Gộp hai loại vào một bảng vì vòng đời xử lý giống nhau, chỉ khác hệ quả lên trạng thái phòng.
/// </summary>
public class ServiceRequest : BaseEntity
{
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    public RequestType Type { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public RequestPriority Priority { get; set; } = RequestPriority.Medium;
    public RequestStatus Status { get; set; } = RequestStatus.New;

    public int? AssignedTo { get; set; }
    public Employee? AssignedEmployee { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string? Resolution { get; set; }
}
