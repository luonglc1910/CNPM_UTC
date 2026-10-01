using System.ComponentModel.DataAnnotations;
using HotelManagement.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Tổng hợp một folio theo BR-04 — dùng chung cho SCR-D04/D08/F02/F05.</summary>
public class BillingFolioSummary
{
    public int FolioId { get; set; }
    public string FolioCode { get; set; } = string.Empty;
    public int StayId { get; set; }
    public bool IsLocked { get; set; }

    public decimal RoomCharge { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal DepositApplied { get; set; }
    public decimal BalanceDue { get; set; }

    /// <summary>Chi tiết từng khoản phụ thu — hiển thị dưới dòng "Phụ thu" trên màn check-out.</summary>
    public List<SurchargeLineView> SurchargeLines { get; set; } = new();

    /// <summary>Chi tiết từng dòng tiền phòng — hiển thị dưới "Tiền phòng" khi có nhiều dòng.</summary>
    public List<RoomLineView> RoomLines { get; set; } = new();
}

/// <summary>Một khoản phụ thu cụ thể (nhận sớm, thêm người, trả trễ, quá gói…).</summary>
public class SurchargeLineView
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>True nếu khoản này chưa ghi vào DB — chỉ là dự kiến xem trước lúc check-out.</summary>
    public bool IsPending { get; set; }
}

/// <summary>Một dòng tiền phòng cụ thể trong folio — hiển thị breakdown dưới "Tiền phòng".</summary>
public class RoomLineView
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class FolioLineView
{
    public int Id { get; set; }
    public DateTime ChargedAt { get; set; }
    public FolioItemType ItemType { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public bool IsVoided { get; set; }
    public bool IsRoomCharge => ItemType == FolioItemType.Room;
}

// ---------- SCR-F01 ----------

public class OpenFolioListItem
{
    public int StayId { get; set; }
    public string FolioCode { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime ActualCheckIn { get; set; }
    public DateTime ExpectedCheckOut { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DepositApplied { get; set; }
    public decimal BalanceDue { get; set; }
    public bool IsLocked { get; set; }
}

public class InvoiceListItem
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public InvoiceStatus Status { get; set; }
}

public class BillingIndexViewModel
{
    public const string OpenTab = "open";
    public const string InvoiceTab = "invoice";

    public string Tab { get; set; } = OpenTab;
    public bool IsInvoiceTab => string.Equals(Tab, InvoiceTab, StringComparison.OrdinalIgnoreCase);

    public string? Keyword { get; set; }

    public PagedList<OpenFolioListItem> OpenFolios { get; set; } = new();
    public PagedList<InvoiceListItem> Invoices { get; set; } = new();
}

// ---------- SCR-F02 ----------

public class FolioDetailViewModel
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime ActualCheckIn { get; set; }
    public DateTime ExpectedCheckOut { get; set; }
    public int Nights { get; set; }
    public StayStatus StayStatus { get; set; }

    public BillingFolioSummary Summary { get; set; } = new();
    public IReadOnlyList<FolioLineView> Lines { get; set; } = new List<FolioLineView>();

    public bool CanEdit => StayStatus == StayStatus.CheckedIn && !Summary.IsLocked;
    public int? InvoiceId { get; set; }
}

// ---------- SCR-F03 ----------

public class AddChargeViewModel
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;

    [Display(Name = "Loại chi phí")]
    public FolioItemType ItemType { get; set; } = FolioItemType.Service;

    [Display(Name = "Dịch vụ")]
    public int? HotelServiceId { get; set; }

    [Display(Name = "Số lượng")]
    [Range(1, 10000, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Đơn giá")]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Nội dung")]
    [MaxLength(255)]
    public string? Description { get; set; }

    public IReadOnlyList<SelectListItem> ServiceOptions { get; set; } = new List<SelectListItem>();
}

// ---------- SCR-F04 ----------

public class DiscountViewModel
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public decimal CurrentSubTotal { get; set; }
    public decimal ReceptionistMaxAmount { get; set; }
    public bool IsAdmin { get; set; }

    [Display(Name = "Hình thức")]
    public bool IsPercent { get; set; }

    [Display(Name = "Giá trị")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    public decimal Value { get; set; }

    [Display(Name = "Lý do")]
    [Required(ErrorMessage = "Vui lòng nhập lý do giảm giá.")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

// ---------- SCR-F05 ----------

public class PaymentViewModel
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;

    public BillingFolioSummary Summary { get; set; } = new();

    public bool HasOpenShift { get; set; }
    public bool IsInspected { get; set; }
    public bool IsLocked { get; set; }
    public bool IsRefund => Summary.BalanceDue < 0;
    public bool IsAdmin { get; set; }

    [Display(Name = "Phương thức")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [Display(Name = "Số tiền khách đưa")]
    public decimal AmountTendered { get; set; }

    [Display(Name = "Mã giao dịch")]
    [MaxLength(50)]
    public string? TransactionRef { get; set; }

    [Display(Name = "Ghi nhận công nợ (chỉ Admin)")]
    public bool RecordAsDebt { get; set; }
}

// ---------- SCR-F06 ----------

public class InvoiceViewModel
{
    public Invoice Invoice { get; set; } = null!;
    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;
    public string HotelPhone { get; set; } = string.Empty;
    public string HotelTaxCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? GuestIdNumber { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public int Nights { get; set; }
    public IReadOnlyList<FolioLineView> Lines { get; set; } = new List<FolioLineView>();
    public IReadOnlyList<Payment> Payments { get; set; } = new List<Payment>();
}

// ---------- SCR-F07 ----------

public class VoidInvoiceViewModel
{
    public int InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public bool HasOpenShift { get; set; }

    [Display(Name = "Lý do hủy")]
    [Required(ErrorMessage = "Vui lòng nhập lý do hủy hóa đơn.")]
    [MinLength(10, ErrorMessage = "Lý do phải từ 10 ký tự.")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Tôi hiểu thao tác này không thể hoàn tác")]
    public bool Confirmed { get; set; }
}

// ---------- E02 kiểm minibar ----------

public class MinibarLineInput
{
    public int HotelServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int Quantity { get; set; }
}

public class MinibarViewModel
{
    public int StayId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public bool AlreadyInspected { get; set; }
    public List<MinibarLineInput> Lines { get; set; } = new();
}
