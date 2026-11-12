using Microsoft.Data.Sqlite;

namespace Mega.Web.Data;

public sealed class DatabaseLocation(IWebHostEnvironment environment, IConfiguration configuration)
{
    public string Path { get; } = Resolve(environment, configuration);
    public string ConnectionString(bool readOnly = false) => new SqliteConnectionStringBuilder
    {
        DataSource = Path, ForeignKeys = true, Pooling = false, DefaultTimeout = 15,
        Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate
    }.ToString();

    private static string Resolve(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["MEGA:DatabasePath"];
        var file = configuration.GetValue<bool>("MEGA:DemoData") ? "mega-demo.db" : "mega.db";
        return System.IO.Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? System.IO.Path.Combine(environment.ContentRootPath, "App_Data", file)
            : System.IO.Path.IsPathRooted(configured) ? configured : System.IO.Path.Combine(environment.ContentRootPath, configured));
    }
}
