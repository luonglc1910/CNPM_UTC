using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Thu ngân: bảng kê tạm tính (folio), phụ phí, thanh toán, xuất hóa đơn.
[Authorize(Roles = Roles.All)]
public class BillingController : AdminControllerBase
{
    // Danh sách folio đang mở và hóa đơn đã xuất.
    public IActionResult Index() => View();

    public IActionResult Folio(int stayId) => View();

    [HttpGet]
    public IActionResult AddCharge(int stayId) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(AddCharge))]
    public IActionResult AddChargePost(int stayId) => Pending(nameof(Folio), new { stayId });

    // Thanh toán: tiền mặt, quẹt thẻ, chuyển khoản.
    [HttpGet]
    public IActionResult Payment(int stayId) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Payment))]
    public IActionResult PaymentPost(int stayId) => Pending(nameof(Folio), new { stayId });

    // Chốt sổ và xuất hóa đơn cuối cùng.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GenerateInvoice(int stayId) => Pending(nameof(Folio), new { stayId });
}
