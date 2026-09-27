using System.ComponentModel.DataAnnotations;
using HotelManagement.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Web.Models.ViewModels;

// ---------- SCR-E01: bảng trạng thái buồng phòng ----------

public class BoardRoom
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public RoomStatus Status { get; set; }
    public string? StatusNote { get; set; }

    /// <summary>Đang có tác vụ dọn ở trạng thái InProgress.</summary>
    public bool IsBeingCleaned { get; set; }
    public DateTime? CleaningStartedAt { get; set; }

    /// <summary>Chờ dọn kể từ (xấp xỉ theo lần cập nhật phòng gần nhất).</summary>
    public DateTime? DirtySince { get; set; }

    public int OpenRequestCount { get; set; }

    /// <summary>Phòng chờ dọn mà có khách sẽ nhận trong hôm nay — cần dọn gấp.</summary>
    public bool IsUrgentArrival { get; set; }
}

public class HousekeepingFloorGroup
{
    public int Floor { get; set; }
    public IReadOnlyList<BoardRoom> Rooms { get; set; } = new List<BoardRoom>();
}

public class HousekeepingBoardViewModel
{
    [Display(Name = "Tầng")]
    public int? Floor { get; set; }

    [Display(Name = "Trạng thái")]
    public RoomStatus? Status { get; set; }

    [Display(Name = "Chỉ hiện phòng cần xử lý")]
    public bool OnlyActionable { get; set; } = true;

    public IReadOnlyList<HousekeepingFloorGroup> Floors { get; set; } = new List<HousekeepingFloorGroup>();
    public IReadOnlyList<int> FloorOptions { get; set; } = new List<int>();
}

// ---------- SCR-E03: tạo yêu cầu ----------

public class CreateRequestViewModel
{
    [Display(Name = "Loại yêu cầu")]
    public RequestType Type { get; set; } = RequestType.Maintenance;

    [Display(Name = "Phòng")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng.")]
    public int RoomId { get; set; }

    [Display(Name = "Mô tả")]
    [Required(ErrorMessage = "Vui lòng nhập mô tả.")]
    [MinLength(10, ErrorMessage = "Mô tả cần ít nhất 10 ký tự.")]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Mức độ")]
    public RequestPriority Priority { get; set; } = RequestPriority.Medium;

    [Display(Name = "Người xử lý")]
    public int? AssignedTo { get; set; }

    [Display(Name = "Chuyển phòng sang Bảo trì")]
    public bool MakeRoomMaintenance { get; set; } = true;

    public IReadOnlyList<SelectListItem> RoomOptions { get; set; } = new List<SelectListItem>();
    public IReadOnlyList<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();
}

// ---------- SCR-E04: danh sách yêu cầu ----------

public class RequestListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public RequestType Type { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RequestPriority Priority { get; set; }
    public RequestStatus Status { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? AssignedName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Yêu cầu bảo trì và phòng đang ở trạng thái Bảo trì — cho phép "mở bán lại".</summary>
    public bool RoomIsUnderMaintenance { get; set; }
}

public class RequestListViewModel
{
    [Display(Name = "Loại")]
    public RequestType? Type { get; set; }

    [Display(Name = "Trạng thái")]
    public RequestStatus? Status { get; set; }

    [Display(Name = "Mức độ")]
    public RequestPriority? Priority { get; set; }

    /// <summary>Người dùng đã chủ động lọc; mặc định chỉ hiện yêu cầu chưa hoàn thành.</summary>
    public bool CustomFilter { get; set; }

    public PagedList<RequestListItem> Results { get; set; } = new();
    public IReadOnlyList<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();

    public bool HasFilter => Type is not null || Status is not null || Priority is not null;
}
