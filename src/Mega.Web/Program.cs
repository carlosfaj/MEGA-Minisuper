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

var databaseCommands = args.Where(a => a is "--db-check" or "--db-upgrade").ToArray();
if (databaseCommands.Length > 1) throw new ArgumentException("Utiliza --db-check o --db-upgrade por separado.");
var builder = WebApplication.CreateBuilder(args.Except(databaseCommands).ToArray());
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
    o.AddPolicy("acceso", context => HttpMethods.IsPost(context.Request.Method)
        ? RateLimitPartition.GetFixedWindowLimiter("attempt-" + (context.Connection.RemoteIpAddress?.ToString() ?? "local"), _ => new FixedWindowRateLimiterOptions
            { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
        : RateLimitPartition.GetNoLimiter("view-" + (context.Connection.RemoteIpAddress?.ToString() ?? "local")));
    o.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Se alcanzó el límite de intentos de acceso. Espera un minuto y vuelve a intentarlo.", token);
    };
});
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<Operations>(); builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<AccessControl>(); builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddSingleton<DatabaseLocation>(); builder.Services.AddSingleton<DatabaseContract>();
builder.Services.AddSingleton<DatabaseInspector>(); builder.Services.AddScoped<DatabaseSchema>();
builder.Services.AddDbContext<MegaDbContext>((services, o) => o.UseSqlite(services.GetRequiredService<DatabaseLocation>().ConnectionString()));
var app = builder.Build();
if (databaseCommands.Length == 1)
{
    using var scope = app.Services.CreateScope();
    if (databaseCommands[0] == "--db-upgrade") await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    using var connection = new SqliteConnection(scope.ServiceProvider.GetRequiredService<DatabaseLocation>().ConnectionString(readOnly: true));
    await connection.OpenAsync();
    var report = await scope.ServiceProvider.GetRequiredService<DatabaseInspector>().InspectAsync(connection, scope.ServiceProvider.GetRequiredService<MegaDbContext>().Model);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    Environment.ExitCode = report.Healthy ? 0 : 1;
    return;
}
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
