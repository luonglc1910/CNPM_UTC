using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>
/// Một trang dữ liệu cho bảng danh sách — mặc định 20 dòng/trang
/// (docs/screens/00-conventions.md mục 5).
/// </summary>
public class PagedList<T>
{
    public const int DefaultPageSize = 20;

    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public int TotalItems { get; init; }

    public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> query, int page, int pageSize = DefaultPageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        var total = await query.CountAsync();

        // Xóa bớt dòng khiến trang hiện tại vượt quá số trang thì lùi về trang cuối.
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages)
        {
            page = totalPages;
        }

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedList<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
