using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>Phòng vật lý — FR-A02.</summary>
public class Room : BaseEntity
{
    [MaxLength(10)]
    public string RoomNumber { get; set; } = string.Empty;

    public int Floor { get; set; }

    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public RoomStatus Status { get; set; } = RoomStatus.Available;

    /// <summary>Lý do khi chuyển sang Maintenance hoặc OutOfService (FR-A03).</summary>
    [MaxLength(255)]
    public string? StatusNote { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ReservationRoom> ReservationRooms { get; set; } = new List<ReservationRoom>();
    public ICollection<Stay> Stays { get; set; } = new List<Stay>();
    public ICollection<HousekeepingTask> HousekeepingTasks { get; set; } = new List<HousekeepingTask>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
