using HotelManagement.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Chạy một khối nghiệp vụ nhiều bước trong đúng một database transaction — NFR-03, REQUIREMENTS mục 7.
///
/// Dùng cho check-in, check-out, thanh toán, đổi phòng, hủy đơn: hoặc mọi thay đổi (kể cả dòng
/// audit log và số chứng từ vừa cấp) cùng được ghi, hoặc không có gì được ghi. Bọc trong
/// execution strategy để tương thích với <c>EnableRetryOnFailure</c> — nếu không, EF Core cấm
/// mở transaction thủ công khi bật retry.
/// </summary>
public interface ITransactionRunner
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action);
    Task ExecuteAsync(Func<Task> action);
}

/// <inheritdoc />
public class TransactionRunner : ITransactionRunner
{
    private readonly HotelDbContext _db;

    public TransactionRunner(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            var result = await action();
            await tx.CommitAsync();
            return result;
        });
    }

    public async Task ExecuteAsync(Func<Task> action)
        => await ExecuteAsync(async () =>
        {
            await action();
            return true;
        });
}
