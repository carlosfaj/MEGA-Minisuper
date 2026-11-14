using System.Net;
using System.Text.Json;

namespace Mega.Web.Services;
public sealed record RecoveryControl(int Format,string Mode,string Epoch,Dictionary<string,long> IdentityFloors,long OperationFloor,
    string? Key=null,long? RestorationId=null,long? OwnerId=null,string? OwnerAccount=null,string? ArchivePath=null,string? ArchiveHash=null,
    string? PreservationFile=null,string? PreservationHash=null,string? StartedUtc=null,string? Phase=null,string? Message=null);

public sealed class RecoveryGate
{
    public const string LeaseKey="MEGA.DatabaseLease";
    private readonly object sync=new();private readonly SemaphoreSlim writer=new(1,1);private readonly string path;
    private RecoveryControl state;private int readers;private bool exclusive;private TaskCompletionSource<bool>? drained;
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web){WriteIndented=true};
    private readonly ILogger<RecoveryGate>? logger;
    public RecoveryGate(OperationalFiles files,ILogger<RecoveryGate>? logger=null)
    {
        this.logger=logger;
        path=Path.Combine(files.Root,"Recuperacion.json");state=new(1,"normal",Guid.NewGuid().ToString(),[],0);
        try
        {
            if(File.Exists(path))
            {
                if(new FileInfo(path).Length>1024*1024)throw new JsonException();
                var saved=JsonSerializer.Deserialize<RecoveryControl>(File.ReadAllText(path),Json);
                if(saved is null||saved.Format!=1||saved.Mode is not ("normal" or "restaurando" or "conciliar" or "fallo")||!Guid.TryParse(saved.Epoch,out _)||saved.IdentityFloors is null||saved.IdentityFloors.Count>100||saved.IdentityFloors.Any(x=>x.Value<0)||saved.OperationFloor<0)throw new JsonException();state=saved;
            }
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or JsonException){state=state with{Mode="fallo",Message="El control de recuperación requiere revisión técnica. Se mantiene pausada la operación."};}
    }
    public RecoveryControl State{get{lock(sync)return state with{IdentityFloors=new(state.IdentityFloors)};}}
    public string Epoch{get{lock(sync)return state.Epoch;}}
    public bool DatabaseSafe{get{lock(sync)return state.Mode is "normal" or "conciliar";}}
    public bool HasIdentityFloors{get{lock(sync)return state.IdentityFloors.Count>0;}}
    public void EnsurePersisted(){lock(sync)if(!File.Exists(path))Store(state);}
    public IDisposable? TryWorker()=>TryRead(null);
    public IDisposable? TryHttp(string requestPath)=>TryRead(requestPath);
    private IDisposable? TryRead(string? requestPath)
    {
        lock(sync)
        {
            if(exclusive||state.Mode is "restaurando" or "fallo"||state.Mode=="conciliar"&&!RecoveryPath(requestPath))return null;
            readers++;return new ReadLease(this);
        }
    }
    private static bool RecoveryPath(string? path)=>path is "/Acceso" or "/Salir" or "/Recuperacion" or "/Recuperacion/Estado";
    private void Leave(){lock(sync){readers--;if(readers==0)drained?.TrySetResult(true);}}
    public async Task<IDisposable> EnterExclusiveAsync(HttpContext? context=null,CancellationToken cancellation=default)
    {
        if(!await writer.WaitAsync(0,cancellation))throw new BusinessException("Ya hay una recuperación en curso.");
        Task wait;
        lock(sync){exclusive=true;drained=new(TaskCreationOptions.RunContinuationsAsynchronously);if(readers==0)drained.TrySetResult(true);wait=drained.Task;}
        if(context?.Items[LeaseKey] is IDisposable own){own.Dispose();context.Items.Remove(LeaseKey);}
        try{await wait.WaitAsync(TimeSpan.FromMinutes(2),cancellation);return new ExclusiveLease(this,context);}
        catch{EndExclusive(context);throw new BusinessException("No se pudieron pausar todos los procesos. Espera a que finalicen antes de recuperar; no se reemplazaron datos.");}
    }
    private void EndExclusive(HttpContext? context=null)
    {
        lock(sync)
        {
            exclusive=false;drained=null;
            if(context is not null&&state.Mode is "normal" or "conciliar"&&(state.Mode!="conciliar"||RecoveryPath(context.Request.Path.Value)))
            {readers++;context.Items[LeaseKey]=new ReadLease(this);}
        }
        writer.Release();
    }
    public void Begin(string key,long ownerId,string ownerAccount,string archivePath,string archiveHash,Dictionary<string,long> floors,long operationFloor)
    {
        lock(sync)
        {
            var merged=new Dictionary<string,long>(state.IdentityFloors);foreach(var (name,value) in floors)merged[name]=Math.Max(merged.GetValueOrDefault(name),value);
            Store(state with{Mode="restaurando",Key=key,RestorationId=null,OwnerId=ownerId,OwnerAccount=ownerAccount,ArchivePath=archivePath,ArchiveHash=archiveHash,PreservationFile=null,PreservationHash=null,StartedUtc=Formats.Now(),Phase="preparando",Message="Recuperación autorizada en preparación.",IdentityFloors=merged,OperationFloor=Math.Max(state.OperationFloor,operationFloor)});
        }
    }
    public void Preserved(string reference,string hash){lock(sync)Store(state with{PreservationFile=reference,PreservationHash=hash,Phase="preservada",Message="Estado anterior preservado y comprobado."});}
    public void Installing(){lock(sync)Store(state with{Phase="instalando",Message="Aplicando la copia verificada con la operación pausada."});}
    public void AwaitConciliation(long id){lock(sync)Store(state with{Mode="conciliar",RestorationId=id,Epoch=Guid.NewGuid().ToString(),Phase="validada",Message="La copia fue recuperada. El Propietario debe registrar la conciliación antes de reabrir."});}
    public void Normal(){lock(sync)Store(state with{Mode="normal",Phase="finalizada",Message=null});}
    public void Failed(string message){lock(sync)Store(state with{Mode="fallo",Message=message});}
    public long ReserveId(string table,long observed)
    {
        lock(sync)
        {
            long next;try{next=checked(Math.Max(observed,state.IdentityFloors.GetValueOrDefault(table))+1);}catch(OverflowException){throw new BusinessException("La numeración alcanzó su capacidad; requiere revisión.");}
            var floors=new Dictionary<string,long>(state.IdentityFloors){[table]=next};Store(state with{IdentityFloors=floors});return next;
        }
    }
    public long ReserveOperation(long observed)
    {
        lock(sync)
        {
            long next;try{next=checked(Math.Max(observed,state.OperationFloor)+1);}catch(OverflowException){throw new BusinessException("La numeración de operaciones alcanzó su capacidad; requiere revisión.");}if(state.IdentityFloors.Count>0)Store(state with{OperationFloor=next});return next;
        }
    }
    private void Store(RecoveryControl value)
    {
        var directory=Path.GetFullPath(Path.GetDirectoryName(path)!);Directory.CreateDirectory(directory);var temporary=Path.Combine(directory,"recuperacion-"+Guid.NewGuid().ToString("N")+".tmp");
        if(Path.GetDirectoryName(Path.GetFullPath(temporary))!=directory||Path.GetDirectoryName(Path.GetFullPath(path))!=directory)throw new InvalidOperationException("Control de recuperación fuera del almacén previsto.");
        try
        {
            using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {JsonSerializer.Serialize(output,value,Json);output.Flush(true);}
            ReplaceControl(temporary,path);state=value;
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException){logger?.LogError(error,"No se pudo conservar el control de continuidad");throw new BusinessException("No se pudo conservar el control de continuidad. Revisa espacio y permisos del almacén.");}
    }
    internal static void ReplaceControl(string temporary,string destination)
    {
        for(var attempt=0;;attempt++)
        {
            try{File.Move(temporary,destination,true);return;}
            catch(IOException error)when(attempt<5&&(error.HResult&0xffff) is 32 or 33){Thread.Sleep(20*(attempt+1));}
            catch(UnauthorizedAccessException error)when(attempt<5&&OperatingSystem.IsWindows()&&(error.HResult&0xffff)==5){Thread.Sleep(20*(attempt+1));}
        }
    }
    private sealed class ReadLease(RecoveryGate gate):IDisposable
    {private int disposed;public void Dispose(){if(Interlocked.Exchange(ref disposed,1)==0)gate.Leave();}}
    private sealed class ExclusiveLease(RecoveryGate gate,HttpContext? context):IDisposable
    {private int disposed;public void Dispose(){if(Interlocked.Exchange(ref disposed,1)==0)gate.EndExclusive(context);}}
}
public sealed class RecoveryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context,RecoveryGate gate)
    {
        var state=gate.State;
        if(context.Request.Path=="/Recuperacion/Estado")
        {context.Response.ContentType="text/html; charset=utf-8";await context.Response.WriteAsync("<!doctype html><html lang='es'><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Estado de recuperación · MEGA</title><main><h1>Estado de recuperación</h1><p>"+WebUtility.HtmlEncode(state.Message??"La operación está disponible.")+"</p><p>Estado: "+WebUtility.HtmlEncode(state.Mode)+"</p><a href='/Recuperacion'>Continuar revisión autorizada</a></main></html>");return;}
        var lease=gate.TryHttp(context.Request.Path.Value??"");
        if(lease is null){context.Response.StatusCode=503;context.Response.Headers.RetryAfter="15";context.Response.ContentType="text/html; charset=utf-8";await context.Response.WriteAsync("<!doctype html><html lang='es'><meta charset='utf-8'><title>Operación pausada · MEGA</title><main><h1>Operación pausada</h1><p>La recuperación requiere finalizar procesos o registrar la conciliación del Propietario.</p><a href='/Recuperacion/Estado'>Consultar estado</a></main></html>");return;}
        context.Items[RecoveryGate.LeaseKey]=lease;try{await next(context);}finally{if(context.Items[RecoveryGate.LeaseKey] is IDisposable current)current.Dispose();lease.Dispose();context.Items.Remove(RecoveryGate.LeaseKey);}
    }
}
public sealed class DatabaseInstanceLease:IDisposable
{
    private readonly FileStream stream;
    public DatabaseInstanceLease(OperationalFiles files)
    {Directory.CreateDirectory(files.Root);try{stream=new(Path.Combine(files.Root,"Instancia.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){throw new InvalidOperationException("Ya existe una instancia usando esta base. Detén la anterior antes de iniciar otra.");}}
    public void Dispose()=>stream.Dispose();
}
