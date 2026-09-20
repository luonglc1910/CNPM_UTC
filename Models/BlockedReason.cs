namespace HotelManagement.Web.Models;

/// <summary>
/// Soạn câu giải thích vì sao không mở được một màn hình gắn với đơn đặt phòng hoặc
/// lượt lưu trú — 00-conventions.md mục 4.
///
/// Trước đây các action GET trả thẳng <c>NotFound()</c> khi service trả null. Người dùng
/// nhận trang 404 trắng và tưởng đường dẫn hỏng, trong khi bản ghi vẫn tồn tại và chỉ đang
/// ở trạng thái không cho thao tác đó. Lớp này đổi trạng thái thành câu nói được lý do thật.
/// </summary>
public static class BlockedReason
{
    /// <param name="status">Trạng thái đơn; <c>null</c> nghĩa là không tìm thấy đơn.</param>
    /// <param name="action">Việc đang muốn làm, ví dụ "check-in", "thu cọc".</param>
    public static string Reservation(ReservationStatus? status, string action, string? code = null)
    {
        var name = code is null ? "Đơn đặt phòng" : $"Đơn {code}";

        return status switch
        {
            null => "Không tìm thấy đơn đặt phòng.",
            ReservationStatus.Draft =>
                $"{name} còn ở trạng thái Nháp — cần xác nhận đơn trước khi {action}.",
            ReservationStatus.CheckedIn =>
                $"{name} đã nhận phòng rồi, không {action} được nữa.",
            ReservationStatus.CheckedOut =>
                $"{name} đã trả phòng, không {action} được nữa.",
            ReservationStatus.Cancelled =>
                $"{name} đã hủy, không {action} được.",
            ReservationStatus.NoShow =>
                $"{name} đã bị đánh dấu không đến, không {action} được.",
            _ => $"{name} đang ở trạng thái không cho phép {action}."
        };
    }

    /// <param name="status">Trạng thái lượt lưu trú; <c>null</c> nghĩa là không tìm thấy.</param>
    public static string Stay(StayStatus? status, string action) => status switch
    {
        null => "Không tìm thấy lượt lưu trú.",
        StayStatus.CheckedOut =>
            $"Lượt lưu trú này đã trả phòng, không {action} được nữa.",
        _ => $"Lượt lưu trú đang ở trạng thái không cho phép {action}."
    };

    /// <param name="status">Trạng thái hóa đơn; <c>null</c> nghĩa là không tìm thấy.</param>
    public static string Invoice(InvoiceStatus? status, string action) => status switch
    {
        null => "Không tìm thấy hóa đơn.",
        InvoiceStatus.Void => $"Hóa đơn đã bị hủy, không {action} được nữa.",
        _ => $"Hóa đơn đang ở trạng thái không cho phép {action}."
    };
}
