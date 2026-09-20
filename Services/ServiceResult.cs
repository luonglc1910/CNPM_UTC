namespace HotelManagement.Web.Services;

/// <summary>
/// Kết quả một thao tác nghiệp vụ. Dùng thay cho việc ném exception cho các lỗi nghiệp vụ
/// thông thường (trùng mã, vi phạm ràng buộc) — exception chỉ dành cho lỗi thật sự bất thường.
///
/// <see cref="Warning"/> khác <see cref="Error"/>: thao tác **đã thành công** nhưng có điều
/// người dùng cần biết, ví dụ giảm sức chứa xuống dưới số khách đang thực ở.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; private init; }

    /// <summary>Lý do thất bại — hiển thị ở vùng lỗi của form.</summary>
    public string? Error { get; private init; }

    /// <summary>Tên trường gây lỗi, để gắn thông báo đúng ô nhập. Rỗng = lỗi chung của form.</summary>
    public string? ErrorField { get; private init; }

    /// <summary>Cảnh báo kèm theo khi thành công.</summary>
    public string? Warning { get; private init; }

    public static ServiceResult Ok(string? warning = null)
        => new() { Succeeded = true, Warning = warning };

    public static ServiceResult Fail(string error, string? field = null)
        => new() { Succeeded = false, Error = error, ErrorField = field };
}
