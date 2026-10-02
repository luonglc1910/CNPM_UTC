namespace HotelManagement.Web.Models.Responses;

/// <summary>
/// Lớp quy chuẩn dữ liệu trả về chung cho toàn bộ API Backend của hệ thống.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T? data = default, string message = "Thành công")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            ErrorCode = string.Empty
        };
    }

    public static ApiResponse<T> Fail(string message, string errorCode = "ERROR_GENERAL")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Data = default
        };
    }
}

/// <summary>
/// Lớp quy chuẩn dữ liệu trả về không cần payload Data.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Thành công")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Data = null,
            ErrorCode = string.Empty
        };
    }

    public static new ApiResponse Fail(string message, string errorCode = "ERROR_GENERAL")
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Data = null
        };
    }
}
