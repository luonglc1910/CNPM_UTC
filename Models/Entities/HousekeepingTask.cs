using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Nhiệm vụ dọn phòng — FR-E04, FR-E05.
/// Một phòng chỉ có một nhiệm vụ đang mở tại một thời điểm.
/// </summary>
public class HousekeepingTask : BaseEntity
{
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public HousekeepingTaskStatus Status { get; set; } = HousekeepingTaskStatus.Pending;

    /// <summary>Nhân viên nhận dọn.</summary>
    public int? AssignedTo { get; set; }
    public Employee? AssignedEmployee { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Lượt lưu trú vừa trả phòng sinh ra nhiệm vụ này, nếu có.</summary>
    public int? StayId { get; set; }
    public Stay? Stay { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
