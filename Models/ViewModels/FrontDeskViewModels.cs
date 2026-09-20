using System.ComponentModel.DataAnnotations;
using HotelManagement.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Web.Models.ViewModels;

// ---------- SCR-D01 ----------

public class ArrivalItem
{
    public int ReservationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RoomTypeSummary { get; set; } = string.Empty;
    public int Guests { get; set; }
    public decimal DepositPaid { get; set; }
    public bool CanCheckIn { get; set; }
}

public class DepartureItem
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime ExpectedCheckOut { get; set; }
    public int Nights { get; set; }
    public decimal BalanceDue { get; set; }
    public bool IsInspected { get; set; }
    public bool OverdueNoon { get; set; }
}

public class InHouseItem
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime ActualCheckIn { get; set; }
    public DateTime ExpectedCheckOut { get; set; }
}

public class RoomGridItem
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public RoomStatus Status { get; set; }
}

public class FrontDeskDashboardViewModel
{
    public IReadOnlyList<ArrivalItem> Arrivals { get; set; } = new List<ArrivalItem>();
    public IReadOnlyList<DepartureItem> Departures { get; set; } = new List<DepartureItem>();
    public IReadOnlyList<InHouseItem> InHouse { get; set; } = new List<InHouseItem>();
    public IReadOnlyList<RoomGridItem> Rooms { get; set; } = new List<RoomGridItem>();

    public int AvailableCount { get; set; }
    public int OccupiedCount { get; set; }
    public int DirtyCount { get; set; }
    public int MaintenanceCount { get; set; }
}

// ---------- SCR-D02 ----------

public class CheckInRoomAssignment
{
    public int ReservationRoomId { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int StandardCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal PricePerNight { get; set; }

    public int? SelectedRoomId { get; set; }
    public IReadOnlyList<SelectListItem> AvailableRooms { get; set; } = new List<SelectListItem>();
}

public class CheckInViewModel
{
    public int ReservationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? IdNumber { get; set; }
    public bool GuestBlacklisted { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }
    public decimal DepositPaid { get; set; }

    [Display(Name = "Đã đối chiếu giấy tờ tùy thân")]
    public bool IdVerified { get; set; }

    [Display(Name = "Giờ nhận phòng thực tế")]
    public DateTime ActualCheckIn { get; set; } = DateTime.Now;

    public List<CheckInRoomAssignment> Rooms { get; set; } = new();
}

// ---------- SCR-D03 ----------

public class WalkInViewModel
{
    [Display(Name = "Khách có sẵn")]
    public int? ExistingGuestId { get; set; }

    // Khách mới (khi ExistingGuestId trống)
    [Display(Name = "Họ tên")]
    [MaxLength(100)]
    public string? FullName { get; set; }

    [Display(Name = "Loại giấy tờ")]
    public GuestIdType IdType { get; set; } = GuestIdType.CitizenId;

    [Display(Name = "Số giấy tờ")]
    [MaxLength(20)]
    public string? IdNumber { get; set; }

    [Display(Name = "SĐT")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Quốc tịch")]
    [MaxLength(50)]
    public string Nationality { get; set; } = "Việt Nam";

    [Display(Name = "Phòng")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng.")]
    public int RoomId { get; set; }

    [Display(Name = "Ngày đi dự kiến")]
    [DataType(DataType.Date)]
    public DateTime ExpectedCheckOut { get; set; } = DateTime.Now.Date.AddDays(1);

    [Display(Name = "Người lớn")]
    [Range(1, 50)]
    public int Adults { get; set; } = 1;

    [Display(Name = "Trẻ em")]
    [Range(0, 50)]
    public int Children { get; set; }

    // Đặt cọc tùy chọn
    [Display(Name = "Tiền cọc")]
    [Range(0, double.MaxValue)]
    public decimal DepositAmount { get; set; }

    [Display(Name = "Phương thức")]
    public PaymentMethod DepositMethod { get; set; } = PaymentMethod.Cash;

    [Display(Name = "Mã giao dịch")]
    [MaxLength(50)]
    public string? TransactionRef { get; set; }

    public bool HasOpenShift { get; set; }
    public IReadOnlyList<SelectListItem> GuestOptions { get; set; } = new List<SelectListItem>();

    /// <summary>
    /// Id những khách đang nằm trong danh sách hạn chế — SCR-B05, SCR-D03.
    /// Gửi cả danh sách xuống thay vì một cờ, để cảnh báo hiện ngay lúc chọn khách
    /// chứ không phải chờ gửi form rồi mới biết.
    /// </summary>
    public IReadOnlyList<int> BlacklistedGuestIds { get; set; } = new List<int>();
    public IReadOnlyList<SelectListItem> RoomOptions { get; set; } = new List<SelectListItem>();
}

// ---------- SCR-D04 ----------

public class StayHistoryLine
{
    public DateTime At { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class StayDetailViewModel
{
    public Stay Stay { get; set; } = null!;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string PrimaryGuestName { get; set; } = string.Empty;
    public IReadOnlyList<string> Guests { get; set; } = new List<string>();
    public BillingFolioSummary Summary { get; set; } = new();
    public IReadOnlyList<StayHistoryLine> History { get; set; } = new List<StayHistoryLine>();
}

// ---------- SCR-D05 ----------

public class AddGuestViewModel
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int CurrentGuestCount { get; set; }
    public int StandardCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public decimal ExtraGuestFee { get; set; }
    public int RemainingNights { get; set; }

    [Display(Name = "Họ tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Loại giấy tờ")]
    public GuestIdType IdType { get; set; } = GuestIdType.CitizenId;

    [Display(Name = "Số giấy tờ")]
    [MaxLength(20)]
    public string? IdNumber { get; set; }

    [Display(Name = "Quốc tịch")]
    [MaxLength(50)]
    public string Nationality { get; set; } = "Việt Nam";

    [Display(Name = "Là trẻ em")]
    public bool IsChild { get; set; }
}

// ---------- SCR-D06 ----------

public class ChangeRoomViewModel
{
    public int StayId { get; set; }
    public string CurrentRoomNumber { get; set; } = string.Empty;
    public string CurrentRoomType { get; set; } = string.Empty;
    public decimal CurrentPricePerNight { get; set; }
    public bool IsAdmin { get; set; }

    [Display(Name = "Phòng mới")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng đích.")]
    public int NewRoomId { get; set; }

    [Display(Name = "Lý do đổi phòng")]
    [Required(ErrorMessage = "Vui lòng nhập lý do đổi phòng.")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Miễn chênh lệch giá (chỉ Admin)")]
    public bool WaivePriceDifference { get; set; }

    public IReadOnlyList<SelectListItem> AvailableRooms { get; set; } = new List<SelectListItem>();
}

// ---------- SCR-D07 ----------

public class ExtendStayViewModel
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CurrentCheckOut { get; set; }
    public decimal PricePerNight { get; set; }
    public bool IsAdmin { get; set; }

    [Display(Name = "Ngày đi mới")]
    [DataType(DataType.Date)]
    public DateTime NewCheckOut { get; set; }

    [Display(Name = "Giá / đêm áp dụng cho đêm thêm")]
    [Range(0, double.MaxValue)]
    public decimal ExtraNightPrice { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500)]
    public string? Notes { get; set; }
}

// ---------- SCR-D08 ----------

public class CheckOutViewModel
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime ActualCheckIn { get; set; }
    public DateTime ExpectedCheckOut { get; set; }
    public int PlannedNights { get; set; }
    public bool IsAdmin { get; set; }

    // Điều kiện BR-08
    public bool IsInspected { get; set; }
    public bool IsFolioLocked { get; set; }
    public bool CanProceed => IsInspected;

    [Display(Name = "Giờ trả phòng thực tế")]
    public DateTime ActualCheckOut { get; set; } = DateTime.Now;

    public string? LateSurchargeDescription { get; set; }
    public decimal LateSurchargeAmount { get; set; }
    public int ExtraNightsFromLate { get; set; }

    [Display(Name = "Miễn phụ thu trễ giờ (chỉ Admin)")]
    public bool WaiveLateSurcharge { get; set; }

    public BillingFolioSummary Summary { get; set; } = new();
}
