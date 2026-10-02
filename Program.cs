using HotelManagement.Web.Data;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Gán người dùng hiện tại cho DbContext (CreatedBy/UpdatedBy), chạy trước mọi filter khác.
    options.Filters.Add<CurrentUserFilter>(int.MinValue);
    // Ép đổi mật khẩu ở lần đăng nhập đầu — FR-A08.
    options.Filters.Add<MustChangePasswordFilter>();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
// Menu trái hỏi dịch vụ này để biết mục nào được hiện — xem Security/ScreenAccess.cs.
builder.Services.AddScoped<IScreenAccess, ScreenAccess>();

// Nghiệp vụ danh mục.
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Nghiệp vụ khách hàng.
builder.Services.AddScoped<IGuestService, GuestService>();

// Hạ tầng dùng chung cho phần vận hành (SP0) — đặt phòng, lễ tân, thu ngân, báo cáo đều dựa vào.
builder.Services.AddScoped<ISettingsReader, SettingsReader>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<INumberSequenceService, NumberSequenceService>();
builder.Services.AddScoped<ITransactionRunner, TransactionRunner>();
builder.Services.AddScoped<IShiftService, ShiftService>();

// Nghiệp vụ đặt phòng (SP1 — nhóm C).
builder.Services.AddScoped<IReservationService, ReservationService>();

// Vận hành: lễ tân (D), thu ngân (F), buồng phòng (E), báo cáo (G).
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IFrontDeskService, FrontDeskService>();
builder.Services.AddScoped<IHousekeepingService, HousekeepingService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Đối chiếu cookie đăng nhập với bản ghi nhân viên ở mỗi request — xem Security/EmployeeCookieEvents.cs.
builder.Services.AddScoped<EmployeeCookieEvents>();

// Giữ nguyên ký tự tiếng Việt trong HTML thay vì mã hóa thành &#x...;
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("HotelDb"),
        // Tự thử lại khi gặp lỗi tạm thời của SQL Server — NFR-03. Transaction nghiệp vụ phải
        // đi qua ITransactionRunner (bọc execution strategy) mới mở được transaction thủ công.
        sql => sql.EnableRetryOnFailure()));

// ── DUAL COOKIE AUTHENTICATION — SCR-S01, NFR-02 ──────────────────────────────
// Hai scheme hoàn toàn độc lập: StaffCookie cho nhân viên, ClientCookie cho khách hàng.
// DefaultScheme trỏ về Staff để FallbackPolicy bảo vệ các trang nội bộ theo mặc định.
builder.Services.AddAuthentication(AppSchemes.Staff)
    // 1. SCHEME NHÂN VIÊN (Staff)
    .AddCookie(AppSchemes.Staff, options =>
    {
        options.Cookie.Name          = "StaffAuth";
        options.LoginPath            = "/Account/Login";
        options.LogoutPath           = "/Account/Logout";
        options.AccessDeniedPath     = "/Home/Forbidden"; // SCR-S03: 403, không im lặng redirect.
        options.ReturnUrlParameter   = "returnUrl";
        options.ExpireTimeSpan       = TimeSpan.FromHours(8); // Hết hạn sau 1 ca làm việc.
        options.SlidingExpiration    = true;
        options.Cookie.HttpOnly      = true;
        options.Cookie.SameSite      = SameSiteMode.Lax;
        options.Cookie.SecurePolicy  = CookieSecurePolicy.SameAsRequest;
        options.Cookie.IsEssential   = true;
        // Mỗi request đối chiếu vai trò & trạng thái với DB — SCR-A10, SCR-A11.
        options.EventsType           = typeof(EmployeeCookieEvents);
    })
    // 2. SCHEME KHÁCH HÀNG (Client)
    .AddCookie(AppSchemes.Client, options =>
    {
        options.Cookie.Name          = "ClientAuth";
        options.LoginPath            = "/ClientAuth/Login";
        options.LogoutPath           = "/ClientAuth/Logout";
        options.AccessDeniedPath     = "/ClientAuth/Login";
        options.ReturnUrlParameter   = "returnUrl";
        options.ExpireTimeSpan       = TimeSpan.FromDays(30); // Khách hàng lưu phiên lâu hơn.
        options.SlidingExpiration    = true;
        options.Cookie.HttpOnly      = true;
        options.Cookie.SameSite      = SameSiteMode.Lax;
        options.Cookie.SecurePolicy  = CookieSecurePolicy.SameAsRequest;
        options.Cookie.IsEssential   = true;
    });

// ── AUTHORIZATION POLICIES ──────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    // Policy: chỉ dành cho Nhân viên (mọi chức vụ).
    options.AddPolicy("StaffOnly", policy =>
    {
        policy.AddAuthenticationSchemes(AppSchemes.Staff);
        policy.RequireAuthenticatedUser();
    });

    // Policy: chỉ dành cho Admin.
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AddAuthenticationSchemes(AppSchemes.Staff);
        policy.RequireRole(Roles.Admin);
    });

    // Policy: chỉ dành cho Khách hàng đã đăng nhập trên Portal.
    options.AddPolicy("ClientOnly", policy =>
    {
        policy.AddAuthenticationSchemes(AppSchemes.Client);
        policy.RequireAuthenticatedUser();
    });

    // FallbackPolicy: bất kỳ endpoint nào không có [AllowAnonymous] đều phải qua StaffAuth.
    // Điều này bảo vệ toàn bộ trang nội bộ mà không cần [Authorize] trên từng Controller.
    options.FallbackPolicy = new AuthorizationPolicyBuilder(AppSchemes.Staff)
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Áp migration còn thiếu và nạp dữ liệu khởi tạo (NFR-08).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// File người dùng tải lên lúc chạy (logo khách sạn — SCR-A12) không nằm trong manifest của
// MapStaticAssets vốn chỉ biết những file có sẵn lúc build, nên phải phục vụ bằng middleware
// tĩnh riêng. Chỉ mở đúng thư mục uploads, không mở cả wwwroot lần nữa.
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// AllowAnonymous bắt buộc: MapStaticAssets đăng ký endpoint thật, nếu không loại trừ thì
// FallbackPolicy sẽ chặn cả CSS/JS và trang đăng nhập hiện ra không có định dạng.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Portal}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
