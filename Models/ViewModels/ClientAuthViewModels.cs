using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Form đăng ký tài khoản khách hàng trên Portal.</summary>
public class ClientRegisterViewModel
{
    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email (Tài khoản đăng nhập)")]
    [Required(ErrorMessage = "Vui lòng nhập Email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^(0|\+84)(2\d{9}|[35789]\d{8})$",
        ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam (VD: 0912345678).")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
        ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái và một chữ số.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Xác nhận mật khẩu")]
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Form đăng nhập tài khoản khách hàng trên Portal.</summary>
public class ClientLoginViewModel
{
    [Display(Name = "Email hoặc Tên đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập Email hoặc Tên đăng nhập.")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
}
