using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mega.Tests;
public sealed class RecoveryTests
{
    private sealed class Printer:IPrintTransport
    {public int Count;public IReadOnlyList<string> Printers()=>["Impresora ficticia"];public Task<PrintAcceptance> SendAsync(PrintDocument document){Count++;return Task.FromResult(new PrintAcceptance("TEST","Ensayo sin papel"));}}
    private sealed class Mailer:IMailTransport
    {public int Count;public Task<MailAcceptance> SendAsync(MailDocument document){Count++;return Task.FromResult(new MailAcceptance("Ensayo sin red"));}}
    private sealed class Fixture:IDisposable
    {
        public MegaFactory Factory{get;}public IServiceScope Scope{get;}public Printer Printer{get;}=new();public Mailer Mailer{get;}=new();
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();public Operations Op=>Scope.ServiceProvider.GetRequiredService<Operations>();public RecoveryService Service=>Scope.ServiceProvider.GetRequiredService<RecoveryService>();public RecoveryGate Gate=>Factory.Services.GetRequiredService<RecoveryGate>();public OperationalFiles Files=>Factory.Services.GetRequiredService<OperationalFiles>();
        public string Medium{get;}=Path.Combine(Path.GetTempPath(),"mega-recovery-medium-"+Guid.NewGuid().ToString("N"));
        public Fixture(){Factory=new(services:s=>{s.RemoveAll<IPrintTransport>();s.AddSingleton<IPrintTransport>(Printer);s.RemoveAll<IMailTransport>();s.AddSingleton<IMailTransport>(Mailer);});Scope=Factory.Services.CreateScope();Directory.CreateDirectory(Medium);}
        public async Task<long> Sale(){if((await Op.CashAsync(2)).Shift is null)await Op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(2,"unidad","1")];var quote=await Op.QuoteAsync(2,lines);return await Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),quote.Hash,"50",false,lines));}
        public async Task<long> Backup(){var c=await Scope.ServiceProvider.GetRequiredService<SettingsService>().CurrentAsync();await Scope.ServiceProvider.GetRequiredService<BackupService>().SavePolicyAsync(5,new(c.Id,c.Version,Medium,"Medio ficticio",false,30,false,false));return await Factory.Services.GetRequiredService<BackupDispatcher>().ManualAsync(1,Guid.NewGuid().ToString());}
        public async Task<RecoveryRequest> Request(long id){var review=await Service.PreviewAsync(5,id);return new(id,review.Token,Guid.NewGuid().ToString(),"MegaDemo2026!","Ensayo de recuperación en base aislada",true);}
        public Task Reopen(long id)=>Service.ReopenAsync(5,id,"Ensayo aislado: inventario, caja y novedades revisados; trabajos conservados en cuarentena.",true,true,true,true);
        public void Dispose(){Scope.Dispose();Factory.Dispose();var p=Path.GetFullPath(Medium);if(Path.GetDirectoryName(p)!=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))||!Path.GetFileName(p).StartsWith("mega-recovery-medium-"))throw new InvalidOperationException();Directory.Delete(p,true);}
    }
    [Fact]
    public async Task T15_restore_preserves_current_rewinds_data_quarantines_and_requires_owner_reopen()
    {
        using var f=new Fixture();await f.Sale();var mail=await f.Scope.ServiceProvider.GetRequiredService<MailQueue>().EnqueueAsync(1,Guid.NewGuid().ToString(),"destino@example.invalid",new("inventario","2026-01-01","2026-12-31"),"pdf");var backup=await f.Backup();await f.Sale();
        var before=await RecoverySnapshot.CaptureAsync(f.Factory.Services.GetRequiredService<DatabaseLocation>().Path);var request=await f.Request(backup);var id=await f.Service.RestoreAsync(5,request);f.Db.ChangeTracker.Clear();
        Assert.Equal("conciliar",f.Gate.State.Mode);Assert.Single(await f.Db.Set<Venta>().ToListAsync());Assert.Equal(53800,(await f.Op.CashAsync(2)).Expected);Assert.Equal(29m,(await f.Op.StocksAsync())[2]);
        var restoration=await f.Db.Set<Restauracion>().SingleAsync();Assert.Equal("Pendiente",restoration.ConciliacionEstado);Assert.Null(restoration.NucleoReabiertoEn);Assert.True(File.Exists(restoration.PreservacionActualReferencia));
        var preserve=await f.Scope.ServiceProvider.GetRequiredService<BackupArchive>().VerifyAsync(restoration.PreservacionActualReferencia!,f.Gate.State.PreservationHash);Assert.True(preserve.Preservation);Assert.Equal(2,preserve.Point.LastSale);Assert.Equal(before.Rows["Venta"],preserve.Point.Rows["Venta"]);
        var download=await f.Service.DownloadPreservationAsync(5,id);await using(var stream=download.Stream){Assert.Equal(f.Gate.State.PreservationHash,Convert.ToHexStringLower(await System.Security.Cryptography.SHA256.HashDataAsync(stream)));}await Assert.ThrowsAsync<BusinessException>(()=>f.Service.DownloadPreservationAsync(1,id));
        var print=await f.Db.Set<SolicitudImpresion>().SingleAsync();Assert.Equal("En_cuarentena",print.Estado);Assert.Equal("Pendiente",print.EstadoAntesCuarentena);Assert.Equal(id,print.RestauracionId);Assert.Null(print.LeaseToken);
        Assert.Equal("En_cuarentena",(await f.Db.Set<SolicitudCorreo>().SingleAsync(x=>x.Id==mail)).Estado);Assert.False(await f.Factory.Services.GetRequiredService<PrintDispatcher>().RunOnceAsync());Assert.False(await f.Factory.Services.GetRequiredService<MailDispatcher>().RunOnceAsync());Assert.False(await f.Factory.Services.GetRequiredService<BackupDispatcher>().TickAsync());
        await Assert.ThrowsAsync<BusinessException>(()=>f.Scope.ServiceProvider.GetRequiredService<PrintQueue>().ResolveAsync(1,print.Id,PrintQueue.ReviewHash(print),"no_reenviar","Ensayo","Cola ficticia revisada",true));
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.ReopenAsync(1,id,"Ensayo",true,true,true,true));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.ReopenAsync(5,id,"Ensayo",true,false,true,true));Assert.Equal("conciliar",f.Gate.State.Mode);
        Assert.Equal(id,await f.Service.RestoreAsync(5,request));await f.Reopen(id);await f.Reopen(id);f.Db.ChangeTracker.Clear();Assert.Equal("normal",f.Gate.State.Mode);Assert.Equal("Completada",(await f.Db.Set<Restauracion>().SingleAsync()).ConciliacionEstado);
        var config=await f.Scope.ServiceProvider.GetRequiredService<SettingsService>().CurrentAsync();Assert.Equal(0,config.ImpresionHabilitada);Assert.Null(config.SmtpHost);Assert.Null(config.PautaRespaldo);Assert.False(await f.Factory.Services.GetRequiredService<PrintDispatcher>().RunOnceAsync());Assert.False(await f.Factory.Services.GetRequiredService<MailDispatcher>().RunOnceAsync());Assert.Equal(0,f.Printer.Count);Assert.Equal(0,f.Mailer.Count);
        await f.Scope.ServiceProvider.GetRequiredService<PrintQueue>().ResolveAsync(1,print.Id,PrintQueue.ReviewHash(print),"no_reenviar","Ensayo sin duplicación","Cola y papel ficticios revisados",true);Assert.Equal(1,await f.Db.Set<SolicitudImpresion>().CountAsync());
        var afterReopen=await f.Service.DownloadPreservationAsync(5,id);await afterReopen.Stream.DisposeAsync();
    }
    [Fact]
    public async Task restored_numbering_never_reuses_ids_or_receipt_sequence_and_survives_next_backup()
    {
        using var f=new Fixture();await f.Sale();var backup=await f.Backup();var lost=await f.Sale();var maxSequence=await f.Db.Set<OperacionInventario>().MaxAsync(x=>x.SecuenciaConfirmacion);var maxMovement=await f.Db.Set<MovimientoInventario>().MaxAsync(x=>x.Id);var maxAudit=await f.Db.Set<EventoAuditoria>().MaxAsync(x=>x.Id);
        var id=await f.Service.RestoreAsync(5,await f.Request(backup));await f.Reopen(id);var next=await f.Sale();f.Db.ChangeTracker.Clear();Assert.True(next>lost);var sale=await f.Db.Set<Venta>().SingleAsync(x=>x.Id==next);var operation=await f.Db.Set<OperacionInventario>().SingleAsync(x=>x.Id==sale.OperacionId);Assert.True(operation.SecuenciaConfirmacion>maxSequence);Assert.True(await f.Db.Set<MovimientoInventario>().MaxAsync(x=>x.Id)>maxMovement);Assert.True(await f.Db.Set<EventoAuditoria>().MaxAsync(x=>x.Id)>maxAudit);
        var nextBackup=await f.Backup();var record=await f.Db.Set<RespaldoEjecucion>().SingleAsync(x=>x.Id==nextBackup);var manifest=await f.Scope.ServiceProvider.GetRequiredService<BackupArchive>().VerifyAsync(record.ArchivoReferencia!,record.Checksum);Assert.True(manifest.IdentityFloors!["Venta"]>=next);Assert.True(manifest.OperationFloor>maxSequence);Assert.Contains(manifest.Files,x=>x.Name.StartsWith("archivos/Preservaciones/"));
    }
    [Fact]
    public async Task current_owner_only_password_and_stale_review_are_enforced_without_replacing_data()
    {
        using var f=new Fixture();var backup=await f.Backup();var request=await f.Request(backup);await Assert.ThrowsAsync<BusinessException>(()=>f.Service.PreviewAsync(1,backup));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RestoreAsync(1,request));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RestoreAsync(5,request with{Password="Incorrecta"}));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RestoreAsync(5,request with{Acknowledged=false}));await f.Sale();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RestoreAsync(5,request));Assert.Equal("normal",f.Gate.State.Mode);Assert.Single(await f.Db.Set<Venta>().ToListAsync());Assert.Empty(await f.Db.Set<Restauracion>().ToListAsync());
    }
    [Fact]
    public async Task file_installation_failure_rolls_back_and_keeps_a_verified_preservation()
    {
        using var f=new Fixture();await f.Scope.ServiceProvider.GetRequiredService<MailQueue>().EnqueueAsync(1,Guid.NewGuid().ToString(),"destino@example.invalid",new("inventario","2026-01-01","2026-12-31"),"pdf");var backup=await f.Backup();await f.Sale();var report=await f.Db.Set<SolicitudCorreo>().SingleAsync();var path=f.Files.ReportPath(report.ReporteReferencia);File.Delete(path);Directory.CreateDirectory(path);var before=await RecoverySnapshot.CaptureAsync(f.Factory.Services.GetRequiredService<DatabaseLocation>().Path);await Assert.ThrowsAsync<BusinessException>(async ()=>await f.Service.RestoreAsync(5,await f.Request(backup)));var after=await RecoverySnapshot.CaptureAsync(f.Factory.Services.GetRequiredService<DatabaseLocation>().Path);Assert.Equal(before.Fingerprint,after.Fingerprint);Assert.Equal("normal",f.Gate.State.Mode);var preservation=Assert.Single(Directory.GetFiles(Path.Combine(f.Files.Root,"Preservaciones"),"*.zip"));var manifest=await f.Scope.ServiceProvider.GetRequiredService<BackupArchive>().VerifyAsync(preservation);Assert.True(manifest.Preservation);Assert.NotEmpty(manifest.Warnings);Assert.Empty(await f.Db.Set<Restauracion>().ToListAsync());
    }
    [Fact]
    public async Task interrupted_installation_is_reverted_at_startup_without_external_dispatch()
    {
        using var f=new Fixture();await f.Sale();var before=await RecoverySnapshot.CaptureAsync(f.Factory.Services.GetRequiredService<DatabaseLocation>().Path);var key=Guid.NewGuid().ToString();var file=Guid.Parse(key).ToString("N")+".zip";Directory.CreateDirectory(Path.Combine(f.Files.Root,"Preservaciones"));f.Gate.Begin(key,5,"PROPIETARIO","Ensayo","0".PadLeft(64,'0'),before.Ids,before.OperationSequence);var archive=await f.Scope.ServiceProvider.GetRequiredService<BackupArchive>().CreateAsync(Path.Combine(f.Files.Root,"Preservaciones",file),0,true);f.Gate.Preserved(file,archive.Checksum);f.Gate.Installing();await f.Db.Database.ExecuteSqlRawAsync("UPDATE Producto SET nombre='Cambio ficticio interrumpido' WHERE id=2");await f.Service.RecoverInterruptedAsync();Assert.Equal("normal",f.Gate.State.Mode);Assert.Equal(before.Fingerprint,(await RecoverySnapshot.CaptureAsync(f.Factory.Services.GetRequiredService<DatabaseLocation>().Path)).Fingerprint);Assert.Equal(0,f.Printer.Count);Assert.Equal(0,f.Mailer.Count);
    }
    [Fact]
    public async Task pending_receipt_approval_is_invalidated_and_can_be_resubmitted_with_new_version()
    {
        using var f=new Fixture();var service=f.Scope.ServiceProvider.GetRequiredService<ReceivingService>();var supplier=await service.SaveSupplierAsync(1,new(null,0,"Proveedor ficticio",null,null,null,null,true));var receipt=await service.SaveAsync(3,new(null,0,Guid.NewGuid().ToString(),"REC-ENSAYO",supplier,"DOC-ENSAYO","2026-11-13",null,[new(2,"2","2","20",null,null,null)]));var approval=await service.SubmitAsync(3,receipt,1);var backup=await f.Backup();var id=await f.Service.RestoreAsync(5,await f.Request(backup));await f.Reopen(id);f.Db.ChangeTracker.Clear();var old=await f.Db.Set<Autorizacion>().SingleAsync(x=>x.Id==approval);Assert.NotNull(old.InvalidadaEn);Assert.Equal(2,(await f.Db.Set<Recepcion>().SingleAsync()).Version);var next=await service.SubmitAsync(3,receipt,2);Assert.NotEqual(approval,next);
    }
    [Fact]
    public async Task pause_drains_leases_and_blocks_new_work_without_touching_data()
    {
        using var f=new Fixture();var lease=f.Gate.TryWorker();Assert.NotNull(lease);var wait=f.Gate.EnterExclusiveAsync();Assert.False(wait.IsCompleted);Assert.Null(f.Gate.TryWorker());lease.Dispose();using(await wait){Assert.Null(f.Gate.TryHttp("/Acceso"));}using var normal=f.Gate.TryWorker();Assert.NotNull(normal);
        var context=new DefaultHttpContext();var own=f.Gate.TryHttp("/Recuperacion");context.Items[RecoveryGate.LeaseKey]=own;normal.Dispose();using(await f.Gate.EnterExclusiveAsync(context)){Assert.False(context.Items.ContainsKey(RecoveryGate.LeaseKey));}((IDisposable)context.Items[RecoveryGate.LeaseKey]!).Dispose();
    }
    [Fact]
    public void temporary_windows_reader_delays_atomic_counter_replacement_without_reusing_id()
    {
        if(!OperatingSystem.IsWindows())return;using var f=new Fixture();var path=Path.Combine(f.Files.Root,"Recuperacion.json");using var holder=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);var release=new Thread(()=>{Thread.Sleep(80);holder.Dispose();});release.Start();try{Assert.Equal(21,f.Gate.ReserveId("Venta",20));}finally{release.Join();}Assert.Equal(21,new RecoveryGate(f.Files).State.IdentityFloors["Venta"]);
    }
    [Fact]
    public void persistent_windows_reader_rejects_reservation_and_keeps_previous_counter()
    {
        if(!OperatingSystem.IsWindows())return;using var f=new Fixture();var path=Path.Combine(f.Files.Root,"Recuperacion.json");using var holder=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);Assert.Throws<BusinessException>(()=>f.Gate.ReserveId("Venta",20));Assert.False(f.Gate.State.IdentityFloors.ContainsKey("Venta"));holder.Dispose();Assert.Equal(21,f.Gate.ReserveId("Venta",20));
    }
    [Fact]
    public async Task restart_keeps_pending_conciliation_and_recognizes_committed_reopening()
    {
        using var f=new Fixture();var backup=await f.Backup();var id=await f.Service.RestoreAsync(5,await f.Request(backup));var epoch=f.Gate.Epoch;Assert.Equal("conciliar",new RecoveryGate(f.Files).State.Mode);await f.Service.RecoverInterruptedAsync();Assert.Equal("conciliar",f.Gate.State.Mode);Assert.Equal(epoch,f.Gate.Epoch);await f.Reopen(id);
        f.Gate.AwaitConciliation(id);await f.Service.RecoverInterruptedAsync();Assert.Equal("normal",f.Gate.State.Mode);Assert.Equal("normal",new RecoveryGate(f.Files).State.Mode);
    }
    [Fact]
    public async Task second_instance_and_corrupt_recovery_control_fail_closed()
    {
        using var f=new Fixture();Assert.Throws<InvalidOperationException>(()=>new DatabaseInstanceLease(f.Files));await File.WriteAllTextAsync(Path.Combine(f.Files.Root,"Recuperacion.json"),"{control dañado}");var gate=new RecoveryGate(f.Files);Assert.False(gate.DatabaseSafe);Assert.Null(gate.TryWorker());Assert.Null(gate.TryHttp("/Acceso"));
    }
    [Fact]
    public async Task credentials_deactivations_and_support_revocations_do_not_rewind_with_business_data()
    {
        using var f=new Fixture();var accounts=f.Scope.ServiceProvider.GetRequiredService<AccountService>();var support=await accounts.CreateSupportAsync(5,"tecnico","ClaveFicticia2026!",1,"Ensayo de soporte");var backup=await f.Backup();
        var owner=await f.Db.Set<Usuario>().SingleAsync(x=>x.Id==5);var admin=await f.Db.Set<Usuario>().SingleAsync(x=>x.Id==1);var cashier=await f.Db.Set<Usuario>().SingleAsync(x=>x.Id==2);var hasher=f.Scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();owner.PasswordHash=hasher.HashPassword(owner,"ClaveActual2026!");owner.CredencialesVersion++;admin.PasswordHash=hasher.HashPassword(admin,"ClaveAdminActual2026!");admin.CredencialesVersion++;cashier.Activo=0;await f.Db.SaveChangesAsync();
        var request=(await f.Request(backup)) with{Password="ClaveActual2026!"};var id=await f.Service.RestoreAsync(5,request);await f.Reopen(id);f.Db.ChangeTracker.Clear();owner=await f.Db.Set<Usuario>().SingleAsync(x=>x.Id==5);admin=await f.Db.Set<Usuario>().SingleAsync(x=>x.Id==1);Assert.NotEqual(PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(owner,owner.PasswordHash,"ClaveActual2026!"));Assert.Equal(PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(owner,owner.PasswordHash,"MegaDemo2026!"));Assert.NotEqual(PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(admin,admin.PasswordHash,"ClaveAdminActual2026!"));Assert.Equal(0,(await f.Db.Set<Usuario>().SingleAsync(x=>x.Id==2)).Activo);Assert.Equal(0,(await f.Db.Set<Usuario>().SingleAsync(x=>x.Id==support)).Activo);Assert.NotNull((await f.Db.Set<AccesoSoporte>().SingleAsync()).RevocadoEn);Assert.DoesNotContain("ClaveActual2026!",string.Join("",await f.Db.Set<EventoAuditoria>().Select(x=>x.DatosCambio).ToListAsync()));
    }
    [Fact]
    public async Task in_flight_attempts_become_uncertain_and_quarantine_cannot_retry_the_same_job()
    {
        using var f=new Fixture();await f.Sale();var print=await f.Db.Set<SolicitudImpresion>().SingleAsync();var now=Formats.Now();print.Estado="En_proceso";print.LeaseToken=Guid.NewGuid().ToString();print.LeaseHasta=DateTimeOffset.UtcNow.AddMinutes(3).ToString("O");f.Db.Add(new IntentoImpresion{SolicitudImpresionId=print.Id,Numero=1,Inicio=now,Resultado="en_curso",Impresora="Impresora ficticia",AgenteIdentidad="Ensayo aislado",LeaseToken=print.LeaseToken});await f.Db.SaveChangesAsync();var backup=await f.Backup();var id=await f.Service.RestoreAsync(5,await f.Request(backup));await f.Reopen(id);f.Db.ChangeTracker.Clear();print=await f.Db.Set<SolicitudImpresion>().SingleAsync();var attempt=await f.Db.Set<IntentoImpresion>().SingleAsync();Assert.Equal("incierta",attempt.Resultado);Assert.NotNull(attempt.Fin);Assert.Equal("En_proceso",print.EstadoAntesCuarentena);Assert.Null(print.LeaseToken);await Assert.ThrowsAsync<BusinessException>(()=>f.Scope.ServiceProvider.GetRequiredService<PrintQueue>().ResolveAsync(1,print.Id,PrintQueue.ReviewHash(print),"reintentar_mismo","Ensayo","Cola ficticia",true));Assert.Equal(0,f.Printer.Count);
    }
    [Fact]
    public async Task missing_selected_medium_and_missing_preservation_fail_without_silent_reopening()
    {
        using var f=new Fixture();var backup=await f.Backup();var request=await f.Request(backup);var row=await f.Db.Set<RespaldoEjecucion>().SingleAsync();var bytes=await File.ReadAllBytesAsync(row.ArchivoReferencia!);File.Delete(row.ArchivoReferencia!);await Assert.ThrowsAnyAsync<IOException>(()=>f.Service.RestoreAsync(5,request));Assert.Equal("normal",f.Gate.State.Mode);Assert.Empty(await f.Db.Set<Restauracion>().ToListAsync());await File.WriteAllBytesAsync(row.ArchivoReferencia!,bytes);
        var snapshot=await RecoverySnapshot.CaptureAsync(f.Factory.Services.GetRequiredService<DatabaseLocation>().Path);f.Gate.Begin(Guid.NewGuid().ToString(),5,"PROPIETARIO",row.ArchivoReferencia!,row.Checksum!,snapshot.Ids,snapshot.OperationSequence);f.Gate.Preserved(Guid.NewGuid().ToString("N")+".zip",new string('0',64));f.Gate.Installing();await f.Service.RecoverInterruptedAsync();Assert.Equal("fallo",f.Gate.State.Mode);Assert.Null(f.Gate.TryWorker());Assert.Null(f.Gate.TryHttp("/Venta"));
    }
    [Fact]
    public async Task HTTP_restore_and_reopen_pause_commerce_and_invalidate_previous_sessions()
    {
        using var f=new Fixture();await f.Sale();var backup=await f.Backup();await f.Sale();using var cashier=f.Factory.Client();await HttpTestSupport.LoginAsync(cashier,"cajero");using var owner=f.Factory.Client();await HttpTestSupport.LoginAsync(owner,"propietario");using var admin=f.Factory.Client();await HttpTestSupport.LoginAsync(admin);Assert.Contains("SinPermiso",(await admin.GetAsync("/Recuperacion")).Headers.Location!.ToString());var request=await f.Request(backup);
        var result=await HttpTestSupport.PostAsync(owner,"/Recuperacion?handler=Restore",new(){["Input.BackupId"]=backup.ToString(),["Input.Token"]=request.Token,["Input.Key"]=request.Key,["Input.Password"]=request.Password,["Input.Reason"]=request.Reason,["Input.Acknowledged"]="true"});Assert.Equal(HttpStatusCode.Redirect,result.StatusCode);Assert.Contains("Acceso",result.Headers.Location!.ToString());Assert.Equal(HttpStatusCode.ServiceUnavailable,(await cashier.GetAsync("/Venta")).StatusCode);Assert.Equal(HttpStatusCode.OK,(await cashier.GetAsync("/Recuperacion/Estado")).StatusCode);
        await HttpTestSupport.LoginAsync(owner,"propietario");Assert.Equal(HttpStatusCode.OK,(await owner.GetAsync("/Recuperacion")).StatusCode);var id=f.Gate.State.RestorationId!.Value;var reopened=await HttpTestSupport.PostAsync(owner,"/Recuperacion?handler=Reopen",new(){["id"]=id.ToString(),["notes"]="Conciliación del ensayo aislado, sin aceptación comercial.",["inventory"]="true",["cash"]="true",["changes"]="true",["queues"]="true"});Assert.Equal(HttpStatusCode.Redirect,reopened.StatusCode);Assert.Contains("Acceso",(await cashier.GetAsync("/Venta")).Headers.Location!.ToString());Assert.Equal("normal",f.Gate.State.Mode);
    }
}
