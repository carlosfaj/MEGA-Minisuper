using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mega.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;
public sealed record RecoveryPreview(long BackupId,string Token,BackupManifest Manifest,RecoverySurvey Current,RecoverySurvey Recovered);
public sealed record RecoveryRequest(long BackupId,string Token,string Key,string Password,string Reason,bool Acknowledged);
public sealed class RecoveryService(MegaDbContext db,AccessControl access,Operations operations,BackupArchive archive,
    OperationalFiles files,DatabaseLocation location,DatabaseInspector inspector,RecoveryGate gate,IPasswordHasher<Usuario> passwords,ILogger<RecoveryService> logger)
{
    public async Task<RecoveryPreview> PreviewAsync(long ownerId,long backupId)
    {
        await access.RequireAsync(ownerId,"Propietario");if(gate.State.Mode!="normal")throw new BusinessException("Finaliza la recuperación pendiente antes de seleccionar otra copia.");
        var row=await SelectAsync(backupId);using var work=new BackupWorkspace(files);var manifest=await archive.ExtractVerifiedAsync(row.ArchivoReferencia!,work.Path,row.Checksum);
        if(manifest.Preservation||manifest.ExecutionId!=row.Id)throw new BusinessException("Selecciona un respaldo operativo comprobado y vinculado a esta ejecución.");
        await CheckOwnerAsync(Path.Combine(work.Path,"base.db"),ownerId);
        var current=await RecoverySnapshot.CaptureAsync(location.Path);var recovered=await RecoverySnapshot.CaptureAsync(Path.Combine(work.Path,"base.db"));
        return new(backupId,Token(row.Checksum!,current.Fingerprint),manifest,current,recovered);
    }
    private async Task<RespaldoEjecucion> SelectAsync(long id)=>await db.Set<RespaldoEjecucion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.Estado=="Comprobada"&&x.Checksum!=null&&x.ArchivoReferencia!=null)??throw new BusinessException("La copia seleccionada no está comprobada.");
    private static string Token(string checksum,string fingerprint)=>Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(checksum+":"+fingerprint)));
    private DbContextOptions<MegaDbContext> Options(string path)=>new DbContextOptionsBuilder<MegaDbContext>().UseSqlite(new SqliteConnectionStringBuilder{DataSource=path,ForeignKeys=true,Pooling=false}.ToString()).AddInterceptors(new RecoveryIdentityInterceptor(gate)).Options;
    private async Task<Usuario> CheckOwnerAsync(string path,long ownerId)
    {
        var owner=await db.Set<Usuario>().AsNoTracking().SingleAsync(x=>x.Id==ownerId);await using var copy=new MegaDbContext(Options(path));
        var restored=await copy.Set<Usuario>().SingleOrDefaultAsync(x=>x.Id==ownerId&&x.Activo==1&&x.TipoCuenta=="comercial"&&x.NombreNormalizado==owner.NombreNormalizado);
        if(restored?.RolId is not long role||!await copy.Set<Rol>().AnyAsync(x=>x.Id==role&&x.Nombre=="Propietario"))throw new BusinessException("La copia no conserva la identidad activa del Propietario que autoriza. Requiere una intervención técnica supervisada.");
        return owner;
    }
    public async Task<long> RestoreAsync(long ownerId,RecoveryRequest request,HttpContext? context=null)
    {
        await access.RequireAsync(ownerId,"Propietario");if(!Guid.TryParseExact(request.Key,"D",out _))throw new BusinessException("Recarga la revisión de recuperación.");
        var previous=await db.Set<Restauracion>().AsNoTracking().SingleOrDefaultAsync(x=>x.ClaveIntervencion==request.Key);
        if(previous is not null){if(previous.AutorizadoPor!=ownerId)throw new BusinessException("La intervención pertenece a otra autorización.");return previous.Id;}
        if(gate.State.Mode!="normal")throw new BusinessException("Ya existe una recuperación pendiente.");
        if(gate.State.Key==request.Key)throw new BusinessException("Esta intervención ya finalizó o falló. Inicia una revisión con una clave nueva.");
        var reason=AccessControl.Text(request.Reason,"Motivo de recuperación",1000);if(!request.Acknowledged)throw new BusinessException("Reconoce el punto de recuperación y la revisión de novedades pendientes.");
        var actor=await db.Set<Usuario>().AsNoTracking().SingleAsync(x=>x.Id==ownerId);
        if(request.Password.Length>200||passwords.VerifyHashedPassword(actor,actor.PasswordHash,request.Password)==PasswordVerificationResult.Failed)throw new BusinessException("La contraseña del Propietario no es válida.");
        using var exclusive=await gate.EnterExclusiveAsync(context);
        db.ChangeTracker.Clear();await db.Database.CloseConnectionAsync();
        var selected=await SelectAsync(request.BackupId);using var stage=new BackupWorkspace(files);
        var manifest=await archive.ExtractVerifiedAsync(selected.ArchivoReferencia!,stage.Path,selected.Checksum);
        if(manifest.Preservation||manifest.ExecutionId!=selected.Id)throw new BusinessException("La copia no corresponde a la ejecución comprobada.");
        actor=await CheckOwnerAsync(Path.Combine(stage.Path,"base.db"),ownerId);
        var current=await RecoverySnapshot.CaptureAsync(location.Path);if(Token(selected.Checksum!,current.Fingerprint)!=request.Token)throw new BusinessException("Los datos cambiaron desde la revisión. Vuelve a comprobar el punto antes de autorizar.");
        var recovered=await RecoverySnapshot.CaptureAsync(Path.Combine(stage.Path,"base.db"));var floors=new Dictionary<string,long>(current.Ids);
        foreach(var pair in recovered.Ids.Concat(manifest.IdentityFloors??[]))floors[pair.Key]=Math.Max(floors.GetValueOrDefault(pair.Key),pair.Value);
        var key=Guid.Parse(request.Key).ToString("N");var preservation=Path.Combine(files.Root,"Preservaciones",key+".zip");Directory.CreateDirectory(Path.GetDirectoryName(preservation)!);
        gate.Begin(request.Key,ownerId,actor.NombreNormalizado,selected.ArchivoReferencia!,selected.Checksum!,floors,Math.Max(current.OperationSequence,Math.Max(recovered.OperationSequence,manifest.OperationFloor)));
        try
        {
            operations.Audit(ownerId,"Autorizar restauración","Restauracion",request.Key,Guid.NewGuid().ToString(),new{motivo=reason,respaldo=selected.Id,selected.Checksum,punto=manifest.Point});await db.SaveChangesAsync();await db.Database.CloseConnectionAsync();
            var preserved=await archive.CreateAsync(preservation,0,true);gate.Preserved(Path.GetFileName(preservation),preserved.Checksum);
            var id=await PrepareAsync(Path.Combine(stage.Path,"base.db"),actor,request,selected,manifest,current,recovered,preserved);
            await CheckDatabaseAsync(Path.Combine(stage.Path,"base.db"));await InstallFilesAsync(stage.Path,manifest);
            gate.Installing();await RecoverySnapshot.InstallAsync(Path.Combine(stage.Path,"base.db"),location.Path);db.ChangeTracker.Clear();await CheckDatabaseAsync(location.Path);
            gate.AwaitConciliation(id);return id;
        }
        catch(Exception error)
        {
            logger.LogError(error,"Falló la recuperación autorizada {Key}; se conserva la preservación",request.Key);
            try{await RollbackAsync();operations.Audit(ownerId,"Fallo de restauración; estado anterior conservado","Restauracion",request.Key,Guid.NewGuid().ToString(),new{intervencion=request.Key,preservacion=gate.State.PreservationFile},"fallo");await db.SaveChangesAsync();}catch(Exception rollback){logger.LogCritical(rollback,"No pudo comprobarse la reversión de recuperación");gate.Failed("No se pudo comprobar la recuperación ni la reversión. Conserva los archivos y solicita revisión técnica supervisada.");}
            throw new BusinessException(gate.DatabaseSafe?"La recuperación no se completó. Se conservó o recuperó el estado anterior; revisa la copia y vuelve a comenzar.":"La operación permanece pausada para revisión técnica. Consulta el estado de recuperación.");
        }
    }
    private async Task<long> PrepareAsync(string path,Usuario owner,RecoveryRequest request,RespaldoEjecucion selected,BackupManifest manifest,RecoverySurvey current,RecoverySurvey recovered,BackupArtifact preserved)
    {
        await using var copy=new MegaDbContext(Options(path));await using var tx=await copy.Database.BeginTransactionAsync();var now=Formats.Now();
        var accounts=await db.Set<Usuario>().AsNoTracking().ToDictionaryAsync(x=>x.Id);var roles=await db.Set<Rol>().AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Nombre);var restoredRoles=await copy.Set<Rol>().ToDictionaryAsync(x=>x.Nombre,x=>x.Id);
        foreach(var account in await copy.Set<Usuario>().Where(x=>x.TipoCuenta=="comercial").ToListAsync())
        {
            if(accounts.TryGetValue(account.Id,out var currentAccount)&&currentAccount.TipoCuenta=="comercial"&&currentAccount.NombreNormalizado==account.NombreNormalizado)
            {account.PasswordHash=currentAccount.PasswordHash;account.CredencialesVersion=Math.Max(account.CredencialesVersion,currentAccount.CredencialesVersion)+1;account.Activo=currentAccount.Activo;account.PermisoOperativoCaja=currentAccount.PermisoOperativoCaja;account.DelegacionCuentas=currentAccount.DelegacionCuentas;if(currentAccount.RolId is long role&&roles.TryGetValue(role,out var roleName)&&restoredRoles.TryGetValue(roleName,out var restoredRole))account.RolId=restoredRole;else account.Activo=0;}
            else{account.Activo=0;account.CredencialesVersion++;}
        }
        var row=new Restauracion{RespaldoId=await copy.Set<RespaldoEjecucion>().AnyAsync(x=>x.Id==selected.Id)?selected.Id:null,RespaldoReferencia=selected.ArchivoReferencia!,RespaldoChecksum=selected.Checksum!,VersionEsquemaOrigen=manifest.SchemaVersion.ToString(),AutorizadoPor=owner.Id,AutorizadoEn=gate.State.StartedUtc!,EjecutadoPor=owner.Id,Inicio=gate.State.StartedUtc!,Fin=now,Estado="Validada",ResultadoDetalle="Base y archivos comprobados; operación pausada hasta conciliación expresa.",Motivo=request.Reason.Trim(),PreservacionActualReferencia=preserved.Path,PuntoRecuperado=Formats.Json(manifest.Point),ConciliacionEstado="Pendiente",ConciliacionDetalle=Formats.Json(new{preservacionHash=preserved.Checksum,advertenciasPreservacion=preserved.Manifest.Warnings,antes=current.Rows,recuperado=recovered.Rows,numeracionConservada=gate.State.IdentityFloors,secuenciaConservada=gate.State.OperationFloor}),ClaveIntervencion=request.Key};
        copy.Add(row);await copy.SaveChangesAsync();
        await copy.Database.ExecuteSqlInterpolatedAsync($"UPDATE SolicitudImpresion SET estadoAntesCuarentena=estado,estado='En_cuarentena',restauracionId={row.Id},leaseToken=NULL,leaseHasta=NULL,proximoIntentoEn=NULL");
        await copy.Database.ExecuteSqlInterpolatedAsync($"UPDATE SolicitudCorreo SET estadoAntesCuarentena=estado,estado='En_cuarentena',restauracionId={row.Id},leaseToken=NULL,leaseHasta=NULL,proximoIntentoEn=NULL");
        await copy.Database.ExecuteSqlInterpolatedAsync($"UPDATE IntentoImpresion SET resultado='incierta',fin={now},errorCodigo='Recuperación: comprobar cola y papel' WHERE resultado='en_curso'");
        await copy.Database.ExecuteSqlInterpolatedAsync($"UPDATE IntentoCorreo SET resultado='incierta',fin={now},etapaSMTP='registro_resultado',errorCodigo='Recuperación: comprobar servidor y destinatario' WHERE resultado='en_curso'");
        await copy.Database.ExecuteSqlInterpolatedAsync($"UPDATE Recepcion SET version=version+1 WHERE id IN (SELECT recepcionId FROM Autorizacion WHERE invalidadaEn IS NULL AND decision='pendiente')");
        await copy.Database.ExecuteSqlRawAsync("UPDATE SolicitudInventario SET estado='En_preparacion',version=version+1 WHERE id IN (SELECT solicitudInventarioId FROM Autorizacion WHERE invalidadaEn IS NULL AND decision='pendiente'); UPDATE SolicitudAnulacion SET estado='En_preparacion',version=version+1 WHERE id IN (SELECT solicitudAnulacionId FROM Autorizacion WHERE invalidadaEn IS NULL AND decision='pendiente')");
        await copy.Database.ExecuteSqlInterpolatedAsync($"UPDATE Autorizacion SET invalidadaEn={now},motivoInvalidacion='Recuperación: requiere nueva revisión' WHERE invalidadaEn IS NULL AND decision='pendiente'");
        await copy.Database.ExecuteSqlInterpolatedAsync($"UPDATE AccesoSoporte SET revocadoEn={now},revocadoPor={owner.Id},motivoRevocacion='Recuperación: requiere nueva autorización' WHERE revocadoEn IS NULL");
        await copy.Database.ExecuteSqlRawAsync("UPDATE Usuario SET activo=0,credencialesVersion=credencialesVersion+1 WHERE tipoCuenta='soporte_temporal'");
        var config=await copy.Set<ConfiguracionNegocio>().SingleAsync(x=>x.VigenteHasta==null);if(string.CompareOrdinal(now,config.VigenteDesde)<=0){await Task.Delay(2);now=Formats.Now();}config.VigenteHasta=now;
        var next=(ConfiguracionNegocio)copy.Entry(config).CurrentValues.ToObject();next.Id=0;next.Version++;next.VigenteDesde=now;next.VigenteHasta=null;next.CambiadoPor=owner.Id;next.ImpresionHabilitada=0;next.SmtpHost=null;next.PautaRespaldo=null;next.PautaAutorizadaPor=null;next.PautaAutorizadaEn=null;copy.Add(next);
        foreach(var backup in await copy.Set<RespaldoEjecucion>().Where(x=>x.Estado=="En_proceso").ToListAsync())
        {backup.Fin=now;backup.Estado="Fallida";backup.UltimoError="Ejecución interrumpida en el punto recuperado; requiere comprobación.";}
        var linked=await copy.Set<RespaldoEjecucion>().SingleOrDefaultAsync(x=>x.Id==selected.Id);
        if(linked is not null){copy.Entry(linked).CurrentValues.SetValues(selected);linked.ComprobadoEn=now;linked.UltimoError=null;}
        copy.Add(new EventoAuditoria{Fecha=now,UsuarioId=owner.Id,Origen="persona",Accion="Aplicar restauración comprobada",Entidad="Restauracion",EntidadId=row.Id.ToString(),ClaveOperacion=Guid.NewGuid().ToString(),Resultado="exito",VersionFormato=1,DatosCambio=Formats.Json(new{respaldo=selected.Id,preservacion=preserved.Path,preserved.Checksum,cuarentena=true,reaperturaPendiente=true})});
        await copy.SaveChangesAsync();await tx.CommitAsync();return row.Id;
    }
    private async Task CheckDatabaseAsync(string path)
    {await using var connection=BackupArchive.OpenRead(path);await connection.OpenAsync();var report=await inspector.InspectAsync(connection);DatabaseInspector.RequireHealthy(report);if(!report.UpToDate)throw new BusinessException("El esquema recuperado requiere revisión.");}
    private async Task InstallFilesAsync(string stage,BackupManifest manifest)
    {
        foreach(var entry in manifest.Files.Where(x=>x.Name.StartsWith("archivos/",StringComparison.Ordinal)))
        {
            var target=Path.GetFullPath(Path.Combine(files.Root,entry.Name[9..].Replace('/',Path.DirectorySeparatorChar)));var root=Path.GetFullPath(files.Root)+Path.DirectorySeparatorChar;
            if(!target.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new BusinessException("Referencia de archivo fuera del almacén.");
            if(File.Exists(target)&&await BackupArchive.FileHashAsync(target)==entry.Hash)continue;
            if(entry.Name.StartsWith("archivos/Preservaciones/")&&File.Exists(target))throw new BusinessException("Una preservación existente tiene contenido diferente; requiere revisión técnica.");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(Path.Combine(stage,entry.Name.Replace('/',Path.DirectorySeparatorChar)),target,true);
            if(await BackupArchive.FileHashAsync(target)!=entry.Hash)throw new BusinessException("No se pudo comprobar un archivo recuperado.");
        }
    }
    private async Task RollbackAsync()
    {
        var state=gate.State;if(state.PreservationFile is null){gate.Normal();return;}
        if(!Guid.TryParseExact(Path.GetFileNameWithoutExtension(state.PreservationFile),"N",out _)||Path.GetFileName(state.PreservationFile)!=state.PreservationFile)throw new BusinessException("La preservación no tiene una referencia válida.");
        using var work=new BackupWorkspace(files);var manifest=await archive.ExtractVerifiedAsync(Path.Combine(files.Root,"Preservaciones",state.PreservationFile),work.Path,state.PreservationHash);
        if(!manifest.Preservation)throw new BusinessException("El archivo no es una preservación de intervención.");await InstallFilesAsync(work.Path,manifest);await db.Database.CloseConnectionAsync();await RecoverySnapshot.InstallAsync(Path.Combine(work.Path,"base.db"),location.Path);db.ChangeTracker.Clear();await CheckDatabaseAsync(location.Path);gate.Normal();
    }
    public async Task RecoverInterruptedAsync()
    {
        if(!File.Exists(location.Path)&&(gate.HasIdentityFloors||gate.State.Mode=="conciliar")){gate.Failed("Falta la base asociada al control de recuperación. Conserva el almacén y solicita revisión técnica.");return;}
        if(gate.State.Mode=="conciliar")
        {
            try
            {
                await CheckDatabaseAsync(location.Path);var state=gate.State;var row=await db.Set<Restauracion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==state.RestorationId&&x.ClaveIntervencion==state.Key&&x.Estado=="Validada"&&x.AutorizadoPor==state.OwnerId);
                if(row is null)throw new BusinessException("La base no corresponde a la intervención pendiente.");
                if(row.NucleoReabiertoEn is not null&&row.ConciliacionEstado=="Completada")gate.Normal();
            }
            catch(Exception error){logger.LogCritical(error,"El control pendiente no coincide con la base");gate.Failed("La intervención pendiente no pudo comprobarse en esta base. Se mantiene pausada para revisión técnica.");}
            return;
        }
        if(gate.State.Mode!="restaurando")return;
        try{using var exclusive=await gate.EnterExclusiveAsync();await RollbackAsync();logger.LogWarning("Se recuperó el estado anterior de una intervención interrumpida. Revisa la bitácora y la preservación antes de volver a intentar.");}
        catch(Exception error){logger.LogCritical(error,"La recuperación interrumpida requiere revisión");gate.Failed("Una intervención interrumpida no pudo revertirse de forma comprobada. Se mantiene pausada la operación.");}
    }
    public async Task<(FileStream Stream,string Name)> DownloadPreservationAsync(long ownerId,long id)
    {
        await access.RequireAsync(ownerId,"Propietario");var row=await db.Set<Restauracion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("La intervención no existe.");
        var path=Path.GetFullPath(row.PreservacionActualReferencia??throw new BusinessException("La intervención no tiene preservación asociada."));var parent=Path.GetFullPath(Path.Combine(files.Root,"Preservaciones"));
        if(!string.Equals(Path.GetDirectoryName(path),parent,StringComparison.OrdinalIgnoreCase)||Path.GetExtension(path)!=".zip"||!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path),"N",out _))throw new BusinessException("La referencia de preservación requiere revisión.");
        using var data=JsonDocument.Parse(row.ConciliacionDetalle!);using var initial=data.RootElement.TryGetProperty("revisionInicial",out var nested)?JsonDocument.Parse(nested.GetString()!):JsonDocument.Parse(row.ConciliacionDetalle!);var checksum=initial.RootElement.GetProperty("preservacionHash").GetString();
        var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        try{if(Convert.ToHexStringLower(await SHA256.HashDataAsync(stream))!=checksum)throw new BusinessException("La preservación no coincide con su huella comprobada.");stream.Position=0;return(stream,"MEGA-estado-anterior-"+id+".zip");}catch{await stream.DisposeAsync();throw;}
    }
    public async Task ReopenAsync(long ownerId,long restorationId,string notes,bool inventory,bool cash,bool changes,bool queues,HttpContext? context=null)
    {
        await access.RequireAsync(ownerId,"Propietario");if(!inventory||!cash||!changes||!queues)throw new BusinessException("Registra la revisión de inventario, caja, novedades y trabajos externos antes de reabrir.");notes=AccessControl.Text(notes,"Resultado de conciliación",4000);
        using var exclusive=await gate.EnterExclusiveAsync(context);await using var tx=await db.Database.BeginTransactionAsync();var row=await db.Set<Restauracion>().SingleOrDefaultAsync(x=>x.Id==restorationId)??throw new BusinessException("La intervención no existe.");
        if(row.NucleoReabiertoEn is not null){if(gate.State.Mode=="conciliar"&&gate.State.RestorationId==row.Id)gate.Normal();return;}if(gate.State.Mode!="conciliar"||gate.State.RestorationId!=row.Id||row.Estado!="Validada")throw new BusinessException("La intervención no está lista para reapertura.");
        await CheckDatabaseAsync(location.Path);row.ConciliacionEstado="Completada";row.ConciliacionDetalle=Formats.Json(new{revisionInicial=row.ConciliacionDetalle,resultado=notes,inventario=inventory,caja=cash,novedades=changes,trabajosRetenidos=queues});row.NucleoReabiertoEn=Formats.Now();row.ReaperturaAutorizadaPor=ownerId;
        operations.Audit(ownerId,"Conciliar y reabrir núcleo","Restauracion",row.Id.ToString(),Guid.NewGuid().ToString(),new{resultado=notes,cuarentenaSeConserva=true});await db.SaveChangesAsync();await tx.CommitAsync();gate.Normal();
    }
}
