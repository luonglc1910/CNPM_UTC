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

    // Ảnh giá hai hình thức còn lại, chốt cùng lúc với giá đêm — BR-02, BR-13.
    // Chốt cả ba dù đơn chỉ dùng một, để đổi hình thức khi sửa đơn không phải tra lại bảng giá
    // đã có thể thay đổi trong lúc đó.
    public decimal PriceFirstHour { get; set; }
    public decimal PriceExtraHour { get; set; }
    public decimal PriceOvernight { get; set; }
}
