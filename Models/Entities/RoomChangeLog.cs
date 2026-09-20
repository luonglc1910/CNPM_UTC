using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>Lịch sử đổi phòng giữa kỳ lưu trú — FR-D05, BR-09. Lý do là bắt buộc.</summary>
public class RoomChangeLog : BaseEntity
{
    public int StayId { get; set; }
    public Stay Stay { get; set; } = null!;

    public int FromRoomId { get; set; }
    public Room FromRoom { get; set; } = null!;

    public int ToRoomId { get; set; }
    public Room ToRoom { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public decimal OldPricePerNight { get; set; }
    public decimal NewPricePerNight { get; set; }

    /// <summary>Admin miễn chênh lệch giá khi đổi phòng do lỗi khách sạn.</summary>
    public bool PriceDifferenceWaived { get; set; }
}
