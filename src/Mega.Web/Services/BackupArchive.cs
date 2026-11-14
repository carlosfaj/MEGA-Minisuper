using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mega.Web.Data;
using Microsoft.Data.Sqlite;

namespace Mega.Web.Services;
public static class AppRelease{public static string Version=>typeof(AppRelease).Assembly.GetName().Version!.ToString(3);}
public sealed record BackupEntry(string Name,long Bytes,string Hash);
public sealed record RecoveryPoint(string StartedUtc,string FinishedUtc,long LastSale,long LastOperation,long LastMovement,long LastCashMovement,long LastClosedShift,long LastAudit,Dictionary<string,long> Rows);
public sealed record BackupManifest(int Format,long ExecutionId,string AppVersion,int SchemaVersion,RecoveryPoint Point,List<BackupEntry> Files,List<string> Warnings,bool Preservation=false,Dictionary<string,long>? IdentityFloors=null,long OperationFloor=0);
public sealed record BackupArtifact(string Path,string Checksum,long Bytes,BackupManifest Manifest);
public sealed class BackupWorkspace:IDisposable
{
    private readonly string parent;
    public string Path{get;}
    public BackupWorkspace(OperationalFiles files){parent=System.IO.Path.GetFullPath(System.IO.Path.Combine(files.Root,"Temporales"));Path=System.IO.Path.Combine(parent,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path);}
    public void Dispose()
    {
        var resolved=System.IO.Path.GetFullPath(Path);
        if(System.IO.Path.GetDirectoryName(resolved)!=parent||!Guid.TryParseExact(System.IO.Path.GetFileName(resolved),"N",out _))throw new InvalidOperationException("Directorio temporal fuera del ámbito previsto.");
        try{if(Directory.Exists(resolved))Directory.Delete(resolved,true);}catch(IOException){}catch(UnauthorizedAccessException){}
    }
}
public sealed class BackupArchive(DatabaseLocation location,OperationalFiles files,DatabaseInspector inspector,RecoveryGate recovery)
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web){WriteIndented=true};
    public static async Task<string> FileHashAsync(string path){await using var stream=File.OpenRead(path);return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream));}
    public async Task<BackupArtifact> CreateAsync(string destination,long executionId,bool preservation=false)
    {
        using var work=new BackupWorkspace(files);var database=Path.Combine(work.Path,"base.db");var began=Formats.Now();
        await using(var source=new SqliteConnection(location.ConnectionString(readOnly:true)))
        await using(var copy=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,ForeignKeys=true,Pooling=false}.ToString()))
        {await source.OpenAsync();await copy.OpenAsync();source.BackupDatabase(copy);}
        var finished=Formats.Now();var items=new Dictionary<string,string>(StringComparer.Ordinal){{"base.db",database}};List<string> warnings=[];DatabaseReport report;RecoveryPoint point;
        await using(var snapshot=OpenRead(database))
        {
            await snapshot.OpenAsync();report=await inspector.InspectAsync(snapshot);DatabaseInspector.RequireHealthy(report);
            if(!report.UpToDate)throw new BusinessException("Actualiza el esquema antes de generar este respaldo operativo.");
            var refs=await QueryAsync(snapshot,"SELECT DISTINCT reporteReferencia,reporteHash FROM SolicitudCorreo");
            foreach(var entry in refs)
            {var path=files.ReportPath(entry[0]);if(!File.Exists(path)||await FileHashAsync(path)!=entry[1]){if(!preservation)throw new BusinessException("Un reporte asociado falta o no coincide con su huella. No se comprobó la copia.");warnings.Add("Reporte ausente o alterado: "+entry[0]);if(!File.Exists(path))continue;}items["archivos/Reportes/"+entry[0]]=path;}
            foreach(var entry in await QueryAsync(snapshot,"SELECT DISTINCT smtpSecretoReferencia FROM ConfiguracionNegocio WHERE smtpSecretoReferencia IS NOT NULL"))
            {if(!Guid.TryParseExact(entry[0],"N",out _))throw new BusinessException("Una referencia de credencial requiere revisión.");var path=Path.Combine(files.Secrets,entry[0]+".bin");if(!File.Exists(path)){if(!preservation)throw new BusinessException("Falta una credencial protegida asociada. No se comprobó la copia.");warnings.Add("Credencial protegida ausente: "+Path.GetFileName(path));continue;}items["archivos/Secretos/"+entry[0]+".bin"]=path;}
            foreach(var entry in await QueryAsync(snapshot,"SELECT DISTINCT evidencia FROM IntentoImpresion WHERE evidencia LIKE 'Salida virtual PDF: %' AND resultado='aceptada_SO'"))
            {
                var path=Path.GetFullPath(entry[0]["Salida virtual PDF: ".Length..]);var folder=Path.GetFullPath(Path.Combine(files.Root,"Impresiones"))+Path.DirectorySeparatorChar;
                if(!path.StartsWith(folder,StringComparison.OrdinalIgnoreCase)){warnings.Add("Salida PDF histórica fuera del almacén administrado; se conserva su referencia en la base.");continue;}
                if(!File.Exists(path)){if(!preservation)throw new BusinessException("Falta una salida PDF administrada asociada. No se comprobó la copia.");warnings.Add("Salida PDF administrada ausente: "+Path.GetFileName(path));continue;}items["archivos/Impresiones/"+Path.GetFileName(path)]=path;
            }
            point=new(began,finished,await MaxAsync(snapshot,"Venta"),await MaxAsync(snapshot,"OperacionInventario"),await MaxAsync(snapshot,"MovimientoInventario"),await MaxAsync(snapshot,"MovimientoCaja"),await MaxAsync(snapshot,"TurnoCaja","WHERE estado='Cerrado'"),await MaxAsync(snapshot,"EventoAuditoria"),report.Tables.ToDictionary(t=>t.Name,t=>t.Rows));
        }
        if(!preservation&&Directory.Exists(Path.Combine(files.Root,"Preservaciones")))foreach(var path in Directory.GetFiles(Path.Combine(files.Root,"Preservaciones"),"*.zip"))items["archivos/Preservaciones/"+Path.GetFileName(path)]=path;
        var entries=new List<BackupEntry>();foreach(var (name,path) in items){RequireEntryName(name);entries.Add(new(name,new FileInfo(path).Length,await FileHashAsync(path)));}
        var manifest=new BackupManifest(2,executionId,AppRelease.Version,report.SchemaVersion,point,entries,warnings.Distinct().ToList(),preservation,recovery.State.IdentityFloors,recovery.State.OperationFloor);
        await using(var output=new FileStream(destination,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None))
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            foreach(var entry in entries)
            {await using var stream=zip.CreateEntry(entry.Name,CompressionLevel.Optimal).Open();await using var input=File.OpenRead(items[entry.Name]);await input.CopyToAsync(stream);}
            await using var meta=zip.CreateEntry("manifiesto.json",CompressionLevel.Optimal).Open();await JsonSerializer.SerializeAsync(meta,manifest,JsonOptions);
        }
        var checksum=await FileHashAsync(destination);var verified=await VerifyAsync(destination,checksum);return new(destination,checksum,new FileInfo(destination).Length,verified);
    }
    public async Task<BackupManifest> VerifyAsync(string path,string? expectedHash=null)
    {
        using var work=new BackupWorkspace(files);return await ExtractVerifiedAsync(path,work.Path,expectedHash);
    }
    public async Task<BackupManifest> ExtractVerifiedAsync(string path,string destination,string? expectedHash=null)
    {
        await using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);if(input.Length>1040L*1024*1024)throw new BusinessException("La copia excede el tamaño admitido.");var hash=Convert.ToHexStringLower(await SHA256.HashDataAsync(input));
        if(expectedHash is not null&&hash!=expectedHash)throw new BusinessException("La huella de la copia no coincide con el registro comprobado.");input.Position=0;
        using var zip=new ZipArchive(input,ZipArchiveMode.Read,true);
        if(zip.Entries.Count is <2 or >10001||zip.Entries.Sum(e=>e.Length)>1024L*1024*1024||zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=zip.Entries.Count)throw new BusinessException("La copia excede el formato admitido o contiene nombres duplicados.");
        var manifestEntry=zip.GetEntry("manifiesto.json")??throw new BusinessException("Falta el manifiesto de la copia.");if(manifestEntry.Length>4*1024*1024)throw new BusinessException("El manifiesto excede el tamaño admitido.");
        BackupManifest manifest;
        try{await using var meta=manifestEntry.Open();manifest=await JsonSerializer.DeserializeAsync<BackupManifest>(meta,JsonOptions)??throw new JsonException();}
        catch(JsonException){throw new BusinessException("El manifiesto no tiene un formato válido.");}
        if(manifest.Format is not (1 or 2)||manifest.SchemaVersion!=DatabaseContract.CurrentVersion||manifest.Files is null||manifest.Point is null||manifest.Warnings is null||manifest.Files.Any(x=>x is null)||manifest.Files.Count+1!=zip.Entries.Count||manifest.Files.Count(x=>x.Name=="base.db")!=1||manifest.Files.Select(x=>x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=manifest.Files.Count||!DateTimeOffset.TryParse(manifest.Point.StartedUtc,out var began)||!DateTimeOffset.TryParse(manifest.Point.FinishedUtc,out var finished)||finished<began)throw new BusinessException("La versión o el contenido del respaldo no son compatibles.");
        var root=Path.GetFullPath(destination)+Path.DirectorySeparatorChar;Directory.CreateDirectory(destination);
        foreach(var entry in manifest.Files)
        {
            RequireEntryName(entry.Name);var item=zip.GetEntry(entry.Name)??throw new BusinessException("Falta un archivo declarado en el manifiesto.");
            if(item.Length!=entry.Bytes||entry.Hash is null||!Regex.IsMatch(entry.Hash,"^[0-9a-f]{64}$"))throw new BusinessException("Los metadatos de un archivo no coinciden.");
            var target=Path.GetFullPath(Path.Combine(destination,entry.Name.Replace('/',Path.DirectorySeparatorChar)));if(!target.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new BusinessException("La copia intenta escribir fuera del directorio de revisión.");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);await using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write)){await using var content=item.Open();var buffer=new byte[81920];long written=0;int read;while((read=await content.ReadAsync(buffer))>0){written=checked(written+read);if(written>entry.Bytes)throw new BusinessException("El archivo supera el tamaño declarado.");await output.WriteAsync(buffer.AsMemory(0,read));}if(written!=entry.Bytes)throw new BusinessException("El archivo está incompleto.");}
            if(await FileHashAsync(target)!=entry.Hash)throw new BusinessException("Un archivo no coincide con su huella del manifiesto.");
        }
        await using var snapshot=OpenRead(Path.Combine(destination,"base.db"));await snapshot.OpenAsync();var report=await inspector.InspectAsync(snapshot);if(!report.Healthy)throw new BusinessException("El diagnóstico de integridad de la copia no fue satisfactorio.");if(report.SchemaVersion!=manifest.SchemaVersion)throw new BusinessException("La versión de la base no coincide con el manifiesto.");
        var p=manifest.Point;if(p.Rows is null||p.Rows.Count!=report.Tables.Count||report.Tables.Any(t=>!p.Rows.TryGetValue(t.Name,out var rows)||rows!=t.Rows)||p.LastSale!=await MaxAsync(snapshot,"Venta")||p.LastOperation!=await MaxAsync(snapshot,"OperacionInventario")||p.LastMovement!=await MaxAsync(snapshot,"MovimientoInventario")||p.LastCashMovement!=await MaxAsync(snapshot,"MovimientoCaja")||p.LastClosedShift!=await MaxAsync(snapshot,"TurnoCaja","WHERE estado='Cerrado'")||p.LastAudit!=await MaxAsync(snapshot,"EventoAuditoria"))throw new BusinessException("El punto de recuperación no coincide con los datos de la copia.");
        if(manifest.IdentityFloors?.Any(x=>x.Value<0||!report.Tables.Any(t=>t.Name==x.Key))==true||manifest.OperationFloor<0)throw new BusinessException("La continuidad de numeración no es válida.");
        if(!manifest.Preservation)
        {
            foreach(var entry in await QueryAsync(snapshot,"SELECT DISTINCT reporteReferencia,reporteHash FROM SolicitudCorreo"))
                if(!manifest.Files.Any(f=>f.Name=="archivos/Reportes/"+entry[0]&&f.Hash==entry[1]))throw new BusinessException("Falta un reporte asociado válido en la copia.");
            foreach(var entry in await QueryAsync(snapshot,"SELECT DISTINCT smtpSecretoReferencia FROM ConfiguracionNegocio WHERE smtpSecretoReferencia IS NOT NULL"))
                if(!manifest.Files.Any(f=>f.Name=="archivos/Secretos/"+entry[0]+".bin"))throw new BusinessException("Falta una credencial protegida asociada en la copia.");
        }
        return manifest;
    }
    private static void RequireEntryName(string name)
    {
        if(name=="base.db")return;if(name is null)throw new BusinessException("Falta una referencia de archivo del respaldo.");
        if(!Regex.IsMatch(name,"^archivos/(Reportes/[0-9a-f]{32}\\.(pdf|xlsx)|Secretos/[0-9a-f]{32}\\.bin|Preservaciones/[0-9a-f]{32}\\.zip|Impresiones/comprobante-[0-9]+-[0-9a-f]{32}\\.pdf)$"))throw new BusinessException("El respaldo contiene una referencia de archivo no admitida.");
    }
    public static SqliteConnection OpenRead(string path)=>new(new SqliteConnectionStringBuilder{DataSource=path,Mode=SqliteOpenMode.ReadOnly,ForeignKeys=true,Pooling=false}.ToString());
    private static async Task<long> MaxAsync(SqliteConnection connection,string table,string where="")
    {await using var command=connection.CreateCommand();command.CommandText=$"SELECT COALESCE(MAX(id),0) FROM \"{table}\" {where}";return Convert.ToInt64(await command.ExecuteScalarAsync());}
    private static async Task<List<string[]>> QueryAsync(SqliteConnection connection,string sql)
    {await using var command=connection.CreateCommand();command.CommandText=sql;await using var reader=await command.ExecuteReaderAsync();List<string[]> rows=[];while(await reader.ReadAsync()){var row=new string[reader.FieldCount];for(var i=0;i<row.Length;i++)row[i]=reader.GetString(i);rows.Add(row);}return rows;}
}
