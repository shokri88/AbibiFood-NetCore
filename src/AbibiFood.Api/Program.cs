using System.Security.Claims;
using System.Text;
using AbibiFood.Api.Middleware;
using AbibiFood.Api.Services;
using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Services;
using AbibiFood.Infrastructure.Identity;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("Messages.fa.json", optional: false, reloadOnChange: true);
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("ConnectionStrings:DefaultConnection را با User Secrets یا متغیر محیطی تنظیم کنید.");
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32) throw new InvalidOperationException("Jwt:Key باید حداقل ۳۲ بایت و خارج از مخزن تنظیم شود.");
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connection));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddHttpContextAccessor(); builder.Services.AddScoped<IUserScope, CurrentUser>();
builder.Services.AddScoped<ImageUploadStore>();
builder.Services.AddScoped<CatalogService>(); builder.Services.AddSingleton<Messages>();
builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
{
    o.Password.RequiredLength = 12; o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); o.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied";
    o.Cookie.HttpOnly = true; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; o.Cookie.SameSite = SameSiteMode.Lax;
    o.Events.OnRedirectToLogin = c => { if (c.Request.Path.StartsWithSegments("/api")) c.Response.StatusCode = 401; else c.Response.Redirect(c.RedirectUri); return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { if (c.Request.Path.StartsWithSegments("/api")) c.Response.StatusCode = 403; else c.Response.Redirect(c.RedirectUri); return Task.CompletedTask; };
});
builder.Services.AddAuthentication().AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    // احراز هویت Bearer هنگام اجرای policy انجام می‌شود؛ کنترل فعال‌بودن باید همین‌جا باشد.
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var account = id is null ? null : await users.FindByIdAsync(id);
            if (account is null || !account.IsActive)
            {
                context.Fail("حساب کاربری معتبر یا فعال نیست.");
                return;
            }
            // نقش فعلی از سرور خوانده می‌شود؛ توکن قدیمی مجوز حذف‌شده را زنده نگه نمی‌دارد.
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray()) identity.RemoveClaim(claim);
                foreach (var role in await users.GetRolesAsync(account)) identity.AddClaim(new Claim(identity.RoleClaimType, role));
            }
        }
    };
});
builder.Services.AddAuthorization(o => o.AddPolicy("ManagementApi", p =>
{
    p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme); p.RequireRole("Manager", "Employee");
}));
builder.Services.AddControllersWithViews(o => o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? []).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429; o.AddPolicy("login", ctx =>
    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new()
    {
        PermitLimit = 10,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    }));
});
var app = builder.Build();
app.UseMiddleware<ErrorMiddleware>();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting(); app.UseCors(); app.UseRateLimiter(); app.UseAuthentication();
// این middleware برای کوکی پنل است؛ Bearer جداگانه در OnTokenValidated بررسی می‌شود.
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        var uid = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var db = ctx.RequestServices.GetRequiredService<AppDbContext>();
        if (!await db.Users.AnyAsync(x => x.Id == uid && x.IsActive)) { ctx.Response.StatusCode = 403; return; }
    }
    await next();
});
app.UseAuthorization();
app.MapControllers(); app.MapControllerRoute("default", "{controller=Admin}/{action=Index}/{id?}");
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" })).AllowAnonymous();
if (args.Contains("--bootstrap-manager"))
{
    using var scope = app.Services.CreateScope();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Manager", "Employee", "Customer" }) if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
    var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var email = builder.Configuration["Bootstrap:Email"]; var password = builder.Configuration["Bootstrap:Password"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Bootstrap:Email و Bootstrap:Password را خارج از مخزن تنظیم کنید.");
    var user = await users.FindByEmailAsync(email);
    if (user is null)
    {
        user = new AppUser { UserName = email, Email = email, DisplayName = "مدیر آبی‌بی" };
        var result = await users.CreateAsync(user, password); if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        var assigned = await users.AddToRoleAsync(user, "Manager"); if (!assigned.Succeeded) throw new InvalidOperationException("اختصاص نقش مدیر ناموفق بود.");
    }
    else if (!await users.IsInRoleAsync(user, "Manager")) throw new InvalidOperationException("کاربر موجود مدیر نیست؛ ارتقای ضمنی انجام نمی‌شود.");
    return;
}
await app.RunAsync();
public partial class Program { }
