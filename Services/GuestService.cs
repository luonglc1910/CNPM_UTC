using System.Text.RegularExpressions;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Nghiệp vụ hồ sơ khách — SCR-B01, SCR-B02, SCR-B03, FR-B01, FR-B02, FR-B03.
///
/// Hai nguyên tắc xuyên suốt: hồ sơ khách **không bao giờ xóa cứng**, và số giấy tờ là
/// dữ liệu cá nhân nên chỉ hiện đầy đủ ở màn hình chi tiết (nơi lễ tân thật sự cần đối chiếu).
/// </summary>
public interface IGuestService
{
    Task<GuestIndexViewModel> SearchAsync(GuestIndexViewModel filter, int page);
    Task<GuestFormViewModel?> GetForEditAsync(int id);
    Task<GuestDetailsViewModel?> GetDetailsAsync(int id);
    Task<ServiceResult> CreateAsync(GuestFormViewModel form);
    Task<ServiceResult> UpdateAsync(GuestFormViewModel form);

    /// <summary>Tra trùng khi đang nhập — SCR-B02, gọi từ JavaScript.</summary>
    Task<GuestDuplicateCheckResult> CheckDuplicateAsync(string? idNumber, string? phoneNumber, int? excludeId);

    /// <summary>Danh sách khai báo tạm trú của một ngày — SCR-B04.</summary>
    Task<ResidenceViewModel> BuildResidenceAsync(DateTime? date);

    /// <summary>Đưa vào / gỡ khỏi danh sách hạn chế — SCR-B05.</summary>
    Task<ServiceResult> SetBlacklistAsync(BlacklistForm form);
}

/// <inheritdoc />
public class GuestService : IGuestService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;
    private readonly ISettingsService _settings;

    public GuestService(HotelDbContext db, IAuditService audit, ISettingsService settings)
    {
        _db = db;
        _audit = audit;
        _settings = settings;
    }

    // ---------- SCR-B01: danh sách ----------

    public async Task<GuestIndexViewModel> SearchAsync(GuestIndexViewModel filter, int page)
    {
        var threshold = await GetLoyalThresholdAsync();

        var query = _db.Guests.AsNoTracking();

        if (filter.Blacklisted is not null)
        {
            query = query.Where(g => g.IsBlacklisted == filter.Blacklisted);
        }

        if (!string.IsNullOrWhiteSpace(filter.Nationality))
        {
            query = query.Where(g => g.Nationality == filter.Nationality);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim();
            query = query.Where(g => g.FullName.Contains(k)
                                     || g.PhoneNumber.Contains(k)
                                     || g.IdNumber.Contains(k));
        }

        // Lọc theo lần ở gần nhất: so trên ngày nhận phòng của lượt lưu trú mới nhất.
        if (filter.LastStayFrom is not null)
        {
            var from = filter.LastStayFrom.Value.Date;
            query = query.Where(g => g.StayGuests.Any(sg => sg.Stay.ActualCheckIn >= from));
        }

        if (filter.LastStayTo is not null)
        {
            var to = filter.LastStayTo.Value.Date.AddDays(1);
            query = query.Where(g => g.StayGuests.Any(sg => sg.Stay.ActualCheckIn < to));
        }

        var projected = query
            .OrderBy(g => g.FullName)
            .Select(g => new GuestListItemViewModel
            {
                Id = g.Id,
                FullName = g.FullName,
                Gender = g.Gender,
                IdType = g.IdType,
                IdNumber = g.IdNumber,
                PhoneNumber = g.PhoneNumber,
                Nationality = g.Nationality,
                IsBlacklisted = g.IsBlacklisted,
                CompletedStayCount = g.StayGuests.Count(sg => sg.Stay.Status == StayStatus.CheckedOut),
                LastStayAt = g.StayGuests.Max(sg => (DateTime?)sg.Stay.ActualCheckIn),
                LoyalThreshold = threshold
            });

        filter.Results = await PagedList<GuestListItemViewModel>.CreateAsync(projected, page);

        filter.NationalityOptions = await _db.Guests.AsNoTracking()
            .Select(g => g.Nationality)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        return filter;
    }

    // ---------- SCR-B02: thêm / sửa ----------

    public async Task<GuestFormViewModel?> GetForEditAsync(int id)
    {
        var entity = await _db.Guests.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        if (entity is null)
        {
            return null;
        }

        return new GuestFormViewModel
        {
            Id = entity.Id,
            FullName = entity.FullName,
            IdType = entity.IdType,
            IdNumber = entity.IdNumber,
            DateOfBirth = entity.DateOfBirth,
            Gender = entity.Gender,
            Nationality = entity.Nationality,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            Address = entity.Address,
            Notes = entity.Notes,
            IsStaying = await IsStayingAsync(id)
        };
    }

    public async Task<ServiceResult> CreateAsync(GuestFormViewModel form)
    {
        var check = await ValidateAsync(form, excludeId: null);
        if (check is not null)
        {
            return check;
        }

        var entity = new Guest
        {
            FullName = form.FullName.Trim(),
            IdType = form.IdType,
            IdNumber = NormalizeId(form.IdNumber),
            DateOfBirth = form.DateOfBirth,
            Gender = form.Gender,
            Nationality = form.Nationality.Trim(),
            PhoneNumber = form.PhoneNumber.Trim(),
            Email = Normalize(form.Email),
            Address = Normalize(form.Address),
            Notes = Normalize(form.Notes)
        };

        _db.Guests.Add(entity);
        await _db.SaveChangesAsync();

        _audit.Log("CreateGuest", nameof(Guest), entity.Id.ToString(),
            // Nhật ký chỉ ghi số giấy tờ dạng che — không nhân bản dữ liệu cá nhân sang bảng khác.
            newValue: $"{entity.FullName} — {entity.IdType.ToDisplayName()} {IdNumberMask.Mask(entity.IdNumber)}");
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(
            warning: await BuildWarningsAsync(entity.Id, form, isEdit: false),
            message: $"Đã thêm hồ sơ khách {entity.FullName}.");
    }

    public async Task<ServiceResult> UpdateAsync(GuestFormViewModel form)
    {
        var entity = await _db.Guests.FirstOrDefaultAsync(g => g.Id == form.Id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy hồ sơ khách cần sửa.");
        }

        var check = await ValidateAsync(form, excludeId: entity.Id);
        if (check is not null)
        {
            return check;
        }

        var oldIdNumber = entity.IdNumber;
        var newIdNumber = NormalizeId(form.IdNumber);
        var isStaying = await IsStayingAsync(entity.Id);

        entity.FullName = form.FullName.Trim();
        entity.IdType = form.IdType;
        entity.IdNumber = newIdNumber;
        entity.DateOfBirth = form.DateOfBirth;
        entity.Gender = form.Gender;
        entity.Nationality = form.Nationality.Trim();
        entity.PhoneNumber = form.PhoneNumber.Trim();
        entity.Email = Normalize(form.Email);
        entity.Address = Normalize(form.Address);
        entity.Notes = Normalize(form.Notes);

        // Sửa số giấy tờ của khách đang ở thường là sửa lỗi nhập sai, nhưng vẫn phải để lại vết:
        // đây là thông tin dùng để đối chiếu nhân thân (SCR-B02).
        if (!string.Equals(oldIdNumber, newIdNumber, StringComparison.Ordinal))
        {
            _audit.Log("ChangeGuestIdNumber", nameof(Guest), entity.Id.ToString(),
                reason: isStaying ? "Khách đang lưu trú" : null,
                oldValue: IdNumberMask.Mask(oldIdNumber),
                newValue: IdNumberMask.Mask(newIdNumber));
        }

        await _db.SaveChangesAsync();

        var warnings = await BuildWarningsAsync(entity.Id, form, isEdit: true);

        if (!string.Equals(oldIdNumber, newIdNumber, StringComparison.Ordinal) && isStaying)
        {
            warnings = Join(warnings,
                "Khách này đang lưu trú mà số giấy tờ vừa bị đổi. Thay đổi đã được ghi vào nhật ký thao tác.");
        }

        return ServiceResult.Ok(warnings, $"Đã cập nhật hồ sơ khách {entity.FullName}.");
    }

    // ---------- SCR-B03: chi tiết & lịch sử ----------

    public async Task<GuestDetailsViewModel?> GetDetailsAsync(int id)
    {
        var entity = await _db.Guests.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        if (entity is null)
        {
            return null;
        }

        var stays = await _db.StayGuests.AsNoTracking()
            .Where(sg => sg.GuestId == id)
            .OrderByDescending(sg => sg.Stay.ActualCheckIn)
            .Select(sg => new GuestStayHistoryItem
            {
                StayId = sg.StayId,
                CheckIn = sg.Stay.ActualCheckIn,
                CheckOut = sg.Stay.ActualCheckOut,
                RoomNumber = sg.Stay.Room.RoomNumber,
                RoomTypeName = sg.Stay.Room.RoomType.Name,
                Nights = sg.Stay.Nights,
                Status = sg.Stay.Status,
                IsPrimaryGuest = sg.IsPrimary,
                InvoiceNo = sg.Stay.Folio!.Invoice!.InvoiceNo,
                InvoiceTotal = sg.Stay.Folio.Invoice.TotalAmount,
                InvoiceStatus = sg.Stay.Folio.Invoice.Status
            })
            .ToListAsync();

        var reservations = await _db.Reservations.AsNoTracking()
            .Where(r => r.PrimaryGuestId == id)
            .OrderByDescending(r => r.CheckInDate)
            .Select(r => new GuestReservationHistoryItem
            {
                ReservationId = r.Id,
                Code = r.Code,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                Nights = r.Nights,
                Status = r.Status,
                EstimatedTotal = r.EstimatedTotal
            })
            .ToListAsync();

        var completed = stays.Where(s => s.Status == StayStatus.CheckedOut).ToList();

        return new GuestDetailsViewModel
        {
            Id = entity.Id,
            FullName = entity.FullName,
            IdType = entity.IdType,
            IdNumber = entity.IdNumber,
            DateOfBirth = entity.DateOfBirth,
            Gender = entity.Gender,
            Nationality = entity.Nationality,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            Address = entity.Address,
            Notes = entity.Notes,
            IsBlacklisted = entity.IsBlacklisted,
            BlacklistReason = entity.BlacklistReason,
            CompletedStayCount = completed.Count,
            TotalNights = completed.Sum(s => s.Nights),
            // Chỉ cộng hóa đơn Settled, bỏ qua Void (SCR-B03). Và chỉ cộng những lượt khách này
            // đứng tên — hóa đơn của phòng thuộc về người đứng tên, cộng cho cả người ở cùng
            // là tính trùng tiền.
            TotalSpending = completed
                .Where(s => s.IsPrimaryGuest && s.InvoiceStatus == InvoiceStatus.Settled)
                .Sum(s => s.InvoiceTotal ?? 0m),
            LastStayAt = stays.Count == 0 ? null : stays.Max(s => s.CheckIn),
            LoyalThreshold = await GetLoyalThresholdAsync(),
            IsStaying = stays.Any(s => s.Status == StayStatus.CheckedIn),
            Stays = stays,
            Reservations = reservations
        };
    }

    // ---------- SCR-B02: tra trùng ----------

    public async Task<GuestDuplicateCheckResult> CheckDuplicateAsync(
        string? idNumber, string? phoneNumber, int? excludeId)
    {
        var result = new GuestDuplicateCheckResult();

        var id = NormalizeId(idNumber ?? string.Empty);
        if (!string.IsNullOrEmpty(id))
        {
            result.IdMatch = await _db.Guests.AsNoTracking()
                .Where(g => g.IdNumber == id && (excludeId == null || g.Id != excludeId))
                .Select(g => new GuestDuplicateCheckResult.GuestMatch
                {
                    Id = g.Id,
                    FullName = g.FullName,
                    MaskedIdNumber = g.IdNumber,
                    PhoneNumber = g.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (result.IdMatch is not null)
            {
                result.IdMatch.MaskedIdNumber = IdNumberMask.Mask(result.IdMatch.MaskedIdNumber);
            }
        }

        var phone = phoneNumber?.Trim();
        if (!string.IsNullOrEmpty(phone))
        {
            result.PhoneMatches = await _db.Guests.AsNoTracking()
                .Where(g => g.PhoneNumber == phone && (excludeId == null || g.Id != excludeId))
                .Take(5)
                .Select(g => new GuestDuplicateCheckResult.GuestMatch
                {
                    Id = g.Id,
                    FullName = g.FullName,
                    MaskedIdNumber = g.IdNumber,
                    PhoneNumber = g.PhoneNumber
                })
                .ToListAsync();

            foreach (var match in result.PhoneMatches)
            {
                match.MaskedIdNumber = IdNumberMask.Mask(match.MaskedIdNumber);
            }
        }

        return result;
    }

    // ---------- Kiểm tra dùng chung ----------

    /// <summary>
    /// Những ràng buộc DataAnnotations không diễn đạt được: định dạng số giấy tờ phụ thuộc
    /// loại giấy tờ, địa chỉ bắt buộc với khách Việt Nam, và số giấy tờ phải duy nhất.
    /// </summary>
    private async Task<ServiceResult?> ValidateAsync(GuestFormViewModel form, int? excludeId)
    {
        var idNumber = NormalizeId(form.IdNumber);

        var formatError = form.IdType switch
        {
            GuestIdType.CitizenId when !Regex.IsMatch(idNumber, @"^\d{12}$")
                => "Số CCCD phải gồm đúng 12 chữ số.",
            GuestIdType.IdCard when !Regex.IsMatch(idNumber, @"^\d{9}$|^\d{12}$")
                => "Số CMND phải gồm 9 hoặc 12 chữ số.",
            GuestIdType.Passport when !Regex.IsMatch(idNumber, @"^[A-Z0-9]{6,20}$")
                => "Số hộ chiếu gồm 6–20 ký tự chữ in hoa và số.",
            _ => null
        };

        if (formatError is not null)
        {
            return ServiceResult.Fail(formatError, nameof(form.IdNumber));
        }

        var duplicated = await _db.Guests
            .Where(g => g.IdNumber == idNumber && (excludeId == null || g.Id != excludeId))
            .Select(g => new { g.Id, g.FullName })
            .FirstOrDefaultAsync();

        if (duplicated is not null)
        {
            // Chặn cứng, và chỉ ra hồ sơ đang giữ số này để người dùng gộp thủ công (SCR-B02).
            return ServiceResult.Fail(
                $"Số giấy tờ này đã thuộc hồ sơ \"{duplicated.FullName}\" (mã #{duplicated.Id}). "
                + "Hãy dùng hồ sơ có sẵn hoặc gộp hồ sơ thủ công.",
                nameof(form.IdNumber));
        }

        if (form.DateOfBirth is not null && form.DateOfBirth.Value.Date > DateTime.Today)
        {
            return ServiceResult.Fail("Ngày sinh không được ở tương lai.", nameof(form.DateOfBirth));
        }

        if (string.Equals(form.Nationality.Trim(), GuestFormViewModel.VietnameseNationality,
                StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(form.Address))
        {
            return ServiceResult.Fail(
                "Khách Việt Nam phải có địa chỉ thường trú để khai báo tạm trú.", nameof(form.Address));
        }

        return null;
    }

    /// <summary>Những điều nên báo nhưng không đáng chặn: khách vị thành niên, trùng SĐT.</summary>
    private async Task<string?> BuildWarningsAsync(int guestId, GuestFormViewModel form, bool isEdit)
    {
        string? warning = null;

        if (form.DateOfBirth is not null)
        {
            var age = DateTime.Today.Year - form.DateOfBirth.Value.Year;
            if (form.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age))
            {
                age--;
            }

            if (age < 18)
            {
                warning = Join(warning, $"Khách chưa đủ 18 tuổi ({age} tuổi) — cần người giám hộ đi cùng.");
            }
        }

        // Người nhà dùng chung số điện thoại là chuyện bình thường nên chỉ cảnh báo (SCR-B02).
        var phone = form.PhoneNumber.Trim();
        var samePhone = await _db.Guests.AsNoTracking()
            .Where(g => g.PhoneNumber == phone && g.Id != guestId)
            .Select(g => g.FullName)
            .Take(3)
            .ToListAsync();

        if (samePhone.Count > 0)
        {
            warning = Join(warning,
                $"Số điện thoại này trùng với hồ sơ: {string.Join(", ", samePhone)}.");
        }

        return warning;
    }

    private async Task<bool> IsStayingAsync(int guestId)
        => await _db.StayGuests
            .AnyAsync(sg => sg.GuestId == guestId && sg.Stay.Status == StayStatus.CheckedIn);

    /// <summary>
    /// Ngưỡng khách quen lấy từ cấu hình hệ thống (SCR-A12) chứ không viết cứng số 3 —
    /// đây là màn hình đầu tiên thật sự đọc tham số cấu hình.
    /// </summary>
    private async Task<int> GetLoyalThresholdAsync()
    {
        var settings = await _settings.GetAsync();
        return settings.Limit.LoyalGuestStayThreshold;
    }

    private static string NormalizeId(string value)
        => value.Trim().ToUpperInvariant();

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Join(string? first, string second)
        => string.IsNullOrEmpty(first) ? second : $"{first} {second}";

    // ---------- SCR-B04 ----------

    public async Task<ResidenceViewModel> BuildResidenceAsync(DateTime? date)
    {
        var day = (date ?? DateTime.Now).Date;
        var nextDay = day.AddDays(1);

        // Tiêu chí của spec: mọi lượt lưu trú GIAO với ngày được chọn. Khách nhận phòng
        // trong ngày hoặc trước đó, và chưa trả phòng trước khi ngày đó bắt đầu.
        var rows = await _db.StayGuests.AsNoTracking()
            .Where(sg => sg.Stay.ActualCheckIn < nextDay
                && (sg.Stay.ActualCheckOut == null || sg.Stay.ActualCheckOut >= day))
            .OrderBy(sg => sg.Stay.Room.RoomNumber)
            .ThenByDescending(sg => sg.IsPrimary)
            .ThenBy(sg => sg.Guest.FullName)
            .Select(sg => new ResidenceRow
            {
                GuestId = sg.GuestId,
                FullName = sg.Guest.FullName,
                DateOfBirth = sg.Guest.DateOfBirth,
                Gender = sg.Guest.Gender,
                Nationality = sg.Guest.Nationality,
                IdType = sg.Guest.IdType,
                IdNumber = sg.Guest.IdNumber,
                Address = sg.Guest.Address,
                RoomNumber = sg.Stay.Room.RoomNumber,
                From = sg.Stay.ActualCheckIn,
                To = sg.Stay.ActualCheckOut,
                IsPrimary = sg.IsPrimary
            })
            .ToListAsync();

        // Màn hình này hiện số giấy tờ đầy đủ của nhiều người một lúc. Spec xếp nó vào nhóm
        // "dữ liệu cá nhân" nên mỗi lần mở đều phải trả lời được: ai đọc, ngày nào, lúc nào.
        await _audit.LogAndSaveAsync(
            AuditActions.ViewResidenceList,
            nameof(Guest),
            entityId: null,
            reason: $"Ngày {day:dd/MM/yyyy} — {rows.Count} người");

        return new ResidenceViewModel { Date = day, Rows = rows };
    }

    // ---------- SCR-B05 ----------

    public async Task<ServiceResult> SetBlacklistAsync(BlacklistForm form)
    {
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.Id == form.Id);
        if (guest is null)
        {
            return ServiceResult.Fail("Không tìm thấy hồ sơ khách.");
        }

        if (guest.IsBlacklisted == form.AddToBlacklist)
        {
            return ServiceResult.Fail(form.AddToBlacklist
                ? $"Khách {guest.FullName} đã nằm trong danh sách hạn chế."
                : $"Khách {guest.FullName} không nằm trong danh sách hạn chế.");
        }

        var oldReason = guest.BlacklistReason;
        var reason = form.Reason.Trim();
        var note = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();

        guest.IsBlacklisted = form.AddToBlacklist;

        // Gỡ khỏi danh sách thì xóa lý do cũ: để lại sẽ khiến màn hình chi tiết hiện lý do
        // hạn chế cho một khách đang bình thường.
        guest.BlacklistReason = form.AddToBlacklist
            ? (note is null ? reason : $"{reason} — {note}")
            : null;

        _audit.Log(
            form.AddToBlacklist ? AuditActions.BlacklistAdd : AuditActions.BlacklistRemove,
            nameof(Guest),
            guest.Id.ToString(),
            reason: reason,
            oldValue: oldReason ?? "(không hạn chế)",
            newValue: guest.BlacklistReason ?? "(không hạn chế)");

        await _db.SaveChangesAsync();

        return ServiceResult.Ok(form.AddToBlacklist
            ? $"Đã đưa khách {guest.FullName} vào danh sách hạn chế."
            : $"Đã gỡ khách {guest.FullName} khỏi danh sách hạn chế.");
    }
}
