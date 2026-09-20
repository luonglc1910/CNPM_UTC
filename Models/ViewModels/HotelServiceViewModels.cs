using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Một dòng trên bảng danh sách dịch vụ — SCR-A06.</summary>
public class HotelServiceListItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ServiceCategory Category { get; set; }
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsStockManaged { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Dòng tồn dưới định mức được tô nền vàng kèm biểu tượng cảnh báo (SCR-A06).</summary>
    public bool IsLowStock => IsStockManaged && StockQuantity <= MinStockLevel;
}

/// <summary>Toàn bộ trạng thái màn hình danh sách dịch vụ, gồm cả bộ lọc.</summary>
public class HotelServiceIndexViewModel
{
    public PagedList<HotelServiceListItemViewModel> Results { get; set; } = new();

    [Display(Name = "Tìm theo mã hoặc tên")]
    public string? Keyword { get; set; }

    [Display(Name = "Nhóm dịch vụ")]
    public ServiceCategory? Category { get; set; }

    [Display(Name = "Hiện cả dịch vụ đã ngừng")]
    public bool IncludeInactive { get; set; }

    [Display(Name = "Chỉ dịch vụ dưới định mức tồn")]
    public bool LowStockOnly { get; set; }

    /// <summary>Số dòng dưới định mức trong toàn bộ danh mục, không chỉ trang hiện tại.</summary>
    public int LowStockCount { get; set; }

    public bool HasFilter => !string.IsNullOrWhiteSpace(Keyword)
                             || Category is not null
                             || IncludeInactive
                             || LowStockOnly;
}

/// <summary>Form thêm / sửa dịch vụ — SCR-A07.</summary>
public class HotelServiceFormViewModel
{
    /// <summary>0 = thêm mới.</summary>
    public int Id { get; set; }

    public bool IsEdit => Id != 0;

    [Display(Name = "Mã dịch vụ")]
    [Required(ErrorMessage = "Vui lòng nhập mã dịch vụ.")]
    [RegularExpression("^[A-Z0-9]{2,20}$",
        ErrorMessage = "Mã dịch vụ gồm 2–20 ký tự, chỉ dùng chữ in hoa và số.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Tên dịch vụ")]
    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
    [MaxLength(100, ErrorMessage = "Tên dịch vụ tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Nhóm dịch vụ")]
    [Required(ErrorMessage = "Vui lòng chọn nhóm dịch vụ.")]
    public ServiceCategory Category { get; set; } = ServiceCategory.Other;

    [Display(Name = "Đơn giá (₫)")]
    [Range(1, 1_000_000_000, ErrorMessage = "Đơn giá phải lớn hơn 0.")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Đơn vị tính")]
    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính.")]
    [MaxLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]
    public string Unit { get; set; } = string.Empty;

    [Display(Name = "Có quản lý kho")]
    public bool IsStockManaged { get; set; }

    [Display(Name = "Tồn tối thiểu (cảnh báo)")]
    [Range(0, 1_000_000, ErrorMessage = "Tồn tối thiểu không được âm.")]
    public int MinStockLevel { get; set; }

    [Display(Name = "Cho phép bán khi hết hàng")]
    public bool AllowNegativeStock { get; set; }

    [Display(Name = "Đang sử dụng")]
    public bool IsActive { get; set; } = true;

    /// <summary>Tồn hiện tại — chỉ để hiển thị; đổi tồn phải đi qua SCR-A09.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Dịch vụ đã từng được ghi vào folio thì không đổi được mã và không xóa cứng.</summary>
    public bool HasFolioHistory { get; set; }

    /// <summary>Gợi ý đơn vị tính thường dùng, đổ vào datalist cho nhanh tay.</summary>
    public static readonly string[] UnitSuggestions =
    {
        "lon", "chai", "gói", "suất", "kg", "lượt", "giờ", "ngày", "cái"
    };
}
