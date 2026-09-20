using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Một dòng / một ô trên màn hình danh sách phòng — SCR-A03.</summary>
public class RoomListItemViewModel
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public RoomStatus Status { get; set; }
    public string? StatusNote { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Khách đang lưu trú, nếu phòng đang có người.</summary>
    public string? CurrentGuestName { get; set; }
    public int? CurrentStayId { get; set; }
}

/// <summary>Trạng thái màn hình danh sách phòng, gồm bộ lọc và chế độ hiển thị.</summary>
public class RoomIndexViewModel
{
    public PagedList<RoomListItemViewModel> Results { get; set; } = new();

    /// <summary>Dạng lưới theo tầng dùng danh sách đầy đủ, không phân trang.</summary>
    public IReadOnlyList<RoomListItemViewModel> AllForGrid { get; set; } = Array.Empty<RoomListItemViewModel>();

    [Display(Name = "Số phòng")]
    public string? Keyword { get; set; }

    [Display(Name = "Tầng")]
    public int? Floor { get; set; }

    [Display(Name = "Loại phòng")]
    public int? RoomTypeId { get; set; }

    [Display(Name = "Trạng thái")]
    public RoomStatus? Status { get; set; }

    /// <summary>"table" hoặc "grid".</summary>
    public string View { get; set; } = "table";

    public bool IsGrid => string.Equals(View, "grid", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<SelectListItem> FloorOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> RoomTypeOptions { get; set; } = Array.Empty<SelectListItem>();

    public bool HasFilter =>
        !string.IsNullOrWhiteSpace(Keyword) || Floor is not null || RoomTypeId is not null || Status is not null;
}

/// <summary>Form thêm / sửa phòng — SCR-A04.</summary>
public class RoomFormViewModel
{
    public int Id { get; set; }

    public bool IsEdit => Id != 0;

    [Display(Name = "Số phòng")]
    [Required(ErrorMessage = "Vui lòng nhập số phòng.")]
    [MaxLength(10, ErrorMessage = "Số phòng tối đa 10 ký tự.")]
    public string RoomNumber { get; set; } = string.Empty;

    [Display(Name = "Tầng")]
    [Range(0, 200, ErrorMessage = "Tầng phải là số từ 0 trở lên.")]
    public int Floor { get; set; }

    [Display(Name = "Loại phòng")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại phòng.")]
    public int RoomTypeId { get; set; }

    [Display(Name = "Trạng thái ban đầu")]
    public RoomStatus Status { get; set; } = RoomStatus.Available;

    [Display(Name = "Ghi chú")]
    [MaxLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Notes { get; set; }

    /// <summary>Xác nhận của Admin khi đổi loại phòng mà phòng đã có đơn đặt trong tương lai.</summary>
    public bool ConfirmRoomTypeChange { get; set; }

    /// <summary>Danh sách đơn bị ảnh hưởng, hiện ra để Admin cân nhắc trước khi xác nhận.</summary>
    public IReadOnlyList<string> AffectedReservations { get; set; } = Array.Empty<string>();

    public IReadOnlyList<SelectListItem> RoomTypeOptions { get; set; } = Array.Empty<SelectListItem>();
}

/// <summary>Hộp thoại đổi trạng thái phòng — SCR-A05.</summary>
public class RoomStatusFormViewModel
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomStatus CurrentStatus { get; set; }

    [Display(Name = "Trạng thái mới")]
    public RoomStatus NewStatus { get; set; }

    [Display(Name = "Lý do")]
    [MaxLength(255, ErrorMessage = "Lý do tối đa 255 ký tự.")]
    public string? Reason { get; set; }

    /// <summary>Chỉ những trạng thái hợp lệ từ trạng thái hiện tại.</summary>
    public IReadOnlyList<SelectListItem> AllowedStatuses { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>Cảnh báo phòng còn đơn đặt trong tương lai khi chuyển sang Bảo trì / Ngừng khai thác.</summary>
    public IReadOnlyList<string> AffectedReservations { get; set; } = Array.Empty<string>();
}
