using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;

[Authorize(Policy = "Soporte")]
public sealed class SoporteModel(MegaDbContext db, Operations operations) : ManagementPage
{
    public bool Integrity { get; private set; }
    public long Schema { get; private set; }
    public string End { get; private set; } = "";
    public async Task OnGetAsync()
    {
        var user = await db.Set<Usuario>().AsNoTracking().SingleAsync(x => x.Id == UserId); End = user.FinAcceso!;
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var cmd = db.Database.GetDbConnection().CreateCommand(); cmd.CommandText = "PRAGMA quick_check";
            Integrity = (string?)await cmd.ExecuteScalarAsync() == "ok";
            cmd.CommandText = "PRAGMA foreign_key_check"; await using (var reader = await cmd.ExecuteReaderAsync()) Integrity &= !await reader.ReadAsync();
            cmd.CommandText = "PRAGMA user_version"; Schema = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }
        finally { await db.Database.CloseConnectionAsync(); }
        operations.Audit(UserId, "Consultar diagnóstico de soporte", "Sistema", null, Guid.NewGuid().ToString(), new { integridad = Integrity, esquema = Schema }); await db.SaveChangesAsync();
    }
}
