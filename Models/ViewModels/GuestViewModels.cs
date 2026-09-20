using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>
/// Che một phần số giấy tờ — quy tắc hiển thị của nhóm B: danh sách chỉ hiện dạng che,
/// số đầy đủ chỉ xuất hiện ở SCR-B03 và màn hình check-in.
/// </summary>
public static class IdNumberMask
{
    public static string Mask(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return "—";
        }

        var value = idNumber.Trim();

        // Số quá ngắn thì che gần hết: giữ 4 ký tự đầu cuối sẽ lộ gần như toàn bộ.
        if (value.Length <= 8)
        {
            return value.Length <= 2
                ? new string('*', value.Length)
                : value[..2] + new string('*', value.Length - 2);
        }

        return value[..4] + new string('*', value.Length - 8) + value[^4..];
    }
}

/// <summary>Một dòng trên bảng danh sách khách — SCR-B01.</summary>
public class GuestListItemViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Gender? Gender { get; set; }
    public GuestIdType IdType { get; set; }
    public string IdNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public bool IsBlacklisted { get; set; }

    /// <summary>Số lượt lưu trú đã hoàn tất.</summary>
    public int CompletedStayCount { get; set; }

    public DateTime? LastStayAt { get; set; }

    /// <summary>Ngưỡng khách quen đọc từ cấu hình (SCR-A12), không viết cứng số 3.</summary>
    public int LoyalThreshold { get; set; }

    public bool IsLoyal => CompletedStayCount >= LoyalThreshold;

    public string MaskedIdNumber => IdNumberMask.Mask(IdNumber);
}

/// <summary>Toàn bộ trạng thái màn hình danh sách khách, gồm cả bộ lọc.</summary>
public class GuestIndexViewModel
{
    public PagedList<GuestListItemViewModel> Results { get; set; } = new();

    [Display(Name = "Tìm theo họ tên, SĐT hoặc số giấy tờ")]
    public string? Keyword { get; set; }

    [Display(Name = "Quốc tịch")]
    public string? Nationality { get; set; }

    [Display(Name = "Danh sách hạn chế")]
    public bool? Blacklisted { get; set; }

    [Display(Name = "Ở gần nhất từ ngày")]
    [DataType(DataType.Date)]
    public DateTime? LastStayFrom { get; set; }

    [Display(Name = "đến ngày")]
    [DataType(DataType.Date)]
    public DateTime? LastStayTo { get; set; }

    public IReadOnlyList<string> NationalityOptions { get; set; } = Array.Empty<string>();

    public bool HasFilter => !string.IsNullOrWhiteSpace(Keyword)
                             || !string.IsNullOrWhiteSpace(Nationality)
                             || Blacklisted is not null
                             || LastStayFrom is not null
                             || LastStayTo is not null;
}

/// <summary>Form thêm / sửa hồ sơ khách — SCR-B02.</summary>
public class GuestFormViewModel
{
    /// <summary>0 = thêm mới.</summary>
    public int Id { get; set; }

    public bool IsEdit => Id != 0;

    [Display(Name = "Họ tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên khách.")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Loại giấy tờ")]
    [Required(ErrorMessage = "Vui lòng chọn loại giấy tờ.")]
    public GuestIdType IdType { get; set; } = GuestIdType.CitizenId;

    [Display(Name = "Số giấy tờ")]
    [Required(ErrorMessage = "Vui lòng nhập số giấy tờ.")]
    [MaxLength(20, ErrorMessage = "Số giấy tờ tối đa 20 ký tự.")]
    public string IdNumber { get; set; } = string.Empty;

    [Display(Name = "Ngày sinh")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Giới tính")]
    public Gender? Gender { get; set; }

    [Display(Name = "Quốc tịch")]
    [Required(ErrorMessage = "Vui lòng nhập quốc tịch.")]
    [MaxLength(50, ErrorMessage = "Quốc tịch tối đa 50 ký tự.")]
    public string Nationality { get; set; } = VietnameseNationality;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^(0|\+84)(2\d{9}|[35789]\d{8})$",
        ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam (VD: 0912345678).")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string? Email { get; set; }

    [Display(Name = "Địa chỉ thường trú")]
    [MaxLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string? Address { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    public string? Notes { get; set; }

    /// <summary>Khách đang lưu trú — sửa số giấy tờ sẽ kèm cảnh báo và audit log.</summary>
    public bool IsStaying { get; set; }

    public const string VietnameseNationality = "Việt Nam";

    public static readonly string[] NationalitySuggestions =
    {
        VietnameseNationality, "Hoa Kỳ", "Hàn Quốc", "Nhật Bản", "Trung Quốc",
        "Anh", "Pháp", "Đức", "Úc", "Nga"
    };
}

/// <summary>Kết quả tra trùng hồ sơ khi đang nhập — SCR-B02, trả về cho JavaScript.</summary>
public class GuestDuplicateCheckResult
{
    /// <summary>Hồ sơ trùng số giấy tờ — chặn cứng.</summary>
    public GuestMatch? IdMatch { get; set; }

    /// <summary>Hồ sơ trùng số điện thoại — chỉ cảnh báo.</summary>
    public List<GuestMatch> PhoneMatches { get; set; } = new();

    public class GuestMatch
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MaskedIdNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

/// <summary>Một dòng lịch sử lưu trú — SCR-B03.</summary>
public class GuestStayHistoryItem
{
    public int StayId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int Nights { get; set; }
    public StayStatus Status { get; set; }
    public bool IsPrimaryGuest { get; set; }

    public string? InvoiceNo { get; set; }
    public decimal? InvoiceTotal { get; set; }
    public InvoiceStatus? InvoiceStatus { get; set; }
}

/// <summary>Một dòng đơn đặt phòng của khách — SCR-B03.</summary>
public class GuestReservationHistoryItem
{
    public int ReservationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }
    public ReservationStatus Status { get; set; }
    public decimal EstimatedTotal { get; set; }
}

/// <summary>Chi tiết khách và lịch sử lưu trú — SCR-B03.</summary>
public class GuestDetailsViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public GuestIdType IdType { get; set; }

    /// <summary>Số giấy tờ **đầy đủ** — màn hình này là một trong hai nơi được phép hiện.</summary>
    public string IdNumber { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public bool IsBlacklisted { get; set; }

    /// <summary>Lý do hạn chế chỉ Admin được xem; lễ tân chỉ thấy nhãn.</summary>
    public string? BlacklistReason { get; set; }

    public int CompletedStayCount { get; set; }
    public int TotalNights { get; set; }
    public decimal TotalSpending { get; set; }
    public DateTime? LastStayAt { get; set; }
    public int LoyalThreshold { get; set; }

    public bool IsLoyal => CompletedStayCount >= LoyalThreshold;
    public bool IsStaying { get; set; }

    public IReadOnlyList<GuestStayHistoryItem> Stays { get; set; } = Array.Empty<GuestStayHistoryItem>();
    public IReadOnlyList<GuestReservationHistoryItem> Reservations { get; set; }
        = Array.Empty<GuestReservationHistoryItem>();
}
