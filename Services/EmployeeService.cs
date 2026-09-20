using System.Security.Cryptography;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Nghiệp vụ tài khoản nhân viên — SCR-A10, SCR-A11, FR-A06, FR-A08, BR-10, BR-11.
///
/// Ba luật tự bảo vệ hệ thống, đều phải kiểm ở đây chứ không diễn đạt được bằng attribute:
/// không tự khóa / tự hạ quyền chính mình · luôn còn ít nhất một Admin đang hoạt động ·
/// không cho nghỉ việc khi còn ca thu ngân chưa đóng.
/// </summary>
public interface IEmployeeService
{
    Task<EmployeeIndexViewModel> SearchAsync(EmployeeIndexViewModel filter, int page);
    Task<EmployeeFormViewModel?> GetForEditAsync(int id);
    Task<ServiceResult> CreateAsync(EmployeeFormViewModel form);
    Task<ServiceResult> UpdateAsync(EmployeeFormViewModel form);

    /// <summary>Khóa hoặc mở khóa tài khoản — SCR-A10.</summary>
    Task<ServiceResult> ToggleLockAsync(int id);

    /// <summary>Sinh mật khẩu tạm và buộc đổi ở lần đăng nhập kế tiếp — FR-A08.</summary>
    Task<ServiceResult> ResetPasswordAsync(int id);
}

/// <inheritdoc />
public class EmployeeService : IEmployeeService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployeeService(HotelDbContext db, IAuditService audit, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? CurrentEmployeeId => _httpContextAccessor.HttpContext?.User.GetEmployeeId();

    // ---------- SCR-A10: danh sách ----------

    public async Task<EmployeeIndexViewModel> SearchAsync(EmployeeIndexViewModel filter, int page)
    {
        var query = _db.Employees.AsNoTracking();

        // Nhân viên đã nghỉ vẫn nằm trong DB vĩnh viễn (không xóa cứng) nên mặc định ẩn đi
        // cho danh sách gọn; lọc theo trạng thái thì tôn trọng đúng lựa chọn đó.
        if (!filter.IncludeResigned && filter.Status is null)
        {
            query = query.Where(e => e.Status != EmployeeStatus.Resigned);
        }

        if (filter.Role is not null)
        {
            query = query.Where(e => e.Role == filter.Role);
        }

        if (filter.Status is not null)
        {
            query = query.Where(e => e.Status == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim();
            query = query.Where(e => e.Code.Contains(k)
                                     || e.FullName.Contains(k)
                                     || e.PhoneNumber.Contains(k)
                                     || e.UserName.Contains(k));
        }

        var currentId = CurrentEmployeeId;

        var projected = query
            .OrderBy(e => e.Code)
            .Select(e => new EmployeeListItemViewModel
            {
                Id = e.Id,
                Code = e.Code,
                FullName = e.FullName,
                Role = e.Role,
                PhoneNumber = e.PhoneNumber,
                UserName = e.UserName,
                Status = e.Status,
                LastLoginAt = e.LastLoginAt,
                IsSelf = e.Id == currentId,
                HasOpenShift = e.Shifts.Any(s => s.Status == ShiftStatus.Open)
            });

        filter.Results = await PagedList<EmployeeListItemViewModel>.CreateAsync(projected, page);
        return filter;
    }

    // ---------- SCR-A11: thêm / sửa ----------

    public async Task<EmployeeFormViewModel?> GetForEditAsync(int id)
    {
        var entity = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
        {
            return null;
        }

        return new EmployeeFormViewModel
        {
            Id = entity.Id,
            Code = entity.Code,
            FullName = entity.FullName,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            IdNumber = entity.IdNumber,
            Role = entity.Role,
            UserName = entity.UserName,
            Status = entity.Status,
            IsLocked = entity.Status == EmployeeStatus.Locked,
            IsSelf = entity.Id == CurrentEmployeeId,
            HasOpenShift = await _db.CashierShifts
                .AnyAsync(s => s.EmployeeId == id && s.Status == ShiftStatus.Open)
        };
    }

    public async Task<ServiceResult> CreateAsync(EmployeeFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.InitialPassword))
        {
            return ServiceResult.Fail("Vui lòng nhập mật khẩu ban đầu.", nameof(form.InitialPassword));
        }

        var userName = form.UserName.Trim().ToLowerInvariant();

        if (await _db.Employees.AnyAsync(e => e.UserName == userName))
        {
            return ServiceResult.Fail($"Tên đăng nhập \"{userName}\" đã tồn tại.", nameof(form.UserName));
        }

        var idCheck = await CheckDuplicateIdNumberAsync(form.IdNumber, excludeId: null);
        if (idCheck is not null)
        {
            return idCheck;
        }

        var entity = new Employee
        {
            Code = await NextCodeAsync(),
            FullName = form.FullName.Trim(),
            PhoneNumber = form.PhoneNumber.Trim(),
            Email = Normalize(form.Email),
            IdNumber = Normalize(form.IdNumber),
            Role = form.Role,
            UserName = userName,
            PasswordHash = PasswordHasher.Hash(form.InitialPassword),
            // FR-A08: mật khẩu do người khác đặt thì luôn phải đổi ở lần đăng nhập đầu.
            MustChangePassword = true,
            Status = form.Status == EmployeeStatus.Resigned ? EmployeeStatus.Resigned : EmployeeStatus.Active
        };

        _db.Employees.Add(entity);
        await _db.SaveChangesAsync();

        // Không bao giờ ghi giá trị mật khẩu vào nhật ký.
        _audit.Log("CreateEmployee", nameof(Employee), entity.Id.ToString(),
            newValue: $"{entity.Code} — {entity.FullName} — {Roles.ToDisplayName(entity.Role.ToString())}");
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(message: $"Đã thêm nhân viên {entity.Code} — {entity.FullName}. "
                                         + "Người này phải đổi mật khẩu ở lần đăng nhập đầu tiên.");
    }

    public async Task<ServiceResult> UpdateAsync(EmployeeFormViewModel form)
    {
        var entity = await _db.Employees.FirstOrDefaultAsync(e => e.Id == form.Id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy nhân viên cần sửa.");
        }

        var isSelf = entity.Id == CurrentEmployeeId;
        var oldRole = entity.Role;
        var oldStatus = entity.Status;

        // Tên đăng nhập không sửa được sau khi tạo (SCR-A11) — bỏ qua giá trị gửi lên.
        var idCheck = await CheckDuplicateIdNumberAsync(form.IdNumber, excludeId: entity.Id);
        if (idCheck is not null)
        {
            return idCheck;
        }

        var roleChanged = form.Role != oldRole;

        if (roleChanged)
        {
            if (isSelf)
            {
                return ServiceResult.Fail(
                    "Không thể tự đổi vai trò của chính mình. Nhờ một Admin khác thực hiện.",
                    nameof(form.Role));
            }

            if (oldRole == EmployeeRole.Admin && !await HasOtherActiveAdminAsync(entity.Id))
            {
                return ServiceResult.Fail(
                    "Đây là tài khoản Admin đang hoạt động duy nhất. Tạo thêm một Admin khác trước khi hạ quyền.",
                    nameof(form.Role));
            }
        }

        // Trạng thái Locked do nút Khóa/Mở khóa quản — form chỉ nhận Đang làm / Đã nghỉ.
        var newStatus = entity.Status == EmployeeStatus.Locked
            ? entity.Status
            : (form.Status == EmployeeStatus.Resigned ? EmployeeStatus.Resigned : EmployeeStatus.Active);

        if (newStatus == EmployeeStatus.Resigned && oldStatus != EmployeeStatus.Resigned)
        {
            var resignCheck = await CheckCanResignAsync(entity, isSelf);
            if (resignCheck is not null)
            {
                return resignCheck;
            }
        }

        entity.FullName = form.FullName.Trim();
        entity.PhoneNumber = form.PhoneNumber.Trim();
        entity.Email = Normalize(form.Email);
        entity.IdNumber = Normalize(form.IdNumber);
        entity.Role = form.Role;
        entity.Status = newStatus;

        // BR-11: đổi vai trò bắt buộc ghi nhật ký.
        if (roleChanged)
        {
            _audit.Log("ChangeEmployeeRole", nameof(Employee), entity.Id.ToString(),
                oldValue: Roles.ToDisplayName(oldRole.ToString()),
                newValue: Roles.ToDisplayName(entity.Role.ToString()));
        }

        if (newStatus != oldStatus)
        {
            _audit.Log("ChangeEmployeeStatus", nameof(Employee), entity.Id.ToString(),
                oldValue: oldStatus.ToDisplayName(), newValue: newStatus.ToDisplayName());
        }

        await _db.SaveChangesAsync();

        // Người bị đổi vai trò phải đăng nhập lại để nạp quyền mới (SCR-A11). Việc đá phiên
        // cũ ra nằm ở EmployeeCookieEvents: cookie mang vai trò cũ sẽ bị từ chối ở request kế tiếp.
        var warning = roleChanged
            ? $"Đã đổi vai trò của {entity.FullName}. Người này bị đăng xuất khỏi mọi phiên đang mở "
              + "và phải đăng nhập lại để nạp quyền mới."
            : null;

        return ServiceResult.Ok(warning, $"Đã cập nhật nhân viên {entity.Code}.");
    }

    // ---------- SCR-A10: khóa / mở khóa ----------

    public async Task<ServiceResult> ToggleLockAsync(int id)
    {
        var entity = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy nhân viên.");
        }

        if (entity.Id == CurrentEmployeeId)
        {
            return ServiceResult.Fail("Không thể tự khóa tài khoản của chính mình.");
        }

        if (entity.Status == EmployeeStatus.Resigned)
        {
            return ServiceResult.Fail("Nhân viên đã nghỉ việc, tài khoản vốn không đăng nhập được.");
        }

        if (entity.Status == EmployeeStatus.Locked)
        {
            entity.Status = EmployeeStatus.Active;
            // Mở khóa thì xóa luôn bộ đếm đăng nhập sai, nếu không sẽ bị khóa lại ngay (FR-A08).
            entity.FailedLoginCount = 0;

            _audit.Log("UnlockEmployee", nameof(Employee), entity.Id.ToString(),
                oldValue: EmployeeStatus.Locked.ToDisplayName(),
                newValue: EmployeeStatus.Active.ToDisplayName());

            await _db.SaveChangesAsync();
            return ServiceResult.Ok(message: $"Đã mở khóa tài khoản {entity.UserName}.");
        }

        if (entity.Role == EmployeeRole.Admin && !await HasOtherActiveAdminAsync(entity.Id))
        {
            return ServiceResult.Fail(
                "Đây là tài khoản Admin đang hoạt động duy nhất — khóa lại sẽ không còn ai quản trị hệ thống.");
        }

        entity.Status = EmployeeStatus.Locked;

        _audit.Log("LockEmployee", nameof(Employee), entity.Id.ToString(),
            oldValue: EmployeeStatus.Active.ToDisplayName(),
            newValue: EmployeeStatus.Locked.ToDisplayName());

        await _db.SaveChangesAsync();

        return ServiceResult.Ok(message: $"Đã khóa tài khoản {entity.UserName}. "
                                         + "Mọi phiên đang mở của người này bị đăng xuất ngay.");
    }

    // ---------- SCR-A11: đặt lại mật khẩu ----------

    public async Task<ServiceResult> ResetPasswordAsync(int id)
    {
        var entity = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy nhân viên.");
        }

        if (entity.Status == EmployeeStatus.Resigned)
        {
            return ServiceResult.Fail("Nhân viên đã nghỉ việc, không đặt lại mật khẩu.");
        }

        // Hệ thống không đọc được mật khẩu cũ (chỉ lưu hash) nên không có gì để hiển thị —
        // đúng yêu cầu của SCR-A11. Mật khẩu tạm chỉ hiện đúng một lần cho Admin đọc.
        var tempPassword = GenerateTempPassword();

        entity.PasswordHash = PasswordHasher.Hash(tempPassword);
        entity.MustChangePassword = true;
        entity.FailedLoginCount = 0;

        _audit.Log("ResetEmployeePassword", nameof(Employee), entity.Id.ToString(),
            reason: "Admin đặt lại mật khẩu");

        await _db.SaveChangesAsync();

        return ServiceResult.Ok(
            warning: "Mật khẩu tạm chỉ hiện một lần, hãy chuyển cho nhân viên ngay. "
                     + "Người này bắt buộc đổi mật khẩu ở lần đăng nhập kế tiếp.",
            message: $"Mật khẩu tạm của {entity.UserName}: {tempPassword}");
    }

    // ---------- Ràng buộc dùng chung ----------

    /// <summary>Hệ thống luôn phải còn ít nhất một Admin đang hoạt động — SCR-A10.</summary>
    private async Task<bool> HasOtherActiveAdminAsync(int excludeId)
        => await _db.Employees.AnyAsync(e => e.Id != excludeId
                                             && e.Role == EmployeeRole.Admin
                                             && e.Status == EmployeeStatus.Active);

    private async Task<ServiceResult?> CheckCanResignAsync(Employee entity, bool isSelf)
    {
        if (isSelf)
        {
            return ServiceResult.Fail("Không thể tự cho chính mình nghỉ việc.", nameof(EmployeeFormViewModel.Status));
        }

        // BR-10: ca thu ngân phải đóng trước, nếu không tiền trong ca không ai chốt.
        if (await _db.CashierShifts.AnyAsync(s => s.EmployeeId == entity.Id && s.Status == ShiftStatus.Open))
        {
            return ServiceResult.Fail(
                $"{entity.FullName} còn ca làm việc đang mở. Phải đóng ca trước khi cho nghỉ việc (BR-10).",
                nameof(EmployeeFormViewModel.Status));
        }

        if (entity.Role == EmployeeRole.Admin && !await HasOtherActiveAdminAsync(entity.Id))
        {
            return ServiceResult.Fail(
                "Đây là tài khoản Admin đang hoạt động duy nhất. Tạo thêm một Admin khác trước đã.",
                nameof(EmployeeFormViewModel.Status));
        }

        return null;
    }

    private async Task<ServiceResult?> CheckDuplicateIdNumberAsync(string? idNumber, int? excludeId)
    {
        var value = Normalize(idNumber);
        if (value is null)
        {
            return null;
        }

        var duplicated = await _db.Employees
            .AnyAsync(e => e.IdNumber == value && (excludeId == null || e.Id != excludeId));

        return duplicated
            ? ServiceResult.Fail($"CCCD \"{value}\" đã được dùng cho nhân viên khác.",
                nameof(EmployeeFormViewModel.IdNumber))
            : null;
    }

    /// <summary>Mã nhân viên do hệ thống sinh: NV001, NV002... — SCR-A11 không cho nhập tay.</summary>
    private async Task<string> NextCodeAsync()
    {
        var codes = await _db.Employees
            .Where(e => e.Code.StartsWith("NV"))
            .Select(e => e.Code)
            .ToListAsync();

        var max = codes
            .Select(c => int.TryParse(c[2..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"NV{max + 1:D3}";
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Mật khẩu tạm 10 ký tự, luôn có cả chữ và số để qua được luật đổi mật khẩu ở SCR-S02.
    /// Bỏ các ký tự dễ đọc nhầm (0/O, 1/l/I) vì mật khẩu này được đọc bằng miệng cho nhau.
    /// </summary>
    private static string GenerateTempPassword()
    {
        const string letters = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = letters + digits;

        var chars = new char[10];
        chars[0] = letters[RandomNumberGenerator.GetInt32(letters.Length)];
        chars[1] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

        for (var i = 2; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        // Trộn lại để chữ và số không luôn nằm ở hai vị trí đầu.
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
