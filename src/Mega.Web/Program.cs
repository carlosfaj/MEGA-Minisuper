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
builder.Services.AddRazorPages(o => o.Conventions.AuthorizeFolder("/")).AddMvcOptions(o=>o.Filters.Add<RejectionPageFilter>());
builder.Services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; });
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Acceso"; o.AccessDeniedPath = "/SinPermiso";
    o.Cookie.Name = "MEGA.Session"; o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always; o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.Events.OnValidatePrincipal = async context =>
    {
        if (!await context.HttpContext.RequestServices.GetRequiredService<SessionAccess>().ValidateAsync(context.Principal)) context.RejectPrincipal();
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Caja", p => p.RequireAuthenticatedUser().RequireClaim("caja", "1").RequireRole("Cajero", "Administrador", "Propietario"));
    o.AddPolicy("Inventario", p => p.RequireRole("Inventario", "Administrador", "Propietario"));
    o.AddPolicy("Gestion", p => p.RequireRole("Administrador", "Propietario"));
    o.AddPolicy("Cuentas", p => p.RequireAuthenticatedUser().RequireAssertion(c => c.User.IsInRole("Propietario") || c.User.IsInRole("Administrador") && c.User.HasClaim("cuentas", "1")));
    o.AddPolicy("Comercial", p => p.RequireClaim("tipo", "comercial"));
    o.AddPolicy("Soporte", p => p.RequireClaim("tipo", "soporte_temporal"));
    o.AddPolicy("Anulaciones",p=>p.RequireRole("Cajero","Administrador","Propietario"));
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
        await context.HttpContext.RequestServices.GetRequiredService<RejectionAuditWriter>().WriteAsync(context.HttpContext,"Se alcanzó el límite de intentos de acceso.");
    };
});
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<Operations>(); builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<AccessControl>(); builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<AccountService>(); builder.Services.AddScoped<SessionAccess>();
builder.Services.AddScoped<ReceivingService>();
builder.Services.AddScoped<CountingService>();
builder.Services.AddScoped<InventoryRequestService>();
builder.Services.AddScoped<ValuationService>();
builder.Services.AddSingleton<ProjectBoardService>();
builder.Services.AddScoped<CommercialService>(); builder.Services.AddScoped<CommercialCalculator>();
builder.Services.AddScoped<CancellationService>();
builder.Services.AddSingleton<RejectionAuditWriter>();
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
app.UseAuthentication(); app.UseMiddleware<RejectionAuditMiddleware>(); app.UseAuthorization(); app.MapRazorPages(); app.Run();
public partial class Program { }
