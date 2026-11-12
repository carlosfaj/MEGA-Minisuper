using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mega.Tests;
public sealed class MailTests
{
    [Fact]
    public async Task changing_server_requires_new_secret_while_reenabling_same_target_preserves_it()
    {
        using var f=new Fixture();var settings=f.Scope.ServiceProvider.GetRequiredService<SettingsService>();var config=f.Scope.ServiceProvider.GetRequiredService<MailConfiguration>();var c=await settings.CurrentAsync();await config.SaveAsync(1,new(c.Id,c.Version,true,"smtp.invalid",587,"starttls","mega@example.invalid","usuario","Clave ficticia"));c=await settings.CurrentAsync();var reference=c.SmtpSecretoReferencia;await Assert.ThrowsAsync<BusinessException>(()=>config.SaveAsync(1,new(c.Id,c.Version,true,"otro.invalid",587,"starttls","mega@example.invalid","usuario",null)));await config.SaveAsync(1,new(c.Id,c.Version,false,null,587,"starttls",null,null,null));c=await settings.CurrentAsync();await config.SaveAsync(1,new(c.Id,c.Version,true,"smtp.invalid",587,"starttls","mega@example.invalid","usuario",null));Assert.Equal(reference,(await settings.CurrentAsync()).SmtpSecretoReferencia);
    }
    private sealed class Vault:ISecretVault
    {private readonly Dictionary<string,string> values=[];public string Save(string secret){var id=Guid.NewGuid().ToString("N");values[id]=secret;return id;}public string Read(string reference)=>values[reference];}
    private sealed class Transport:IMailTransport
    {public List<MailDocument> Documents{get;}=[];public MailDispatchException? Failure;public int Delay;public async Task<MailAcceptance> SendAsync(MailDocument document){Documents.Add(document);if(Delay>0)await Task.Delay(Delay);if(Failure is not null)throw Failure;return new("FICTICIO-ACEPTADO");}}
    private sealed class Fixture:IDisposable
    {
        public Transport Sender{get;}=new();public MegaFactory Factory{get;}public IServiceScope Scope{get;}public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();public MailQueue Queue=>Scope.ServiceProvider.GetRequiredService<MailQueue>();public MailDispatcher Dispatcher=>Factory.Services.GetRequiredService<MailDispatcher>();
        public Fixture(){Factory=new(services:s=>{s.RemoveAll<ISecretVault>();s.RemoveAll<IMailTransport>();s.AddSingleton<ISecretVault,Vault>();s.AddSingleton<IMailTransport>(Sender);});Scope=Factory.Services.CreateScope();}
        public async Task Enable(){var c=await Scope.ServiceProvider.GetRequiredService<SettingsService>().CurrentAsync();await Scope.ServiceProvider.GetRequiredService<MailConfiguration>().SaveAsync(1,new(c.Id,c.Version,true,"smtp.invalid",587,"starttls","mega@example.invalid",null,null));}
        public Task<long> Enqueue(string? key=null)=>Queue.EnqueueAsync(1,key??Guid.NewGuid().ToString(),"destino@example.invalid",new("inventario","2026-01-01","2026-12-31"),"pdf");
        public async Task<SolicitudCorreo> Job(){Db.ChangeTracker.Clear();return await Db.Set<SolicitudCorreo>().OrderBy(x=>x.Id).FirstAsync();}
        public void Dispose(){Scope.Dispose();Factory.Dispose();}
    }
    [Fact]
    public async Task T12_disabled_mail_keeps_frozen_report_without_internet_or_commercial_effects()
    {
        using var f=new Fixture();var key=Guid.NewGuid().ToString();var id=await f.Enqueue(key);var before=await f.Queue.DownloadAsync(1,id);Assert.Equal(id,await f.Enqueue(key));Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Empty(f.Sender.Documents);var job=await f.Job();Assert.Equal("Pendiente",job.Estado);Assert.NotNull(job.UltimoError);await f.Scope.ServiceProvider.GetRequiredService<ExpenseService>().SaveAsync(1,new(Guid.NewGuid().ToString(),"2026-01-01","Gasto posterior","10",null));Assert.Equal(before,await f.Queue.DownloadAsync(1,id));Assert.Empty(await f.Db.Set<Venta>().ToListAsync());Assert.Empty(await f.Db.Set<MovimientoCaja>().ToListAsync());
    }
    [Fact]
    public async Task known_connection_failure_retries_three_times_with_same_message_and_attachment()
    {
        using var f=new Fixture();await f.Enable();await f.Enqueue();f.Sender.Failure=new("Sin conexión ficticia",false,true,"conexion");
        for(var i=0;i<3;i++){Assert.True(await f.Dispatcher.RunOnceAsync());var row=await f.Job();if(i<2){Assert.Equal("Pendiente",row.Estado);Assert.NotNull(row.ProximoIntentoEn);Assert.False(await f.Dispatcher.RunOnceAsync());row.ProximoIntentoEn="2020-01-01T00:00:00.000Z";await f.Db.SaveChangesAsync();}else Assert.Equal("Fallida",row.Estado);}
        Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Equal(3,f.Sender.Documents.Count);Assert.Single(f.Sender.Documents.Select(x=>x.MessageId).Distinct());Assert.All(f.Sender.Documents,x=>Assert.Equal(f.Sender.Documents[0].Content,x.Content));Assert.Equal(3,await f.Db.Set<IntentoCorreo>().CountAsync());
    }
    [Fact]
    public async Task uncertain_send_requires_explicit_new_request_and_never_blindly_retries()
    {
        using var f=new Fixture();await f.Enable();await f.Enqueue();f.Sender.Failure=new("Conexión perdida tras DATA",true,false,"contenido");await f.Dispatcher.RunOnceAsync();var row=await f.Job();Assert.Equal("Incierta",row.Estado);Assert.False(await f.Dispatcher.RunOnceAsync());await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.ResolveAsync(1,row.Id,MailQueue.ReviewHash(row),"reintentar_mismo","Comprobado",true));await f.Queue.ResolveAsync(1,row.Id,MailQueue.ReviewHash(row),"nueva_solicitud","Servidor/destinatario comprobados; posible duplicado asumido",true);f.Db.ChangeTracker.Clear();var jobs=await f.Db.Set<SolicitudCorreo>().OrderBy(x=>x.Id).ToListAsync();Assert.Equal("Resuelta",jobs[0].Estado);Assert.Equal(jobs[0].ReporteHash,jobs[1].ReporteHash);Assert.Equal(jobs[0].ReporteReferencia,jobs[1].ReporteReferencia);Assert.NotEqual(jobs[0].MessageId,jobs[1].MessageId);Assert.Single(f.Sender.Documents);
    }
    [Fact]
    public async Task expired_reservation_and_late_acceptance_preserve_manual_resolution()
    {
        using var f=new Fixture();await f.Enable();await f.Enqueue();var lease=await f.Queue.ClaimAsync();Assert.NotNull(lease);var row=await f.Job();row.LeaseHasta="2020-01-01T00:00:00.000Z";await f.Db.SaveChangesAsync();Assert.Null(await f.Queue.ClaimAsync());row=await f.Job();Assert.Equal("Incierta",row.Estado);await f.Queue.ResolveAsync(1,row.Id,MailQueue.ReviewHash(row),"no_reenviar","No duplicar",true);await f.Queue.FinishAsync(lease!,new("ACEPTACION-TARDIA"),null);Assert.Equal("Resuelta",(await f.Job()).Estado);Assert.Equal("ACEPTACION-TARDIA",(await f.Db.Set<IntentoCorreo>().SingleAsync()).RespuestaReferencia);
    }
    [Fact]
    public async Task concurrent_enqueue_and_dispatch_create_one_request_and_attempt()
    {
        using var f=new Fixture();await f.Enable();var key=Guid.NewGuid().ToString();async Task<long> Enqueue(){using var s=f.Factory.Services.CreateScope();return await s.ServiceProvider.GetRequiredService<MailQueue>().EnqueueAsync(1,key,"destino@example.invalid",new("inventario","2026-01-01","2026-12-31"),"pdf");}var ids=await Task.WhenAll(Enqueue(),Enqueue());Assert.Equal(ids[0],ids[1]);f.Sender.Delay=80;await Task.WhenAll(f.Dispatcher.RunOnceAsync(),f.Dispatcher.RunOnceAsync());Assert.Single(f.Sender.Documents);Assert.Equal("Aceptada_SMTP",(await f.Job()).Estado);Assert.Equal(1,await f.Db.Set<IntentoCorreo>().CountAsync());
    }
    [Fact]
    public async Task permissions_integrity_and_late_failure_leave_no_partial_or_untrusted_send()
    {
        using var f=new Fixture();await f.Enable();await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.EnqueueAsync(3,Guid.NewGuid().ToString(),"destino@example.invalid",new("inventario","2026-01-01","2026-12-31"),"pdf"));await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_mail BEFORE INSERT ON EventoAuditoria WHEN NEW.entidad='SolicitudCorreo' BEGIN SELECT RAISE(ABORT,'Fallo tardio correo'); END;");await Assert.ThrowsAsync<DbUpdateException>(()=>f.Enqueue());f.Db.ChangeTracker.Clear();Assert.Empty(await f.Db.Set<SolicitudCorreo>().ToListAsync());await f.Db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_mail;");await f.Enqueue();var row=await f.Job();var files=f.Factory.Services.GetRequiredService<OperationalFiles>();await File.WriteAllTextAsync(files.ReportPath(row.ReporteReferencia),"Contenido alterado ficticio");Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Equal("Fallida",(await f.Job()).Estado);Assert.Empty(f.Sender.Documents);
        using var client=f.Factory.Client();await HttpTestSupport.LoginAsync(client,"inventario");Assert.Contains("SinPermiso",(await client.GetAsync("/Correos")).Headers.Location!.ToString());
    }
    [Fact]
    public async Task smtp_credentials_are_protected_and_never_stored_in_audit_or_database_values()
    {
        const string secret="ClaveSMTPFicticiaNoPublicar2026!";using var f=new MegaFactory();using var s=f.Services.CreateScope();var config=s.ServiceProvider.GetRequiredService<MailConfiguration>();var settings=s.ServiceProvider.GetRequiredService<SettingsService>();var c=await settings.CurrentAsync();await config.SaveAsync(1,new(c.Id,c.Version,true,"smtp.invalid",587,"starttls","mega@example.invalid","cuenta-ficticia",secret));var next=await settings.CurrentAsync();var vault=s.ServiceProvider.GetRequiredService<ISecretVault>();Assert.Equal(secret,vault.Read(next.SmtpSecretoReferencia!));var path=Path.Combine(f.Services.GetRequiredService<OperationalFiles>().Secrets,next.SmtpSecretoReferencia+".bin");Assert.DoesNotContain(secret,Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path)));var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();Assert.DoesNotContain(secret,Formats.Json(await db.Set<ConfiguracionNegocio>().ToListAsync()));Assert.DoesNotContain(secret,Formats.Json(await db.Set<EventoAuditoria>().ToListAsync()));await Assert.ThrowsAsync<BusinessException>(()=>config.SaveAsync(2,new(next.Id,next.Version,false,null,0,"",null,null,null)));Assert.Throws<BusinessException>(()=>vault.Read("../otra-ruta"));
    }
    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    [InlineData("drop")]
    public async Task real_mailkit_tls_loopback_distinguishes_accept_reject_and_lost_final_response(string mode)
    {
        await using var server=new LoopbackSmtp(mode);var transport=new MailTransport(new Vault()){ClientFactory=()=>new MailKit.Net.Smtp.SmtpClient{ServerCertificateValidationCallback=(_,_,_,_)=>true}};
        var configuration=new ConfiguracionNegocio{SmtpHost="127.0.0.1",SmtpPuerto=server.Port,SmtpSeguridad="tls",CorreoRemitente="mega@example.invalid"};var document=new MailDocument(configuration,"destino@example.invalid","prueba-fija@mega.local","inventario","prueba.pdf",Encoding.ASCII.GetBytes("PDF ficticio de prueba SMTP"));
        if(mode=="accept")Assert.NotEmpty((await transport.SendAsync(document)).Reference);else{var error=await Assert.ThrowsAsync<MailDispatchException>(()=>transport.SendAsync(document));Assert.Equal(mode=="drop",error.Uncertain);Assert.False(error.Retryable);if(mode=="reject")Assert.Equal("550",error.SmtpCode);}
        var content=await server.Content.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.Contains("prueba-fija@mega.local",content);Assert.Contains("prueba.pdf",content);
        using var message=MimeKit.MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(content)));var attachment=Assert.IsType<MimeKit.MimePart>(message.Attachments.Single());using var decoded=new MemoryStream();attachment.Content.DecodeTo(decoded);Assert.Equal(document.Content,decoded.ToArray());
    }
    private sealed class LoopbackSmtp:IAsyncDisposable
    {
        private readonly TcpListener listener=new(IPAddress.Loopback,0);private readonly X509Certificate2 certificate;private readonly Task task;public int Port{get;}public TaskCompletionSource<string> Content{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public LoopbackSmtp(string mode)
        {
            using var key=RSA.Create(2048);var request=new CertificateRequest("CN=localhost",key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);using var generated=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddHours(1));certificate=X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx),null,X509KeyStorageFlags.UserKeySet|X509KeyStorageFlags.Exportable);listener.Start();Port=((IPEndPoint)listener.LocalEndpoint).Port;task=Run(mode);
        }
        private async Task Run(string mode)
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));using var socket=await listener.AcceptTcpClientAsync(timeout.Token);await using var ssl=new SslStream(socket.GetStream());await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions{ServerCertificate=certificate,EnabledSslProtocols=SslProtocols.Tls12|SslProtocols.Tls13},timeout.Token);using var reader=new StreamReader(ssl,new UTF8Encoding(false),leaveOpen:true);await using var writer=new StreamWriter(ssl,new UTF8Encoding(false),leaveOpen:true){AutoFlush=true,NewLine="\r\n"};await writer.WriteLineAsync("220 localhost MEGA prueba SMTP");
            while(await reader.ReadLineAsync(timeout.Token) is {} line)
            {
                if(line.StartsWith("EHLO")){await writer.WriteLineAsync("250-localhost");await writer.WriteLineAsync("250 SIZE 5000000");}
                else if(line.StartsWith("MAIL FROM")||line.StartsWith("RCPT TO"))await writer.WriteLineAsync("250 OK");
                else if(line=="DATA"){await writer.WriteLineAsync("354 Terminar con punto");var body=new StringBuilder();while(await reader.ReadLineAsync(timeout.Token) is {} part&&part!=".")body.AppendLine(part);Content.TrySetResult(body.ToString());if(mode=="drop")return;await writer.WriteLineAsync(mode=="reject"?"550 Rechazo ficticio":"250 Aceptado ficticio");return;}
                else if(line=="RSET")await writer.WriteLineAsync("250 OK");else if(line=="QUIT"){await writer.WriteLineAsync("221 Adios");return;}else await writer.WriteLineAsync("500 Comando no implementado");
            }
        }
        public async ValueTask DisposeAsync(){listener.Stop();try{await task;}finally{certificate.Dispose();}}
    }
}
