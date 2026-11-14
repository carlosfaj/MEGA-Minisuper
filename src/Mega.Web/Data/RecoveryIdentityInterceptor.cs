using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mega.Web.Data;
public sealed class RecoveryIdentityInterceptor(RecoveryGate gate):SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,InterceptionResult<int> result,CancellationToken cancellationToken=default)
    {
        if(!gate.HasIdentityFloors||eventData.Context is not {} db)return result;
        var groups=db.ChangeTracker.Entries().Where(e=>e.State==EntityState.Added).Select(e=>new{Entry=e,Key=e.Metadata.FindPrimaryKey(),Table=e.Metadata.GetTableName()})
            .Where(x=>x.Table is not null&&x.Key?.Properties.Count==1&&x.Key.Properties[0].ClrType==typeof(long)&&x.Key.Properties[0].ValueGenerated==ValueGenerated.OnAdd)
            .GroupBy(x=>x.Table!);
        foreach(var group in groups)
        {
            var entries=group.ToList();var key=entries[0].Key!.Properties[0];var column=key.GetColumnName(StoreObjectIdentifier.Table(group.Key,entries[0].Entry.Metadata.GetSchema()))!;
            var needing=entries.Where(x=>x.Entry.Property(key.Name).IsTemporary||Convert.ToInt64(x.Entry.Property(key.Name).CurrentValue)<=0).ToList();if(needing.Count==0)continue;
            var connection=db.Database.GetDbConnection();var opened=connection.State!=System.Data.ConnectionState.Open;if(opened)await db.Database.OpenConnectionAsync(cancellationToken);
            long observed;
            try{await using var command=connection.CreateCommand();command.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();command.CommandText=$"SELECT COALESCE(MAX(\"{column.Replace("\"","\"\"")}\"),0) FROM \"{group.Key.Replace("\"","\"\"")}\"";observed=Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));}
            finally{if(opened)await db.Database.CloseConnectionAsync();}
            observed=Math.Max(observed,entries.Where(x=>!x.Entry.Property(key.Name).IsTemporary).Select(x=>Convert.ToInt64(x.Entry.Property(key.Name).CurrentValue)).DefaultIfEmpty(0).Max());
            foreach(var entry in needing){var property=entry.Entry.Property(key.Name);property.CurrentValue=gate.ReserveId(group.Key,observed);property.IsTemporary=false;observed=Convert.ToInt64(property.CurrentValue);}
        }
        return result;
    }
}
