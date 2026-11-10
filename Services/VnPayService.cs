using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HotelManagement.Web.Services;

/// <summary>Cấu hình cổng VNPay (đọc từ appsettings, mục "VnPay").</summary>
public class VnPayOptions
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string Version { get; set; } = "2.1.0";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(TmnCode) && !string.IsNullOrWhiteSpace(HashSecret);
}

/// <summary>Kết quả VNPay trả về sau khi đã kiểm tra chữ ký.</summary>
public class VnPayResult
{
    public bool SignatureValid { get; set; }
    public bool Success { get; set; }
    public string TxnRef { get; set; } = string.Empty;
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;
    public string TransactionNo { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string TmnCode { get; set; } = string.Empty;
}

public interface IVnPayService
{
    bool IsConfigured { get; }
    string CreatePaymentUrl(int reservationId, decimal amount, string orderInfo, string returnUrl, string ipAddress, string? bankCode = "NCB");
    VnPayResult ParseAndVerify(IQueryCollection query);
}

/// <summary>
/// Tích hợp cổng VNPay (Chức năng 1). Thông tin thẻ do khách nhập trực tiếp trên trang VNPay,
/// hệ thống chỉ ghi nhận thanh toán khi chữ ký HMAC-SHA512 trả về hợp lệ và vnp_ResponseCode = "00".
/// </summary>
public class VnPayService : IVnPayService
{
    private readonly VnPayOptions _options;

    public VnPayService(IConfiguration configuration)
    {
        _options = configuration.GetSection("VnPay").Get<VnPayOptions>() ?? new VnPayOptions();
    }

    public bool IsConfigured => _options.IsConfigured;

    public string CreatePaymentUrl(int reservationId, decimal amount, string orderInfo, string returnUrl, string ipAddress, string? bankCode = "NCB")
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("VNPay Sandbox chưa được cấu hình.");
        if (reservationId <= 0)
            throw new ArgumentOutOfRangeException(nameof(reservationId));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        var now = DateTime.Now;
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _options.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Amount"] = decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero))
                .ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            // Mã giao dịch duy nhất: {mã đơn}_{thời điểm}
            ["vnp_TxnRef"] = $"{reservationId}_{now:yyMMddHHmmssfff}"
        };
        if (!string.IsNullOrWhiteSpace(bankCode))
        {
            data["vnp_BankCode"] = bankCode;
        }

        var query = BuildQuery(data);
        var hash = HmacSha512(_options.HashSecret, query);
        return $"{_options.BaseUrl}?{query}&vnp_SecureHash={hash}";
    }

    public VnPayResult ParseAndVerify(IQueryCollection query)
    {
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in query)
        {
            if (key.StartsWith("vnp_", StringComparison.Ordinal)
                && key != "vnp_SecureHash" && key != "vnp_SecureHashType"
                && !string.IsNullOrEmpty(value))
            {
                data[key] = value.ToString();
            }
        }

        var result = new VnPayResult
        {
            TxnRef = data.GetValueOrDefault("vnp_TxnRef") ?? string.Empty,
            ResponseCode = data.GetValueOrDefault("vnp_ResponseCode") ?? string.Empty,
            TransactionStatus = data.GetValueOrDefault("vnp_TransactionStatus") ?? string.Empty,
            TransactionNo = data.GetValueOrDefault("vnp_TransactionNo") ?? string.Empty,
            BankCode = data.GetValueOrDefault("vnp_BankCode") ?? string.Empty,
            TmnCode = data.GetValueOrDefault("vnp_TmnCode") ?? string.Empty
        };

        if (long.TryParse(data.GetValueOrDefault("vnp_Amount"), out var raw))
        {
            result.Amount = raw / 100m;
        }
        var idPart = result.TxnRef.Split('_')[0];
        if (int.TryParse(idPart, out var rid))
        {
            result.ReservationId = rid;
        }

        if (!_options.IsConfigured)
        {
            return result;
        }

        var expected = HmacSha512(_options.HashSecret, BuildQuery(data));
        var received = query["vnp_SecureHash"].ToString();
        result.SignatureValid = string.Equals(result.TmnCode, _options.TmnCode, StringComparison.Ordinal)
                                && FixedTimeHexEquals(expected, received);

        result.Success = result.SignatureValid
                         && result.ResponseCode == "00"
                         && result.TransactionStatus == "00";
        return result;
    }

    private static bool FixedTimeHexEquals(string expected, string received)
    {
        if (expected.Length != received.Length || expected.Length == 0)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected.ToLowerInvariant()),
            Encoding.ASCII.GetBytes(received.ToLowerInvariant()));
    }

    private static string BuildQuery(SortedDictionary<string, string> data)
        => string.Join("&", data.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

    private static string HmacSha512(string key, string input)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

