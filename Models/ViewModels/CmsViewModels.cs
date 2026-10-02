using System.ComponentModel.DataAnnotations;
using HotelManagement.Web.Models.Entities;

namespace HotelManagement.Web.Models.ViewModels;

public class PromotionIndexViewModel
{
    public PagedList<Promotion> Results { get; set; } = new();
}

public class PromotionFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên ưu đãi")]
    [Display(Name = "Tên ưu đãi / Gói")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Mô tả chi tiết")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Ảnh Banner (URL)")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Giá hiển thị (nếu có)")]
    public decimal DisplayPrice { get; set; }

    [Display(Name = "Ngày bắt đầu")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Ngày kết thúc")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

    [Display(Name = "Đẩy lên làm Banner nổi bật nhất?")]
    public bool IsHeroOffer { get; set; }
}

public class TestimonialIndexViewModel
{
    public PagedList<Testimonial> Results { get; set; } = new();
}

public class TestimonialFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
    [Display(Name = "Tên khách hàng")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Số sao")]
    public int Rating { get; set; } = 5;

    [Display(Name = "Nguồn (TripAdvisor, Google,...)")]
    public string Source { get; set; } = string.Empty;
}

public class AmenityIndexViewModel
{
    public PagedList<Amenity> Results { get; set; } = new();
}

public class AmenityFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên tiện nghi")]
    [Display(Name = "Tên tiện nghi")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Icon (SVG / Bootstrap Icon class)")]
    public string? IconSvg { get; set; }
}
