using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Mega.Web.Services;
public sealed record RecoverySurvey(string Fingerprint,Dictionary<string,long> Rows,Dictionary<string,long> Ids,long OperationSequence);
public static class RecoverySnapshot
{
    public static async Task<RecoverySurvey> CaptureAsync(string path)
    {
        await using var connection=BackupArchive.OpenRead(path);await connection.OpenAsync();
        using var tx=connection.BeginTransaction(deferred:true);
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);Dictionary<string,long> rows=[];Dictionary<string,long> ids=[];
        var tables=new List<string>();await using(var list=connection.CreateCommand())
        {list.Transaction=tx;list.CommandText="SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";await using var reader=await list.ExecuteReaderAsync();while(await reader.ReadAsync())tables.Add(reader.GetString(0));}
        foreach(var table in tables)
        {
            var name="\""+table.Replace("\"","\"\"")+"\"";await using var command=connection.CreateCommand();command.Transaction=tx;command.CommandText=$"SELECT * FROM {name} ORDER BY 1";
            await using var reader=await command.ExecuteReaderAsync();long count=0,max=0;var fingerprint=table is not ("EventoAuditoria" or "RespaldoEjecucion");
            if(fingerprint)hash.AppendData(Encoding.UTF8.GetBytes(table+"\n"));
            while(await reader.ReadAsync())
            {
                count++;if(reader.GetName(0)=="id"&&reader.GetValue(0) is long id)max=Math.Max(max,id);
                if(fingerprint){var values=new object?[reader.FieldCount];for(var i=0;i<values.Length;i++)values[i]=reader.IsDBNull(i)?null:reader.GetValue(i);hash.AppendData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(values)+"\n"));}
            }
            rows[table]=count;ids[table]=max;
        }
        await using var sequence=connection.CreateCommand();sequence.Transaction=tx;sequence.CommandText="SELECT COALESCE(MAX(secuenciaConfirmacion),0) FROM OperacionInventario";
        return new(Convert.ToHexStringLower(hash.GetHashAndReset()),rows,ids,Convert.ToInt64(await sequence.ExecuteScalarAsync()));
    }
    public static async Task InstallAsync(string source,string destination)
    {
        await using var from=BackupArchive.OpenRead(source);await from.OpenAsync();
        await using var to=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=destination,ForeignKeys=true,Pooling=false}.ToString());await to.OpenAsync();from.BackupDatabase(to);
    }
}
