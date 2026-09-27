using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Báo cáo & quản trị — nhóm G. Chỉ Admin (ma trận phân quyền: nhóm G không mở cho lễ tân).
//
// Bốn màn hình dùng chung một mục menu "Báo cáo" và một thanh điều hướng con
// (_ReportNav): Doanh thu & công suất · Dịch vụ & tồn kho · Nhân viên / ca · Nhật ký.
[Authorize(Roles = Roles.Admin)]
public class ReportsController : AdminControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    // SCR-G01 + SCR-G02 — doanh thu và công suất dùng chung một khoảng ngày.
    public async Task<IActionResult> Index(BusinessReportViewModel filter)
    {
        // Hai báo cáo nhận cùng From/To để hai tab luôn nói về một kỳ. Người xem đổi
        // khoảng ngày một lần là cả hai tab đổi theo, không phải chỉnh hai chỗ.
        filter.Revenue = await _service.BuildRevenueAsync(new RevenueReportViewModel
        {
            From = filter.From,
            To = filter.To,
            Period = filter.Period
        });

        filter.Occupancy = await _service.BuildOccupancyAsync(new OccupancyReportViewModel
        {
            From = filter.Revenue.From,
            To = filter.Revenue.To
        });

        // BuildRevenueAsync tự điền mặc định khi khoảng ngày để trống; lấy lại để ô lọc
        // trên giao diện hiện đúng giá trị đang áp dụng.
        filter.From = filter.Revenue.From;
        filter.To = filter.Revenue.To;

        return View(filter);
    }

    // Hai URL cũ của SCR-G01 và SCR-G02 trước khi gộp. Giữ lại để link đã lưu không gãy.
    public IActionResult Revenue() => RedirectToAction(nameof(Index));

    public IActionResult Occupancy() => RedirectToAction(nameof(Index), new { tab = "occupancy" });

    // SCR-G03 — dịch vụ bán chạy và đối chiếu tồn kho.
    public async Task<IActionResult> Services(ServicesReportViewModel filter)
        => View(await _service.BuildServicesAsync(filter));

    // SCR-G04 — doanh thu và chênh lệch quỹ theo ca, thao tác theo nhân viên.
    public async Task<IActionResult> Staff(StaffReportViewModel filter)
        => View(await _service.BuildStaffAsync(filter));

    // SCR-G05 — nhật ký thao tác. Chỉ đọc: bảng AuditLog chỉ ghi thêm, toàn ứng dụng
    // không có chỗ nào sửa hay xóa (NFR-07).
    public async Task<IActionResult> AuditLog(AuditLogReportViewModel filter, int page = 1)
        => View(await _service.BuildAuditLogAsync(filter, page));
}
