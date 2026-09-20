using System.Globalization;
using HotelManagement.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Cấp số chứng từ liên tục, không trùng, an toàn khi nhiều người thao tác cùng lúc —
/// FR-C04 (mã đơn), FR-F05 (số hóa đơn), mã folio.
///
/// Mỗi cặp (tiền tố, kỳ) là một dòng trong bảng NumberSequences. Câu UPDATE tăng số giữ khóa
/// độc quyền trên dòng đó tới khi transaction commit, nên hai giao dịch xin số cùng lúc bị tuần
/// tự hóa — không ai nhận trùng. Phải gọi **bên trong** transaction nghiệp vụ (qua
/// <see cref="ITransactionRunner"/>) để nếu nghiệp vụ rollback thì số cũng trả lại, giữ dãy liên
/// tục. Hóa đơn bị hủy (Void) giữ nguyên số đã cấp — không cấp lại số đó cho hóa đơn khác.
/// </summary>
public interface INumberSequenceService
{
    /// <summary>Số kế tiếp của một cặp (tiền tố, kỳ). Kỳ rỗng nghĩa là đánh số liên tục toàn cục.</summary>
    Task<long> NextValueAsync(string prefix, string period);

    /// <summary>Số hóa đơn dạng HD-yyyyMM-##### — FR-F05.</summary>
    Task<string> NextInvoiceNoAsync(DateTime issuedAt);

    /// <summary>Mã đơn đặt phòng dạng RSV-yyMMdd-#### — FR-C04.</summary>
    Task<string> NextReservationCodeAsync(DateTime createdAt);

    /// <summary>Mã folio dạng F-###### — đánh số liên tục toàn cục.</summary>
    Task<string> NextFolioCodeAsync();
}

/// <inheritdoc />
public class NumberSequenceService : INumberSequenceService
{
    private const string TableName = "NumberSequences";

    private readonly HotelDbContext _db;

    public NumberSequenceService(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<long> NextValueAsync(string prefix, string period)
    {
        // Tăng dòng đã có; câu UPDATE khóa dòng tới khi commit nên tuần tự hóa người xin số cùng lúc.
        var affected = await _db.Database.ExecuteSqlRawAsync(
            $"UPDATE {TableName} SET LastValue = LastValue + 1 WHERE Prefix = {{0}} AND Period = {{1}}",
            prefix, period);

        if (affected == 0)
        {
            // Chưa có dòng cho cặp này — tạo mới với giá trị 1. Nếu bị đua chèn, unique index
            // (Prefix, Period) chặn; khi đó quay lại tăng dòng người kia vừa tạo.
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    $"INSERT INTO {TableName} (Prefix, Period, LastValue) VALUES ({{0}}, {{1}}, 1)",
                    prefix, period);
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                await _db.Database.ExecuteSqlRawAsync(
                    $"UPDATE {TableName} SET LastValue = LastValue + 1 WHERE Prefix = {{0}} AND Period = {{1}}",
                    prefix, period);
            }
        }

        // Đọc lại trong cùng transaction: dòng đang bị chính giao dịch này khóa nên trả đúng giá trị vừa cấp.
        var rows = await _db.Database.SqlQueryRaw<long>(
                $"SELECT LastValue AS [Value] FROM {TableName} WHERE Prefix = {{0}} AND Period = {{1}}",
                prefix, period)
            .ToListAsync();

        return rows.Count > 0 ? rows[0] : 1;
    }

    public async Task<string> NextInvoiceNoAsync(DateTime issuedAt)
    {
        var period = issuedAt.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var value = await NextValueAsync("HD", period);
        return $"HD-{period}-{value:D5}";
    }

    public async Task<string> NextReservationCodeAsync(DateTime createdAt)
    {
        var period = createdAt.ToString("yyMMdd", CultureInfo.InvariantCulture);
        var value = await NextValueAsync("RSV", period);
        return $"RSV-{period}-{value:D4}";
    }

    public async Task<string> NextFolioCodeAsync()
    {
        var value = await NextValueAsync("F", string.Empty);
        return $"F-{value:D6}";
    }
}
