using System.Security.Cryptography;
using System.Text.Json;
using Mega.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;
public sealed record BackupPolicy(int Format,string Destination,string Medium,bool Periodic,int? Minutes,bool OnClose,bool BeforeUpdate);
public sealed record BackupPolicyChange(long Id,long Version,string Destination,string Medium,bool Periodic,int Minutes,bool OnClose,bool BeforeUpdate);
public sealed class BackupService(MegaDbContext db,AccessControl access,Operations operations,SettingsService settings,BackupArchive archive,OperationalFiles files,ILogger<BackupService> logger)
{
    public static BackupPolicy? Policy(ConfiguracionNegocio config)
    {
        if(config.PautaRespaldo is null)return null;
        try{var p=JsonSerializer.Deserialize<BackupPolicy>(config.PautaRespaldo,new JsonSerializerOptions(JsonSerializerDefaults.Web));if(p is null||p.Format!=1||string.IsNullOrWhiteSpace(p.Destination)||string.IsNullOrWhiteSpace(p.Medium)||(p.Periodic||p.OnClose)&&(p.Minutes is null or <2 or >1440))throw new JsonException();return p;}
        catch(JsonException){throw new BusinessException("La pauta de respaldo requiere revisión y autorización del Propietario.");}
    }
    public async Task SavePolicyAsync(long userId,BackupPolicyChange input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.RequireAsync(userId,"Propietario");var current=await settings.CurrentAsync();if(current.Id!=input.Id)throw new BusinessException("La configuración cambió. Recarga antes de autorizar la pauta.");AccessControl.Version(current.Version,input.Version);
        var destination=Destination(input.Destination);if(!Directory.Exists(destination))throw new BusinessException("La carpeta no está disponible. Conecta el medio y selecciona una carpeta existente.");
        var medium=AccessControl.Text(input.Medium,"Identificación del medio",100);if((input.Periodic||input.OnClose)&&input.Minutes is <2 or >1440)throw new BusinessException("Selecciona un intervalo entre 2 y 1440 minutos.");
        var policy=new BackupPolicy(1,destination,medium,input.Periodic,input.Periodic||input.OnClose?input.Minutes:null,input.OnClose,input.BeforeUpdate);
        var old=await db.Set<ConfiguracionNegocio>().SingleAsync(x=>x.Id==current.Id);var next=(ConfiguracionNegocio)db.Entry(old).CurrentValues.ToObject();var now=Formats.Now();if(string.CompareOrdinal(now,old.VigenteDesde)<=0){await Task.Delay(2);now=Formats.Now();}if(string.CompareOrdinal(now,old.VigenteDesde)<=0)throw new BusinessException("Vuelve a intentar autorizar la pauta.");old.VigenteHasta=now;await db.SaveChangesAsync();
        next.Id=0;next.Version=checked(current.Version+1);next.VigenteDesde=now;next.VigenteHasta=null;next.CambiadoPor=userId;next.PautaRespaldo=Formats.Json(policy);next.PautaAutorizadaPor=userId;next.PautaAutorizadaEn=now;db.Add(next);await db.SaveChangesAsync();operations.Audit(userId,"Autorizar pauta de respaldo","ConfiguracionNegocio",next.Id.ToString(),Guid.NewGuid().ToString(),new{anteriorId=current.Id,pauta=policy});await db.SaveChangesAsync();await tx.CommitAsync();
    }
    private string Destination(string value)
    {
        var text=AccessControl.Text(value,"Carpeta de respaldo",1000);if(!Path.IsPathFullyQualified(text))throw new BusinessException("Selecciona una ruta completa de carpeta.");var path=Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));var root=Path.GetFullPath(files.Root);
        if(path.Equals(root,StringComparison.OrdinalIgnoreCase)||path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new BusinessException("La copia debe guardarse fuera del almacén de archivos de operación.");return path;
    }
    public async Task<long> RunAsync(long? userId,string key,string origin="manual")
    {
        if(origin is not ("manual" or "preactualizacion" or "cierre" or "pauta_automatica"))throw new BusinessException("Origen de respaldo no válido.");
        if(userId is long actor)await access.ManagerAsync(actor);else if(origin=="manual")throw new BusinessException("La copia manual requiere una cuenta de gestión.");
        if(!Guid.TryParseExact(key,"D",out _))throw new BusinessException("Recarga la solicitud de respaldo.");
        var config=await settings.CurrentAsync();var policy=Policy(config)??throw new BusinessException("El Propietario debe autorizar un destino y una pauta antes de copiar.");
        RespaldoEjecucion row;
        await using(var tx=await db.Database.BeginTransactionAsync())
        {
            var existing=await db.Set<RespaldoEjecucion>().AsNoTracking().SingleOrDefaultAsync(x=>x.ClaveEjecucion==key);if(existing is not null){if(existing.SolicitadoPor!=userId||existing.Origen!=origin)throw new BusinessException("La clave pertenece a otra ejecución.");await tx.CommitAsync();return existing.Id;}
            row=new(){ClaveEjecucion=key,Inicio=Formats.Now(),SolicitadoPor=userId,EjecutadoPor=userId,Origen=origin,Destino=policy.Destination,MedioIdentificador=policy.Medium,Estado="En_proceso",ConfiguracionId=config.Id};
            db.Add(row);await db.SaveChangesAsync();Audit(userId,"Iniciar respaldo",row.Id,"pendiente",new{origen=origin,row.Destino,row.MedioIdentificador});await db.SaveChangesAsync();await tx.CommitAsync();
        }
        try
        {
            if(config.PautaAutorizadaPor is not long owner)throw new BusinessException("La pauta no tiene autorización vigente del Propietario.");await access.RequireAsync(owner,"Propietario");var directory=Destination(policy.Destination);if(!Directory.Exists(directory))throw new BusinessException("El medio o carpeta no están disponibles. La última copia comprobada se conserva.");
            var path=Path.Combine(directory,$"MEGA-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.Parse(key):N}.zip");row.ArchivoReferencia=path;await db.SaveChangesAsync();var artifact=await archive.CreateAsync(path,row.Id);
            row.Fin=Formats.Now();row.Estado="Comprobada";row.Checksum=artifact.Checksum;row.TamanoBytes=artifact.Bytes;row.Manifiesto=Formats.Json(artifact.Manifest);row.VersionEsquema=artifact.Manifest.SchemaVersion.ToString();row.UltimaOperacionId=artifact.Manifest.Point.LastOperation==0?null:artifact.Manifest.Point.LastOperation;row.UltimoMovimientoId=artifact.Manifest.Point.LastMovement==0?null:artifact.Manifest.Point.LastMovement;row.ComprobadoEn=row.Fin;row.UltimoError=null;
            Audit(userId,"Comprobar respaldo generado",row.Id,"exito",new{row.Checksum,row.TamanoBytes,punto=artifact.Manifest.Point,advertencias=artifact.Manifest.Warnings});await db.SaveChangesAsync();return row.Id;
        }
        catch(Exception error)
        {
            logger.LogError(error,"No se completó la ejecución de respaldo {Id}",row.Id);
            db.ChangeTracker.Clear();row=await db.Set<RespaldoEjecucion>().SingleAsync(x=>x.Id==row.Id);row.Fin=Formats.Now();row.Estado="Fallida";row.UltimoError=ErrorText(error);Audit(userId,"Fallo de respaldo",row.Id,"fallo",new{motivo=row.UltimoError});await db.SaveChangesAsync();return row.Id;
        }
    }
    public async Task<bool> VerifyAsync(long userId,long id)
    {
        await access.ManagerAsync(userId);var row=await db.Set<RespaldoEjecucion>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("La copia no existe.");
        if(row.ArchivoReferencia is null||row.Checksum is null)throw new BusinessException("La ejecución no tiene un archivo previamente comprobado.");
        try{var manifest=await archive.VerifyAsync(row.ArchivoReferencia,row.Checksum);row.Estado="Comprobada";row.ComprobadoEn=Formats.Now();row.UltimoError=null;Audit(userId,"Comprobar copia existente",row.Id,"exito",new{row.Checksum,punto=manifest.Point});await db.SaveChangesAsync();return true;}
        catch(Exception error)
        {
            row.UltimoError=ErrorText(error);if(error is BusinessException or InvalidDataException){row.Estado="Fallida";row.Fin??=Formats.Now();}Audit(userId,"Fallo al comprobar copia",row.Id,"fallo",new{motivo=row.UltimoError,estadoConservado=row.Estado});await db.SaveChangesAsync();return false;
        }
    }
    public async Task<(FileStream Stream,string Name)> DownloadAsync(long userId,long id)
    {
        await access.ManagerAsync(userId);var row=await db.Set<RespaldoEjecucion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.Estado=="Comprobada")??throw new BusinessException("Selecciona una copia comprobada.");
        if(row.ArchivoReferencia is null)throw new BusinessException("Falta la referencia del archivo.");var stream=new FileStream(row.ArchivoReferencia,FileMode.Open,FileAccess.Read,FileShare.Read);
        try{if(Convert.ToHexStringLower(await SHA256.HashDataAsync(stream))!=row.Checksum)throw new BusinessException("La copia ya no coincide con su huella. Compruébala antes de descargar.");stream.Position=0;return(stream,Path.GetFileName(row.ArchivoReferencia));}catch{await stream.DisposeAsync();throw;}
    }
    public async Task<string?> DueOriginAsync()
    {
        var config=await settings.CurrentAsync();var policy=Policy(config);if(policy is null||!policy.Periodic&&!policy.OnClose)return null;
        var last=await db.Set<RespaldoEjecucion>().AsNoTracking().OrderByDescending(x=>x.Id).FirstOrDefaultAsync();var now=DateTimeOffset.UtcNow;var due=last is null||last.ConfiguracionId!=config.Id||now-DateTimeOffset.Parse(last.Inicio)>=TimeSpan.FromMinutes(policy.Minutes!.Value);
        if(policy.OnClose)
        {
            var closed=await db.Set<TurnoCaja>().AsNoTracking().Where(x=>x.Estado=="Cerrado"&&string.Compare(x.Cierre,config.VigenteDesde)>=0).OrderByDescending(x=>x.Id).FirstOrDefaultAsync();
            var valid=await db.Set<RespaldoEjecucion>().AsNoTracking().Where(x=>x.Estado=="Comprobada").OrderByDescending(x=>x.Id).FirstOrDefaultAsync();long copied=0;
            if(valid?.Manifiesto is {} json)try{copied=JsonSerializer.Deserialize<BackupManifest>(json,new JsonSerializerOptions(JsonSerializerDefaults.Web))?.Point.LastClosedShift??0;}catch(JsonException){}
            if(closed is not null&&closed.Id>copied&&(due||last is null||string.CompareOrdinal(closed.Cierre,last.Inicio)>0))return "cierre";
        }
        return policy.Periodic&&due?"pauta_automatica":null;
    }
    private void Audit(long? userId,string action,long id,string result,object data)
    {if(userId is long user)operations.Audit(user,action,"RespaldoEjecucion",id.ToString(),Guid.NewGuid().ToString(),data,result);else db.Add(new EventoAuditoria{Fecha=Formats.Now(),Origen="tarea_automatica",Accion=action,Entidad="RespaldoEjecucion",EntidadId=id.ToString(),ClaveOperacion=Guid.NewGuid().ToString(),Resultado=result,DatosCambio=Formats.Json(data),VersionFormato=1});}
    private static string ErrorText(Exception error)=>error switch{BusinessException b=>b.Message,UnauthorizedAccessException=>"Sin permiso para leer o escribir en el medio seleccionado.",IOException io when io.HResult==unchecked((int)0x80070070)=>"El medio no tiene espacio suficiente. La última copia comprobada se conserva.",IOException=>"No se pudo acceder, escribir o comprobar la copia en el medio. Revisa conexión, espacio y permisos; la última copia comprobada se conserva.",_=>"La copia no superó el diagnóstico o no pudo registrarse. Revisa el medio y los registros antes de intentar de nuevo."};
}
public sealed class BackupDispatcher(IServiceScopeFactory scopes,DatabaseLocation location,OperationalFiles files,ILogger<BackupDispatcher> logger)
{
    private readonly SemaphoreSlim gate=new(1,1);
    public async Task<long> ManualAsync(long userId,string key)
    {await gate.WaitAsync();try{await using var s=scopes.CreateAsyncScope();return await s.ServiceProvider.GetRequiredService<BackupService>().RunAsync(userId,key);}finally{gate.Release();}}
    public async Task<bool> TickAsync()
    {
        if(!await gate.WaitAsync(0))return false;
        try{await using var s=scopes.CreateAsyncScope();var service=s.ServiceProvider.GetRequiredService<BackupService>();var origin=await service.DueOriginAsync();if(origin is null)return false;await service.RunAsync(null,Guid.NewGuid().ToString(),origin);return true;}finally{gate.Release();}
    }
    public async Task BeforeUpdateAsync()
    {
        if(!File.Exists(location.Path))return;var marker=Path.Combine(files.Root,"VersionAplicada.txt");if(File.Exists(marker)&&File.ReadAllText(marker).Trim()==AppRelease.Version)return;
        await using(var connection=BackupArchive.OpenRead(location.Path)){await connection.OpenAsync();await using var version=connection.CreateCommand();version.CommandText="PRAGMA user_version";if(Convert.ToInt32(await version.ExecuteScalarAsync())!=DatabaseContract.CurrentVersion)return;}
        await using var scope=scopes.CreateAsyncScope();var config=await scope.ServiceProvider.GetRequiredService<SettingsService>().CurrentAsync();BackupPolicy? policy;try{policy=BackupService.Policy(config);}catch(BusinessException){logger.LogWarning("La pauta previa a actualización requiere revisión del Propietario; no se generó una copia externa.");return;}if(policy?.BeforeUpdate!=true)return;
        await gate.WaitAsync();try{await scope.ServiceProvider.GetRequiredService<BackupService>().RunAsync(null,Guid.NewGuid().ToString(),"preactualizacion");}finally{gate.Release();}
    }
    public void RecordVersion()
    {try{Directory.CreateDirectory(files.Root);File.WriteAllText(Path.Combine(files.Root,"VersionAplicada.txt"),AppRelease.Version);}catch(Exception error)when(error is IOException or UnauthorizedAccessException){logger.LogWarning("No se pudo guardar la referencia local de versión; revisa permisos del almacén de archivos.");}}
}
public sealed class BackupWorker(BackupDispatcher dispatcher,IWebHostEnvironment environment,IConfiguration configuration,ILogger<BackupWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(environment.IsEnvironment("Testing")||!configuration.GetValue("MEGA:BackupAgentEnabled",true))return;using var timer=new PeriodicTimer(TimeSpan.FromSeconds(30));
        try{while(await timer.WaitForNextTickAsync(stoppingToken))try{await dispatcher.TickAsync();}catch(Exception error){logger.LogError(error,"No se pudo ejecutar la pauta de respaldo. Revisa el destino y la autorización.");}}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){}
    }
}
