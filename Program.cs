using HotelManagement.Web.Data;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
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

// Giữ nguyên ký tự tiếng Việt trong HTML thay vì mã hóa thành &#x...;
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HotelDb")));

// Xác thực bằng cookie — SCR-S01, NFR-02.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        // SCR-S03: sai quyền thì trả trang 403, không im lặng đá về trang chủ.
        options.AccessDeniedPath = "/Home/Forbidden";
        options.ReturnUrlParameter = "returnUrl";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "HotelAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.IsEssential = true;

        // Phân biệt "hết phiên" với "chưa từng đăng nhập": còn cookie mà vẫn bị từ chối
        // nghĩa là phiên đã hết hạn (00-conventions.md mục 2).
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Cookies.ContainsKey("HotelAuth"))
            {
                context.RedirectUri = QueryHelpers.AddQueryString(context.RedirectUri, "expired", "1");
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// Mặc định mọi endpoint đều phải đăng nhập; trang công khai phải tự đánh [AllowAnonymous].
// Chọn FallbackPolicy thay vì [Authorize] trên AdminControllerBase vì Home và Account
// không kế thừa lớp đó — đặt ở đây thì quên cũng không hở.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
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
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// AllowAnonymous bắt buộc: MapStaticAssets đăng ký endpoint thật, nếu không loại trừ thì
// FallbackPolicy sẽ chặn cả CSS/JS và trang đăng nhập hiện ra không có định dạng.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
