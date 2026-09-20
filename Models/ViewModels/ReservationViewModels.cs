using System.ComponentModel.DataAnnotations;
using HotelManagement.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Web.Models.ViewModels;

// ---------- SCR-C01: danh sách đơn ----------

public class ReservationListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }
    public int RoomCount { get; set; }
    public string RoomTypeSummary { get; set; } = string.Empty;
    public decimal EstimatedTotal { get; set; }
    public decimal DepositPaid { get; set; }
    public ReservationStatus Status { get; set; }
    public ReservationSource Source { get; set; }
}

public class ReservationIndexViewModel
{
    [Display(Name = "Ngày đến từ")]
    [DataType(DataType.Date)]
    public DateTime? CheckInFrom { get; set; }

    [Display(Name = "Ngày đến đến")]
    [DataType(DataType.Date)]
    public DateTime? CheckInTo { get; set; }

    [Display(Name = "Trạng thái")]
    public ReservationStatus? Status { get; set; }

    [Display(Name = "Nguồn đặt")]
    public ReservationSource? Source { get; set; }

    [Display(Name = "Tìm kiếm")]
    public string? Keyword { get; set; }

    /// <summary>Chỉ true khi người dùng chủ động lọc; mặc định màn hình đã áp bộ lọc "việc cần xử lý".</summary>
    public bool CustomFilter { get; set; }

    public PagedList<ReservationListItem> Results { get; set; } = new();

    public bool HasFilter =>
        CheckInFrom is not null || CheckInTo is not null || Status is not null
        || Source is not null || !string.IsNullOrWhiteSpace(Keyword);
}

// ---------- SCR-C02: tra cứu phòng trống ----------

public class AvailabilityRoomOption
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public bool NeedsCleaning { get; set; }
}

public class AvailabilityGroup
{
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public string RoomTypeCode { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int StandardCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public int TotalRooms { get; set; }
    public IReadOnlyList<AvailabilityRoomOption> AvailableRooms { get; set; } = new List<AvailabilityRoomOption>();

    public int AvailableCount => AvailableRooms.Count;
}

public class AvailabilitySearchViewModel
{
    [Display(Name = "Ngày đến")]
    [DataType(DataType.Date)]
    public DateTime? CheckIn { get; set; }

    [Display(Name = "Ngày đi")]
    [DataType(DataType.Date)]
    public DateTime? CheckOut { get; set; }

    [Display(Name = "Số khách")]
    [Range(1, 50, ErrorMessage = "Số khách phải từ 1 trở lên.")]
    public int Guests { get; set; } = 1;

    [Display(Name = "Loại phòng")]
    public int? RoomTypeId { get; set; }

    public int Nights { get; set; }
    public bool Searched { get; set; }

    public IReadOnlyList<AvailabilityGroup> Groups { get; set; } = new List<AvailabilityGroup>();
    public IReadOnlyList<SelectListItem> RoomTypeOptions { get; set; } = new List<SelectListItem>();
}

// ---------- SCR-C04 / C06: tạo & sửa đơn ----------

public class ReservationRoomInput
{
    [Display(Name = "Loại phòng")]
    public int RoomTypeId { get; set; }

    [Display(Name = "Phòng")]
    public int? RoomId { get; set; }

    [Display(Name = "Người lớn")]
    [Range(1, 50, ErrorMessage = "Ít nhất 1 người lớn.")]
    public int Adults { get; set; } = 1;

    [Display(Name = "Trẻ em")]
    [Range(0, 50)]
    public int Children { get; set; }

    [Display(Name = "Giá / đêm")]
    [Range(0, double.MaxValue)]
    public decimal PricePerNight { get; set; }
}

public class ReservationFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Khách đứng tên")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khách đứng tên.")]
    public int PrimaryGuestId { get; set; }

    [Display(Name = "Ngày đến")]
    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [Display(Name = "Ngày đi")]
    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    [Display(Name = "Nguồn đặt")]
    public ReservationSource Source { get; set; } = ReservationSource.Phone;

    [Display(Name = "Yêu cầu đặc biệt")]
    [MaxLength(500)]
    public string? SpecialRequests { get; set; }

    [Display(Name = "Ghi chú nội bộ")]
    [MaxLength(500)]
    public string? InternalNotes { get; set; }

    public List<ReservationRoomInput> Rooms { get; set; } = new();

    /// <summary>true = Lưu và xác nhận (Confirmed); false = Lưu nháp (Draft).</summary>
    public bool ConfirmNow { get; set; } = true;

    // Dữ liệu đổ vào form
    public string? PrimaryGuestName { get; set; }
    public bool PrimaryGuestBlacklisted { get; set; }
    public IReadOnlyList<SelectListItem> GuestOptions { get; set; } = new List<SelectListItem>();
    public IReadOnlyList<SelectListItem> RoomTypeOptions { get; set; } = new List<SelectListItem>();

    /// <summary>Toàn bộ phòng đang khai thác, kèm data-roomtype để JS lọc theo loại.</summary>
    public IReadOnlyList<RoomPickerItem> AllRooms { get; set; } = new List<RoomPickerItem>();

    /// <summary>Giá đêm mặc định theo loại phòng, để JS tự điền khi chọn loại.</summary>
    public IReadOnlyDictionary<int, decimal> RoomTypePrices { get; set; } = new Dictionary<int, decimal>();
}

public class RoomPickerItem
{
    public int RoomId { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
}

// ---------- SCR-C05: chi tiết đơn ----------

public class ReservationRoomLine
{
    public string RoomTypeName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal LineTotal { get; set; }
}

public class ReservationDepositLine
{
    public DateTime ReceivedAt { get; set; }
    public decimal Amount { get; set; }
    public DepositStatus Status { get; set; }
}

public class ReservationDetailsViewModel
{
    public Reservation Reservation { get; set; } = null!;
    public string GuestName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? IdNumber { get; set; }
    public bool GuestBlacklisted { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    public IReadOnlyList<ReservationRoomLine> Rooms { get; set; } = new List<ReservationRoomLine>();
    public IReadOnlyList<ReservationDepositLine> Deposits { get; set; } = new List<ReservationDepositLine>();

    public decimal DepositPaid { get; set; }
    public decimal EstimatedRemaining { get; set; }

    // Cờ điều khiển nút theo trạng thái (SCR-C01)
    public bool CanEdit { get; set; }
    public bool CanTakeDeposit { get; set; }
    public bool CanCheckIn { get; set; }
    public bool CanCancel { get; set; }
    public bool CanMarkNoShow { get; set; }
}

// ---------- SCR-C07: thu cọc ----------

public class DepositFormViewModel
{
    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;

    public decimal EstimatedTotal { get; set; }
    public decimal AlreadyPaid { get; set; }
    public decimal SuggestedAmount { get; set; }

    public bool HasOpenShift { get; set; }

    [Display(Name = "Số tiền cọc")]
    [Range(1, double.MaxValue, ErrorMessage = "Số tiền cọc phải lớn hơn 0.")]
    public decimal Amount { get; set; }

    [Display(Name = "Phương thức")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [Display(Name = "Mã giao dịch")]
    [MaxLength(50)]
    public string? TransactionRef { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500)]
    public string? Notes { get; set; }
}

// ---------- SCR-C08: hủy đơn ----------

public class CancelReservationViewModel
{
    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }

    public double HoursBeforeArrival { get; set; }
    public string PolicyDescription { get; set; } = string.Empty;
    public decimal DepositPaid { get; set; }
    public decimal CancellationFee { get; set; }
    public decimal RefundAmount { get; set; }

    public bool HasOpenShift { get; set; }
    public bool IsAdmin { get; set; }

    [Display(Name = "Lý do hủy")]
    [Required(ErrorMessage = "Vui lòng nhập lý do hủy.")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Ghi chú")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Miễn phí hủy (chỉ Admin)")]
    public bool WaiveFee { get; set; }
}

// ---------- SCR-C09: đơn quá hạn / no-show ----------

public class OverdueReservationItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime? HoldUntil { get; set; }
    public string RoomSummary { get; set; } = string.Empty;
    public decimal DepositPaid { get; set; }
    public double HoursOverdue { get; set; }
}

public class NoShowListViewModel
{
    public IReadOnlyList<OverdueReservationItem> Items { get; set; } = new List<OverdueReservationItem>();
}

/// <summary>
/// Màn Đơn đặt phòng gộp hai tab: tất cả đơn (SCR-C01) và quá hạn / no-show (SCR-C09).
///
/// Hai tab là hai đường dẫn, mỗi tab giữ nguyên bảng và nút của mình. Gộp thành một bảng
/// sẽ phải thêm hai cột và ba nút chỉ có nghĩa với đơn quá hạn vào một bảng vốn đã mười cột.
/// </summary>
public class ReservationsPageViewModel
{
    public const string ListTab = "all";
    public const string NoShowTab = "noshow";

    public string Tab { get; set; } = ListTab;

    public bool IsNoShow => Tab == NoShowTab;

    public ReservationIndexViewModel? List { get; set; }
    public NoShowListViewModel? NoShow { get; set; }
}

// ===================== SCR-C03 — Sơ đồ phòng theo ngày =====================

/// <summary>Cách một ô trong sơ đồ được lấp — SCR-C03.</summary>
public enum RoomChartCellKind
{
    /// <summary>Không có gì phủ đêm này — bán được.</summary>
    Free = 0,

    /// <summary>Có đơn đặt chưa nhận phòng.</summary>
    Reserved = 1,

    /// <summary>Khách đang ở.</summary>
    Occupied = 2,

    /// <summary>Phòng không khai thác được cả kỳ: bảo trì hoặc ngừng khai thác.</summary>
    Blocked = 3
}

/// <summary>
/// Một dải liền trong sơ đồ — SCR-C03.
/// Nhiều đêm của cùng một đơn gộp thành một ô có <see cref="Span"/> để mắt nhìn ra
/// khoảng trống giữa các đơn, thay vì đếm từng ô rời.
/// </summary>
public class RoomChartSegment
{
    public RoomChartCellKind Kind { get; set; }

    /// <summary>Ngày bắt đầu dải, tính theo cột đầu tiên nó chiếm.</summary>
    public DateTime Date { get; set; }

    /// <summary>Số cột (số đêm) dải này chiếm.</summary>
    public int Span { get; set; } = 1;

    public int? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public string? GuestName { get; set; }
    public int Guests { get; set; }
    public string? StatusLabel { get; set; }

    /// <summary>Nội dung tooltip dựng sẵn ở service để view khỏi ghép chuỗi.</summary>
    public string? Tooltip { get; set; }
}

/// <summary>Một dòng phòng trong sơ đồ — SCR-C03.</summary>
public class RoomChartRow
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public RoomStatus CurrentStatus { get; set; }

    public IReadOnlyList<RoomChartSegment> Segments { get; set; } = new List<RoomChartSegment>();
}

/// <summary>Sơ đồ phòng theo ngày — SCR-C03, FR-C02.</summary>
public class RoomChartViewModel
{
    /// <summary>Trần số ngày hiển thị: quá rộng thì bảng tràn ngang và mất tác dụng nhìn nhanh.</summary>
    public const int MaxDays = 60;

    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; }

    [Display(Name = "Số ngày")]
    public int Days { get; set; } = 14;

    public IReadOnlyList<DateTime> Dates { get; set; } = new List<DateTime>();
    public IReadOnlyList<RoomChartRow> Rows { get; set; } = new List<RoomChartRow>();

    /// <summary>Bị cắt bớt vì người dùng yêu cầu khoảng rộng hơn trần cho phép.</summary>
    public bool Truncated { get; set; }

    public int FreeNights => Rows.Sum(r => r.Segments.Where(s => s.Kind == RoomChartCellKind.Free).Sum(s => s.Span));
    public int TotalNights => Rows.Count * Days;
}
