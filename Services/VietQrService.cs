using HotelManagement.Web.Models.Entities;

namespace HotelManagement.Web.Services;

public class VietQrPayload
{
    public string BankBin { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string IssueDate { get; set; } = "07/15";
    public string DefaultOtp { get; set; } = "123456";
    public decimal Amount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string TransferMemo => Memo;
    public string QrImageUrl { get; set; } = string.Empty;
    public string SuggestedTxRef { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public interface IVietQrService
{
    Task<VietQrPayload> BuildQrAsync(decimal amount, string memo, string? title = null);
    Task<VietQrPayload> BuildAsync(decimal amount, string memo, string? title = null);
}

public class VietQrService : IVietQrService
{
    private readonly ISettingsReader _settings;

    public VietQrService(ISettingsReader settings)
    {
        _settings = settings;
    }

    public Task<VietQrPayload> BuildAsync(decimal amount, string memo, string? title = null)
        => BuildQrAsync(amount, memo, title);

    public async Task<VietQrPayload> BuildQrAsync(decimal amount, string memo, string? title = null)
    {
        var bankBin = (await _settings.GetStringAsync(SystemSettingKeys.BankBin)).Trim();
        var bankName = (await _settings.GetStringAsync(SystemSettingKeys.BankName)).Trim();
        var accountNo = (await _settings.GetStringAsync(SystemSettingKeys.BankAccountNo)).Trim();
        var accountName = (await _settings.GetStringAsync(SystemSettingKeys.BankAccountName)).Trim();

        if (string.IsNullOrWhiteSpace(bankBin)) bankBin = "970419";
        if (string.IsNullOrWhiteSpace(bankName)) bankName = "Ngân hàng NCB";
        if (string.IsNullOrWhiteSpace(accountNo)) accountNo = "9704198526191432198";
        if (string.IsNullOrWhiteSpace(accountName)) accountName = "NGUYEN VAN A";

        var cleanMemo = SanitizeMemo(memo);
        var roundedAmount = (long)Math.Max(0, Math.Round(amount));

        var qrUrl = $"https://img.vietqr.io/image/{bankBin}-{accountNo}-compact2.png" +
                    $"?amount={roundedAmount}" +
                    $"&addInfo={Uri.EscapeDataString(cleanMemo)}" +
                    $"&accountName={Uri.EscapeDataString(accountName)}";

        return new VietQrPayload
        {
            BankBin = bankBin,
            BankName = bankName,
            AccountNo = accountNo,
            AccountName = accountName,
            IssueDate = "07/15",
            DefaultOtp = "123456",
            Amount = roundedAmount,
            Memo = cleanMemo,
            QrImageUrl = qrUrl,
            SuggestedTxRef = $"NCB-{DateTime.Now:yyMMddHHmm}-{Random.Shared.Next(100, 999)}",
            Title = title
        };
    }

    private static string SanitizeMemo(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "AURA PAYMENT";
        }

        var chars = input
            .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-')
            .Select(c => c == '-' ? ' ' : c)
            .ToArray();

        var result = new string(chars).Trim();
        return result.Length > 45 ? result[..45] : result;
    }
}
