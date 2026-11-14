using System.IO.Compression;
using System.Text.Json;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;
public sealed class BackupTests
{
    private sealed class Fixture:IDisposable
    {
        public string Root{get;}=Path.Combine(Path.GetTempPath(),"mega-backup-test-"+Guid.NewGuid().ToString("N"));public string Destination=>Path.Combine(Root,"medio");
        public MegaFactory Factory{get;}=new();public IServiceScope Scope{get;}
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();public BackupService Service=>Scope.ServiceProvider.GetRequiredService<BackupService>();public BackupArchive Archive=>Scope.ServiceProvider.GetRequiredService<BackupArchive>();public BackupDispatcher Dispatcher=>Factory.Services.GetRequiredService<BackupDispatcher>();public Operations Op=>Scope.ServiceProvider.GetRequiredService<Operations>();
        public Fixture(){Directory.CreateDirectory(Destination);Scope=Factory.Services.CreateScope();}
        public async Task Policy(bool periodic=false,bool close=false,bool update=false)
        {var c=await Scope.ServiceProvider.GetRequiredService<SettingsService>().CurrentAsync();await Service.SavePolicyAsync(5,new(c.Id,c.Version,Destination,"Medio ficticio de ensayo",periodic,30,close,update));}
        public async Task<long> Sale()
        {if((await Op.CashAsync(2)).Shift is null)await Op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(2,"unidad","1")];var quote=await Op.QuoteAsync(2,lines);return await Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),quote.Hash,"50",false,lines));}
        public async Task<RespaldoEjecucion> Backup(string? key=null)
        {var id=await Dispatcher.ManualAsync(1,key??Guid.NewGuid().ToString());Db.ChangeTracker.Clear();return await Db.Set<RespaldoEjecucion>().SingleAsync(x=>x.Id==id);}
        public void MoveMedium(bool missing)
        {var from=missing?Destination:Destination+"-ausente";var to=missing?Destination+"-ausente":Destination;if(!Path.GetFullPath(from).StartsWith(Path.GetFullPath(Root)+Path.DirectorySeparatorChar,StringComparison.Ordinal)||!Path.GetFullPath(to).StartsWith(Path.GetFullPath(Root)+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidOperationException();Directory.Move(from,to);}
        public void Dispose(){Scope.Dispose();Factory.Dispose();var p=Path.GetFullPath(Root);if(Path.GetDirectoryName(p)!=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))||!Path.GetFileName(p).StartsWith("mega-backup-test-"))throw new InvalidOperationException();Directory.Delete(p,true);}
    }
    [Fact]
    public async Task T14_consistent_wal_copy_includes_committed_sale_and_frozen_files()
    {
        using var f=new Fixture();await f.Db.Database.OpenConnectionAsync();await f.Db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");await f.Sale();var mail=f.Scope.ServiceProvider.GetRequiredService<MailQueue>();await mail.EnqueueAsync(1,Guid.NewGuid().ToString(),"destino@example.invalid",new("inventario","2026-01-01","2026-12-31"),"pdf");var settings=f.Scope.ServiceProvider.GetRequiredService<SettingsService>();var c=await settings.CurrentAsync();await f.Scope.ServiceProvider.GetRequiredService<MailConfiguration>().SaveAsync(1,new(c.Id,c.Version,true,"smtp.invalid",587,"starttls","mega@example.invalid","usuario-ficticio","Credencial ficticia protegida"));await f.Policy();var row=await f.Backup();Assert.Equal("Comprobada",row.Estado);Assert.NotNull(row.Checksum);
        var manifest=await f.Archive.VerifyAsync(row.ArchivoReferencia!,row.Checksum);Assert.Equal(1,manifest.Point.LastSale);Assert.Contains(manifest.Files,x=>x.Name.StartsWith("archivos/Reportes/"));Assert.Contains(manifest.Files,x=>x.Name.StartsWith("archivos/Secretos/"));Assert.Equal(2,manifest.SchemaVersion);await f.Sale();using var work=new BackupWorkspace(f.Factory.Services.GetRequiredService<OperationalFiles>());await f.Archive.ExtractVerifiedAsync(row.ArchivoReferencia!,work.Path,row.Checksum);await using var snapshot=BackupArchive.OpenRead(Path.Combine(work.Path,"base.db"));await snapshot.OpenAsync();await using var command=snapshot.CreateCommand();command.CommandText="SELECT COUNT(*) FROM Venta";Assert.Equal(1L,(long)(await command.ExecuteScalarAsync())!);Assert.Equal(2,await f.Db.Set<Venta>().CountAsync());Assert.Equal(57600,(await f.Op.CashAsync(2)).Expected);
    }
    [Fact]
    public async Task repeated_and_concurrent_request_recover_one_copy_without_overwriting()
    {
        using var f=new Fixture();await f.Policy();var key=Guid.NewGuid().ToString();var ids=await Task.WhenAll(f.Dispatcher.ManualAsync(1,key),f.Dispatcher.ManualAsync(1,key));Assert.Equal(ids[0],ids[1]);Assert.Single(Directory.GetFiles(f.Destination,"*.zip"));var first=await f.Db.Set<RespaldoEjecucion>().SingleAsync();var hash=await BackupArchive.FileHashAsync(first.ArchivoReferencia!);await f.Backup();Assert.Equal(hash,await BackupArchive.FileHashAsync(first.ArchivoReferencia!));Assert.Equal(2,Directory.GetFiles(f.Destination,"*.zip").Length);
    }
    [Fact]
    public async Task T14_missing_medium_is_visible_and_previous_copy_remains_valid()
    {
        using var f=new Fixture();await f.Policy();var first=await f.Backup();f.MoveMedium(true);try{var failure=await f.Backup();Assert.Equal("Fallida",failure.Estado);Assert.Contains("no están disponibles",failure.UltimoError);f.Db.ChangeTracker.Clear();Assert.Equal("Comprobada",(await f.Db.Set<RespaldoEjecucion>().SingleAsync(x=>x.Id==first.Id)).Estado);}finally{f.MoveMedium(false);}Assert.True(await f.Service.VerifyAsync(1,first.Id));Assert.Equal(first.Checksum,await BackupArchive.FileHashAsync(first.ArchivoReferencia!));
    }
    [Fact]
    public async Task corrupt_archive_does_not_replace_or_invalidate_other_good_copies()
    {
        using var f=new Fixture();await f.Policy();var first=await f.Backup();var second=await f.Backup();await using(var stream=new FileStream(second.ArchivoReferencia!,FileMode.Append))await stream.WriteAsync(new byte[]{1,2,3});Assert.False(await f.Service.VerifyAsync(1,second.Id));f.Db.ChangeTracker.Clear();Assert.Equal("Fallida",(await f.Db.Set<RespaldoEjecucion>().SingleAsync(x=>x.Id==second.Id)).Estado);Assert.True(await f.Service.VerifyAsync(1,first.Id));
    }
    [Fact]
    public async Task owner_policy_permissions_and_destination_boundaries_are_enforced()
    {
        using var f=new Fixture();var c=await f.Scope.ServiceProvider.GetRequiredService<SettingsService>().CurrentAsync();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SavePolicyAsync(1,new(c.Id,c.Version,f.Destination,"Medio",false,30,false,false)));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SavePolicyAsync(5,new(c.Id,c.Version,"relativo","Medio",false,30,false,false)));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SavePolicyAsync(5,new(c.Id,c.Version,f.Factory.Services.GetRequiredService<OperationalFiles>().Root,"Medio",false,30,false,false)));await f.Policy();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RunAsync(3,Guid.NewGuid().ToString()));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SavePolicyAsync(5,new(c.Id,c.Version,f.Destination,"Medio",false,30,false,false)));Assert.Empty(await f.Db.Set<RespaldoEjecucion>().ToListAsync());
    }
    [Fact]
    public async Task periodic_close_and_preupdate_policy_are_conditional_and_do_not_repeat_unnecessarily()
    {
        using var f=new Fixture();Assert.False(await f.Dispatcher.TickAsync());await f.Policy(close:true,update:true);Assert.False(await f.Dispatcher.TickAsync());await f.Sale();var turn=(await f.Op.CashAsync(2)).Shift!;await f.Op.CloseCashAsync(2,turn.Id,turn.Version,"538",null);Assert.True(await f.Dispatcher.TickAsync());Assert.False(await f.Dispatcher.TickAsync());Assert.Equal("cierre",(await f.Db.Set<RespaldoEjecucion>().SingleAsync()).Origen);
        var files=f.Factory.Services.GetRequiredService<OperationalFiles>();await File.WriteAllTextAsync(Path.Combine(files.Root,"VersionAplicada.txt"),"0.0.0");await f.Dispatcher.BeforeUpdateAsync();f.Dispatcher.RecordVersion();await f.Dispatcher.BeforeUpdateAsync();Assert.Equal(1,await f.Db.Set<RespaldoEjecucion>().CountAsync(x=>x.Origen=="preactualizacion"));await f.Policy(periodic:true);Assert.True(await f.Dispatcher.TickAsync());Assert.False(await f.Dispatcher.TickAsync());
    }
    [Theory]
    [InlineData("path")]
    [InlineData("point")]
    public async Task altered_manifest_paths_or_recovery_points_are_rejected_before_restore(string alteration)
    {
        using var f=new Fixture();await f.Policy();var row=await f.Backup();var changed=Path.Combine(f.Root,"alterado.zip");File.Copy(row.ArchivoReferencia!,changed);using(var zip=ZipFile.Open(changed,ZipArchiveMode.Update)){var old=zip.GetEntry("manifiesto.json")!;BackupManifest manifest;using(var reader=old.Open())manifest=JsonSerializer.Deserialize<BackupManifest>(reader,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;old.Delete();if(alteration=="path")manifest=manifest with{Files=[manifest.Files[0] with{Name="../fuera.db"}]};else manifest=manifest with{Point=manifest.Point with{LastSale=12345}};using var stream=zip.CreateEntry("manifiesto.json").Open();JsonSerializer.Serialize(stream,manifest,new JsonSerializerOptions(JsonSerializerDefaults.Web));}await Assert.ThrowsAsync<BusinessException>(()=>f.Archive.VerifyAsync(changed));Assert.False(File.Exists(Path.Combine(f.Root,"fuera.db")));
    }
}
