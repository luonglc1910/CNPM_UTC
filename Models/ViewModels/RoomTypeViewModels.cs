using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Một dòng trên bảng danh sách loại phòng — SCR-A01.</summary>
public class RoomTypeListItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StandardCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public decimal BasePricePerNight { get; set; }
    public decimal ExtraGuestFeePerNight { get; set; }
    public decimal PriceFirstHour { get; set; }
    public decimal PriceExtraHour { get; set; }
    public decimal PriceOvernight { get; set; }

    /// <summary>Số phòng đang hoạt động thuộc loại này.</summary>
    public int ActiveRoomCount { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Chỉ cho ngừng sử dụng khi không còn phòng nào đang hoạt động (SCR-A01).</summary>
    public bool CanDeactivate => IsActive && ActiveRoomCount == 0;
}

/// <summary>Toàn bộ trạng thái màn hình danh sách loại phòng, gồm cả bộ lọc.</summary>
public class RoomTypeIndexViewModel
{
    public PagedList<RoomTypeListItemViewModel> Results { get; set; } = new();

    [Display(Name = "Tìm theo mã hoặc tên")]
    public string? Keyword { get; set; }

    [Display(Name = "Hiện cả loại đã ngừng dùng")]
    public bool IncludeInactive { get; set; }
}

/// <summary>Form thêm / sửa loại phòng — SCR-A02.</summary>
public class RoomTypeFormViewModel
{
    /// <summary>0 = thêm mới.</summary>
    public int Id { get; set; }

    public bool IsEdit => Id != 0;

    [Display(Name = "Mã loại phòng")]
    [Required(ErrorMessage = "Vui lòng nhập mã loại phòng.")]
    [RegularExpression("^[A-Z0-9]{2,10}$",
        ErrorMessage = "Mã loại phòng gồm 2–10 ký tự, chỉ dùng chữ in hoa và số.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Tên loại phòng")]
    [Required(ErrorMessage = "Vui lòng nhập tên loại phòng.")]
    [MaxLength(100, ErrorMessage = "Tên loại phòng tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Sức chứa chuẩn")]
    [Range(1, 10, ErrorMessage = "Sức chứa chuẩn từ 1 đến 10 người.")]
    public int StandardCapacity { get; set; } = 2;

    [Display(Name = "Sức chứa tối đa")]
    [Range(1, 10, ErrorMessage = "Sức chứa tối đa từ 1 đến 10 người.")]
    public int MaxCapacity { get; set; } = 3;

    [Display(Name = "Giá / đêm (₫)")]
    [Range(1, 1_000_000_000, ErrorMessage = "Giá mỗi đêm phải lớn hơn 0.")]
    public decimal BasePricePerNight { get; set; }

    [Display(Name = "Phí thêm người / đêm (₫)")]
    [Range(0, 1_000_000_000, ErrorMessage = "Phí thêm người không được âm.")]
    public decimal ExtraGuestFeePerNight { get; set; }

    [Display(Name = "Phí giường phụ / đêm (₫)")]
    [Range(0, 1_000_000_000, ErrorMessage = "Phí giường phụ không được âm.")]
    public decimal ExtraBedFeePerNight { get; set; }

    [Display(Name = "Giá giờ đầu (₫)")]
    [Range(1, 1_000_000_000, ErrorMessage = "Giá giờ đầu phải lớn hơn 0.")]
    public decimal PriceFirstHour { get; set; }

    [Display(Name = "Giá mỗi giờ tiếp theo (₫)")]
    [Range(1, 1_000_000_000, ErrorMessage = "Giá giờ tiếp theo phải lớn hơn 0.")]
    public decimal PriceExtraHour { get; set; }

    [Display(Name = "Giá qua đêm (₫)")]
    [Range(1, 1_000_000_000, ErrorMessage = "Giá qua đêm phải lớn hơn 0.")]
    public decimal PriceOvernight { get; set; }

    [Display(Name = "Tiện nghi")]
    public List<string> SelectedAmenities { get; set; } = new();

    [Display(Name = "Mô tả")]
    [MaxLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }

    [Display(Name = "Đang sử dụng")]
    public bool IsActive { get; set; } = true;

    /// <summary>Danh sách tiện nghi để dựng các ô chọn.</summary>
    public static readonly string[] AmenityOptions =
    {
        "Điều hòa", "TV", "Tủ lạnh", "Minibar", "Nóng lạnh", "Wifi",
        "Bồn tắm", "Ban công", "Bàn làm việc", "Két sắt"
    };
}
