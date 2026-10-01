using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Một dòng trên bảng danh sách nhân viên — SCR-A10.</summary>
public class EmployeeListItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public EmployeeStatus Status { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Chính người đang đăng nhập — không tự khóa, không tự hạ quyền được.</summary>
    public bool IsSelf { get; set; }

    /// <summary>Còn ca thu ngân chưa đóng — chặn cho nghỉ việc (BR-10).</summary>
    public bool HasOpenShift { get; set; }

    public bool CanLock => !IsSelf && Status != EmployeeStatus.Resigned;
    public bool IsLocked => Status == EmployeeStatus.Locked;
}

/// <summary>Toàn bộ trạng thái màn hình danh sách nhân viên, gồm cả bộ lọc.</summary>
public class EmployeeIndexViewModel
{
    public PagedList<EmployeeListItemViewModel> Results { get; set; } = new();

    [Display(Name = "Tìm theo mã, tên, SĐT hoặc tên đăng nhập")]
    public string? Keyword { get; set; }

    [Display(Name = "Vai trò")]
    public EmployeeRole? Role { get; set; }

    [Display(Name = "Trạng thái")]
    public EmployeeStatus? Status { get; set; }

    [Display(Name = "Hiện cả nhân viên đã nghỉ")]
    public bool IncludeResigned { get; set; }

    public bool HasFilter => !string.IsNullOrWhiteSpace(Keyword)
                             || Role is not null
                             || Status is not null
                             || IncludeResigned;
}

/// <summary>Form thêm / sửa nhân viên — SCR-A11.</summary>
public class EmployeeFormViewModel
{
    /// <summary>0 = thêm mới.</summary>
    public int Id { get; set; }

    public bool IsEdit => Id != 0;

    /// <summary>Mã nhân viên do hệ thống sinh, chỉ để hiển thị khi sửa.</summary>
    public string? Code { get; set; }

    [Display(Name = "Họ tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^(0|\+84)(2\d{9}|[35789]\d{8})$",
        ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam (VD: 0912345678).")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string? Email { get; set; }

    [Display(Name = "CCCD")]
    [RegularExpression(@"^\d{9,12}$", ErrorMessage = "CCCD gồm 9–12 chữ số.")]
    public string? IdNumber { get; set; }

    [Display(Name = "Vai trò")]
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public EmployeeRole Role { get; set; } = EmployeeRole.Receptionist;

    [Display(Name = "Tên đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [RegularExpression("^[a-z0-9._]{3,50}$",
        ErrorMessage = "Tên đăng nhập gồm 3–50 ký tự thường, không dấu, không khoảng trắng.")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu ban đầu")]
    [MinLength(8, ErrorMessage = "Mật khẩu ban đầu phải có ít nhất 8 ký tự.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có cả chữ và số.")]
    [DataType(DataType.Password)]
    public string? InitialPassword { get; set; }

    [Display(Name = "Trạng thái")]
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>Tài khoản đang bị khóa: trạng thái do nút Khóa/Mở khóa quản, form không đụng tới.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Đang sửa chính mình — khóa ô vai trò và ô trạng thái.</summary>
    public bool IsSelf { get; set; }

    /// <summary>Còn ca thu ngân đang mở — không cho chuyển sang "Đã nghỉ" (BR-10).</summary>
    public bool HasOpenShift { get; set; }
}
