using System.Net;
using System.Net.Mail;
using System.Text;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

public interface IEmailService
{
    Task<EmailSendResult> SendBookingConfirmationAsync(int reservationId);
    Task<EmailSendResult> SendInvoiceEmailAsync(int invoiceId, string? recipientEmail = null);
}

public sealed record EmailSendResult(bool Sent, string PreviewUrl, string Message);

public class EmailService : IEmailService
{
    private readonly HotelDbContext _db;
    private readonly ISettingsReader _settings;
    private readonly IVietQrService _vietQr;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        HotelDbContext db, ISettingsReader settings, IVietQrService vietQr,
        IAuditService audit, IWebHostEnvironment env, IConfiguration configuration, ILogger<EmailService> logger)
    {
        _db = db;
        _settings = settings;
        _vietQr = vietQr;
        _audit = audit;
        _env = env;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendBookingConfirmationAsync(int reservationId)
    {
        var r = await _db.Reservations.AsNoTracking()
            .Include(x => x.PrimaryGuest)
            .Include(x => x.Rooms).ThenInclude(rr => rr.RoomType)
            .Include(x => x.Rooms).ThenInclude(rr => rr.Room)
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == reservationId);

        if (r is null)
        {
            return new(false, string.Empty, "Không tìm thấy đơn đặt phòng.");
        }

        var hotelName = await _settings.GetStringAsync(SystemSettingKeys.HotelName);
        var hotelAddress = await _settings.GetStringAsync(SystemSettingKeys.HotelAddress);
        var hotelPhone = await _settings.GetStringAsync(SystemSettingKeys.HotelPhone);

        var depositPaid = r.Deposits.Where(d => d.Status == Models.DepositStatus.Held).Sum(d => d.Amount);
        var remaining = Math.Max(0, r.EstimatedTotal - depositPaid);
        var qr = await _vietQr.BuildQrAsync(remaining > 0 ? remaining : r.EstimatedTotal, $"AURA {r.Code}");

        var sb = new StringBuilder();
        sb.Append($$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="utf-8" />
                <title>Xác nhận đặt phòng {{r.Code}} - {{hotelName}}</title>
                <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f4f6f8; margin: 0; padding: 24px; color: #1f2937; }
                    .container { max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }
                    .header { background: #14362b; color: #ffffff; padding: 28px 32px; text-align: center; }
                    .header h1 { margin: 0; font-size: 22px; letter-spacing: 0.5px; }
                    .header p { margin: 6px 0 0; opacity: 0.85; font-size: 14px; }
                    .body { padding: 28px 32px; }
                    .code-box { background: #f0fdf4; border: 1px dashed #16a34a; border-radius: 10px; padding: 16px; text-align: center; margin: 16px 0; }
                    .code-box .code { font-size: 24px; font-weight: 700; color: #15803d; letter-spacing: 1px; }
                    table { width: 100%; border-collapse: collapse; margin: 16px 0; }
                    th, td { padding: 10px 12px; border-bottom: 1px solid #e5e7eb; font-size: 14px; text-align: left; }
                    th { background: #f9fafb; color: #4b5563; font-weight: 600; }
                    .footer { background: #f9fafb; padding: 20px 32px; text-align: center; font-size: 12px; color: #6b7280; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>{{WebUtility.HtmlEncode(hotelName)}}</h1>
                        <p>XÁC NHẬN ĐẶT PHÒNG CHÍNH THỨC</p>
                    </div>
                    <div class="body">
                        <p>Xin chào quý khách <strong>{{WebUtility.HtmlEncode(r.PrimaryGuest.FullName)}}</strong>,</p>
                        <p>Cảm ơn quý khách đã đặt phòng tại <strong>{{WebUtility.HtmlEncode(hotelName)}}</strong>. Dưới đây là thông tin chi tiết đơn đặt phòng của quý khách:</p>
                        <div class="code-box">
                            <div style="font-size:12px;text-transform:uppercase;color:#4b5563;">Mã đặt phòng của quý khách</div>
                            <div class="code">{{WebUtility.HtmlEncode(r.Code)}}</div>
                        </div>
                        <table>
                            <tr><th>Ngày nhận phòng</th><td>{{r.CheckInDate:dd/MM/yyyy HH:mm}}</td></tr>
                            <tr><th>Ngày trả phòng</th><td>{{r.CheckOutDate:dd/MM/yyyy HH:mm}}</td></tr>
                            <tr><th>Số điện thoại</th><td>{{WebUtility.HtmlEncode(r.PrimaryGuest.PhoneNumber)}}</td></tr>
                            <tr><th>Tổng tiền dự kiến</th><td><strong>{{r.EstimatedTotal:N0}} ₫</strong></td></tr>
                            <tr><th>Đã thanh toán / Đặt cọc</th><td><strong style="color:#16a34a;">{{depositPaid:N0}} ₫</strong></td></tr>
                            <tr><th>Còn lại khi nhận phòng</th><td><strong style="color:#dc2626;">{{remaining:N0}} ₫</strong></td></tr>
                        </table>
                        <h4>Danh sách phòng:</h4>
                        <table>
                            <thead><tr><th>Loại phòng</th><th>Số phòng</th><th>Số khách</th></tr></thead>
                            <tbody>
            """);

        foreach (var rr in r.Rooms)
        {
            var roomNo = rr.Room != null ? $"Phòng {rr.Room.RoomNumber}" : "Sẽ xếp khi nhận phòng";
            sb.Append($"<tr><td>{WebUtility.HtmlEncode(rr.RoomType.Name)}</td><td>{WebUtility.HtmlEncode(roomNo)}</td><td>{rr.Adults + rr.Children} người</td></tr>");
        }

        sb.Append($"""
                            </tbody>
                        </table>
                        {(remaining > 0 ? $"""
                        <div style="text-align:center;margin-top:20px;padding:16px;background:#f8fafc;border-radius:10px;">
                            <p style="margin:0 0 8px;font-weight:600;">Quét mã VietQR để thanh toán trước / giữ phòng đảm bảo:</p>
                            <img src="{qr.QrImageUrl}" alt="VietQR" style="max-width:220px;border-radius:8px;" />
                            <p style="margin:8px 0 0;font-size:12px;color:#64748b;">{WebUtility.HtmlEncode(qr.BankName)} · STK: {WebUtility.HtmlEncode(qr.AccountNo)} · Nội dung: {WebUtility.HtmlEncode(qr.Memo)}</p>
                        </div>
                        """ : "")}
                    </div>
                    <div class="footer">
                        <div><strong>{WebUtility.HtmlEncode(hotelName)}</strong></div>
                        <div>Địa chỉ: {WebUtility.HtmlEncode(hotelAddress)} | Hotline: {WebUtility.HtmlEncode(hotelPhone)}</div>
                    </div>
                </div>
            </body>
            </html>
            """);

        var fileName = $"booking-{r.Code}-{DateTime.Now:HHmmss}.html";
        var (previewUrl, sent, sendNote) = await SaveAndSendAsync(
            r.PrimaryGuest.Email,
            $"[{hotelName}] Xác nhận đặt phòng #{r.Code}",
            sb.ToString(),
            fileName);

        _audit.Log("SendBookingEmail", nameof(Reservation), r.Id.ToString(),
            newValue: $"Email xác nhận {r.Code} -> {r.PrimaryGuest.Email ?? "Bản lưu nội bộ"} | {(sent ? "ĐÃ GỬI" : "KHÔNG GỬI")}: {sendNote} ({previewUrl})");
        await _db.SaveChangesAsync();

        return new(sent, previewUrl, sendNote);
    }

    public async Task<EmailSendResult> SendInvoiceEmailAsync(int invoiceId, string? recipientEmail = null)
    {
        var inv = await _db.Invoices.AsNoTracking()
            .Include(i => i.Folio).ThenInclude(f => f.Items)
            .Include(i => i.Folio).ThenInclude(f => f.Stay).ThenInclude(s => s.Room)
            .Include(i => i.Folio).ThenInclude(f => f.Stay).ThenInclude(s => s.PrimaryGuest)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (inv is null)
        {
            return new(false, string.Empty, "Không tìm thấy hóa đơn.");
        }

        var stay = inv.Folio.Stay;
        var toEmail = !string.IsNullOrWhiteSpace(recipientEmail) ? recipientEmail.Trim() : stay.PrimaryGuest.Email;

        var hotelName = await _settings.GetStringAsync(SystemSettingKeys.HotelName);
        var hotelAddress = await _settings.GetStringAsync(SystemSettingKeys.HotelAddress);
        var hotelPhone = await _settings.GetStringAsync(SystemSettingKeys.HotelPhone);

        var sb = new StringBuilder();
        sb.Append($$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="utf-8" />
                <title>Hóa đơn điện tử {{inv.InvoiceNo}} - {{hotelName}}</title>
                <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f4f6f8; margin: 0; padding: 24px; color: #1f2937; }
                    .container { max-width: 680px; margin: 0 auto; background: #ffffff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }
                    .header { background: #14362b; color: #ffffff; padding: 24px 32px; display: flex; justify-content: space-between; align-items: center; }
                    .body { padding: 28px 32px; }
                    table { width: 100%; border-collapse: collapse; margin: 16px 0; }
                    th, td { padding: 10px 12px; border-bottom: 1px solid #e5e7eb; font-size: 14px; }
                    th { background: #f9fafb; text-align: left; }
                    .text-end { text-align: right; }
                    .footer { background: #f9fafb; padding: 20px 32px; text-align: center; font-size: 12px; color: #6b7280; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <div>
                            <h2 style="margin:0;">{{WebUtility.HtmlEncode(hotelName)}}</h2>
                            <div style="font-size:13px;opacity:0.85;">HÓA ĐƠN GTGT / RECEIPT #{{WebUtility.HtmlEncode(inv.InvoiceNo)}}</div>
                        </div>
                    </div>
                    <div class="body">
                        <p>Kính gửi quý khách <strong>{{WebUtility.HtmlEncode(stay.PrimaryGuest.FullName)}}</strong>,</p>
                        <p>Cảm ơn quý khách đã lưu trú tại Phòng <strong>{{WebUtility.HtmlEncode(stay.Room.RoomNumber)}}</strong> ({{stay.ActualCheckIn:dd/MM/yyyy HH:mm}} – {{stay.ActualCheckOut:dd/MM/yyyy HH:mm}}).</p>
                        <table>
                            <thead><tr><th>Nội dung</th><th class="text-end">SL</th><th class="text-end">Đơn giá</th><th class="text-end">Thành tiền</th></tr></thead>
                            <tbody>
            """);

        foreach (var item in inv.Folio.Items.Where(x => !x.IsVoided))
        {
            sb.Append($"<tr><td>{WebUtility.HtmlEncode(item.Description)}</td><td class='text-end'>{item.Quantity}</td><td class='text-end'>{item.UnitPrice:N0} ₫</td><td class='text-end'>{item.Amount:N0} ₫</td></tr>");
        }

        sb.Append($"""
                            </tbody>
                            <tfoot>
                                <tr><th colspan="3" class="text-end">Tạm tính</th><td class="text-end">{inv.SubTotal:N0} ₫</td></tr>
                                <tr><th colspan="3" class="text-end">VAT ({inv.TaxRate * 100:0.#}%)</th><td class="text-end">{inv.TaxAmount:N0} ₫</td></tr>
                                <tr><th colspan="3" class="text-end">Tổng cộng</th><td class="text-end"><strong>{inv.TotalAmount:N0} ₫</strong></td></tr>
                                <tr><th colspan="3" class="text-end">Đã thanh toán</th><td class="text-end" style="color:#16a34a;"><strong>{inv.AmountPaid:N0} ₫</strong></td></tr>
                                {(inv.DebtAmount > 0 ? $"<tr><th colspan='3' class='text-end'>Còn nợ</th><td class='text-end' style='color:#dc2626;'><strong>{inv.DebtAmount:N0} ₫</strong></td></tr>" : "")}
                            </tfoot>
                        </table>
                    </div>
                    <div class="footer">
                        <div><strong>{WebUtility.HtmlEncode(hotelName)}</strong> — {WebUtility.HtmlEncode(hotelAddress)} — ĐT: {WebUtility.HtmlEncode(hotelPhone)}</div>
                    </div>
                </div>
            </body>
            </html>
            """);

        var fileName = $"invoice-{inv.InvoiceNo}-{DateTime.Now:HHmmss}.html";
        var (previewUrl, sent, sendNote) = await SaveAndSendAsync(
            toEmail,
            $"[{hotelName}] Hóa đơn điện tử #{inv.InvoiceNo}",
            sb.ToString(),
            fileName);

        _audit.Log("SendInvoiceEmail", nameof(Invoice), inv.Id.ToString(),
            newValue: $"Gửi hóa đơn {inv.InvoiceNo} -> {toEmail ?? "Bản lưu nội bộ"} | {(sent ? "ĐÃ GỬI" : "KHÔNG GỬI")}: {sendNote} ({previewUrl})");
        await _db.SaveChangesAsync();

        return new(sent, previewUrl, sendNote);
    }

    /// <summary>
    /// Thư mục lưu bản HTML của email — đặt NGOÀI thư mục dự án để `dotnet watch` không coi là
    /// thay đổi mã nguồn (ghi file vào wwwroot làm Hot Reload crash và tắt server mỗi lần đặt phòng).
    /// Program.cs ánh xạ thư mục này ra URL /uploads/emails.
    /// </summary>
    public static string GetPreviewFolder(IConfiguration configuration)
    {
        var configured = configuration["Storage:EmailPreviewPath"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDir)) baseDir = Path.GetTempPath();
        return Path.Combine(baseDir, "AuraHotel", "emails");
    }

    /// <summary>
    /// Lưu bản HTML rồi gửi SMTP thật nếu đã cấu hình. Trả về (đường dẫn xem trước, đã gửi thật hay chưa, lý do).
    /// Cấu hình SMTP lấy từ mục "Smtp" trong appsettings / user-secrets trước,
    /// sau đó mới lùi về cấu hình lưu từ màn Cài đặt.
    /// </summary>
    private async Task<(string PreviewUrl, bool Sent, string Note)> SaveAndSendAsync(string? toEmail, string subject, string htmlBody, string fileName)
    {
        var folder = GetPreviewFolder(_configuration);
        Directory.CreateDirectory(folder);
        var fullPath = Path.Combine(folder, fileName);
        await File.WriteAllTextAsync(fullPath, htmlBody, Encoding.UTF8);
        var previewUrl = $"/uploads/emails/{fileName}";

        var enabled = await _settings.GetBoolAsync(SystemSettingKeys.EmailEnabled);
        if (!enabled) return (previewUrl, false, "Tính năng gửi email đang tắt trong Cài đặt.");
        if (string.IsNullOrWhiteSpace(toEmail)) return (previewUrl, false, "Khách chưa có địa chỉ email.");

        var smtpHost = FirstNonEmpty(_configuration["Smtp:Host"], (await _settings.GetStringAsync(SystemSettingKeys.EmailSmtpHost)).Trim());
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            const string note = "Chưa cấu hình máy chủ SMTP. Hãy nhập SMTP trong Cài đặt > Thanh toán VietQR & Email.";
            _logger.LogWarning("Không gửi email tới {To}: {Note}", toEmail, note);
            return (previewUrl, false, note);
        }

        try
        {
            var portRaw = FirstNonEmpty(_configuration["Smtp:Port"], (await _settings.GetIntAsync(SystemSettingKeys.EmailSmtpPort)).ToString());
            var smtpPort = int.TryParse(portRaw, out var pp) && pp > 0 ? pp : 587;
            var smtpUser = FirstNonEmpty(_configuration["Smtp:User"], (await _settings.GetStringAsync(SystemSettingKeys.EmailSmtpUser)).Trim());
            var smtpPass = FirstNonEmpty(_configuration["Smtp:Password"], await _settings.GetStringAsync(SystemSettingKeys.EmailSmtpPass));
            var senderAddr = FirstNonEmpty(_configuration["Smtp:SenderAddress"], (await _settings.GetStringAsync(SystemSettingKeys.EmailSenderAddress)).Trim());
            var senderName = (await _settings.GetStringAsync(SystemSettingKeys.EmailSenderName)).Trim();

            if (string.IsNullOrWhiteSpace(smtpUser))
                return (previewUrl, false, "Chưa nhập tài khoản SMTP.");
            if (string.IsNullOrWhiteSpace(smtpPass))
                return (previewUrl, false, "Chưa nhập mật khẩu ứng dụng SMTP.");

            if (smtpHost.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                // Google thường hiển thị App Password thành 4 nhóm; chấp nhận cả khi
                // người dùng dán kèm khoảng trắng.
                smtpPass = string.Concat(smtpPass.Where(c => !char.IsWhiteSpace(c)));
                if (smtpPass.Length != 16)
                    return (previewUrl, false,
                        "Gmail yêu cầu Mật khẩu ứng dụng gồm 16 ký tự; không dùng mật khẩu đăng nhập Google.");
            }

            // Gmail/Outlook chỉ cho gửi từ chính tài khoản đăng nhập SMTP; địa chỉ khác sẽ bị ghi đè hoặc từ chối.
            var fromAddr = !string.IsNullOrWhiteSpace(smtpUser) && smtpUser.Contains('@') ? smtpUser
                         : string.IsNullOrWhiteSpace(senderAddr) ? "booking@aurahotel.vn" : senderAddr;

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Timeout = 20000
            };
            if (!string.IsNullOrWhiteSpace(smtpUser))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            }

            using var msg = new MailMessage
            {
                From = new MailAddress(fromAddr, senderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            msg.To.Add(toEmail);
            await client.SendMailAsync(msg);
            return (previewUrl, true, $"Đã gửi thành công tới {toEmail} qua {smtpHost}:{smtpPort}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email SMTP tới {To} thất bại", toEmail);
            // SmtpClient thường bọc lỗi thật (VD: sai mật khẩu / chưa bật App Password)
            // trong InnerException và chỉ để Message bên ngoài là "Failure sending mail".
            var detail = ex.GetBaseException().Message;
            if (string.IsNullOrWhiteSpace(detail)) detail = ex.Message;
            return (previewUrl, false, "SMTP từ chối gửi: " + detail);
        }
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}
