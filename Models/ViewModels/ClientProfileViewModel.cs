using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.ViewModels;

public class ClientProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Số CCCD/Hộ chiếu")]
    public string? IdNumber { get; set; }

    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Giới tính")]
    public Gender? Gender { get; set; }

    [Display(Name = "Địa chỉ thường trú")]
    public string? Address { get; set; }
}
