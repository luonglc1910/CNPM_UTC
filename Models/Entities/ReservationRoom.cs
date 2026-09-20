namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Một dòng phòng trong đơn đặt phòng — FR-C03.
/// RoomId để trống nghĩa là mới đặt theo loại phòng, sẽ xếp phòng cụ thể khi check-in.
/// </summary>
public class ReservationRoom : BaseEntity
{
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    public int Adults { get; set; } = 1;
    public int Children { get; set; }

    /// <summary>Giá/đêm chốt tại thời điểm đặt — BR-02.</summary>
    public decimal PricePerNight { get; set; }
}
