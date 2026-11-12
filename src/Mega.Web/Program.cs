using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(o => o.Conventions.AuthorizeFolder("/"));
builder.Services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; });
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Acceso"; o.AccessDeniedPath = "/SinPermiso";
    o.Cookie.Name = "MEGA.Session"; o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always; o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.Events.OnValidatePrincipal = async context =>
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<MegaDbContext>();
        var id = long.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : 0;
        var user = await db.Set<Usuario>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        var role = user?.RolId is long roleId ? await db.Set<Rol>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == roleId) : null;
        if (user is null || user.Activo != 1 || user.TipoCuenta != "comercial" ||
            user.CredencialesVersion.ToString(CultureInfo.InvariantCulture) != context.Principal?.FindFirstValue("version") ||
            role?.Nombre != context.Principal?.FindFirstValue(ClaimTypes.Role) ||
            user.PermisoOperativoCaja.ToString(CultureInfo.InvariantCulture) != context.Principal?.FindFirstValue("caja")) context.RejectPrincipal();
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Caja", p => p.RequireAuthenticatedUser().RequireClaim("caja", "1").RequireRole("Cajero", "Administrador", "Propietario"));
    o.AddPolicy("Inventario", p => p.RequireRole("Inventario", "Administrador", "Propietario"));
    o.AddPolicy("Gestion", p => p.RequireRole("Administrador", "Propietario"));
});
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("acceso", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
    { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<Operations>(); builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddDbContext<MegaDbContext>(o =>
{
    var file = builder.Configuration.GetValue<bool>("MEGA:DemoData") ? "mega-demo.db" : "mega.db";
    var path = builder.Configuration["MEGA:DatabasePath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", file);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    o.UseSqlite(new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true, Pooling = false, DefaultTimeout = 15 }.ToString());
});
var app = builder.Build();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
app.Use(async (context, next) =>
{
    if (context.Connection.RemoteIpAddress is { } remote && !IPAddress.IsLoopback(remote)) { context.Response.StatusCode = 403; return; }
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    context.Response.Headers["Referrer-Policy"] = "no-referrer"; context.Response.Headers.CacheControl = "no-store";
    await next();
});
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting(); app.UseRateLimiter();
app.UseAuthentication(); app.UseAuthorization(); app.MapRazorPages(); app.Run();
public partial class Program { }
