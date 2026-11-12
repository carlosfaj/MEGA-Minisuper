using System.Security.Cryptography;
using System.Text;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Mega.Web.Services;
public sealed class OperationalFiles(DatabaseLocation database)
{
    public string Root{get;}=Path.Combine(Path.GetDirectoryName(database.Path)!,Path.GetFileNameWithoutExtension(database.Path)+".archivos");
    public string Reports=>Path.Combine(Root,"Reportes");
    public string Secrets=>Path.Combine(Root,"Secretos");
    public string ReportPath(string reference)
    {
        if(Path.GetFileName(reference)!=reference||!Guid.TryParseExact(Path.GetFileNameWithoutExtension(reference),"N",out _)||Path.GetExtension(reference) is not (".pdf" or ".xlsx"))throw new BusinessException("La referencia del archivo no es válida.");
        return Path.Combine(Reports,reference);
    }
}
public interface ISecretVault{string Save(string secret);string Read(string reference);}
public sealed class WindowsSecretVault(OperationalFiles files):ISecretVault
{
    private static readonly byte[] Purpose=Encoding.UTF8.GetBytes("MEGA-SMTP-v1");
    public string Save(string secret)
    {
        if(!OperatingSystem.IsWindows())throw new BusinessException("La protección de credenciales requiere Windows.");
        var bytes=Encoding.UTF8.GetBytes(secret);
        try{var encrypted=ProtectedData.Protect(bytes,Purpose,DataProtectionScope.CurrentUser);Directory.CreateDirectory(files.Secrets);var reference=Guid.NewGuid().ToString("N");using var stream=new FileStream(Path.Combine(files.Secrets,reference+".bin"),FileMode.CreateNew,FileAccess.Write);stream.Write(encrypted);return reference;}
        finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public string Read(string reference)
    {
        if(!OperatingSystem.IsWindows()||!Guid.TryParseExact(reference,"N",out _))throw new BusinessException("La credencial protegida requiere volver a configurarse en este perfil de Windows.");
        try{var bytes=ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(files.Secrets,reference+".bin")),Purpose,DataProtectionScope.CurrentUser);try{return Encoding.UTF8.GetString(bytes);}finally{CryptographicOperations.ZeroMemory(bytes);}}
        catch(Exception e)when(e is IOException or CryptographicException or UnauthorizedAccessException){throw new BusinessException("No se pudo abrir la credencial protegida. Vuelve a configurarla en este perfil de Windows.");}
    }
}
public sealed record MailSettingsChange(long Id,long Version,bool Enabled,string? Host,int Port,string Security,string? Sender,string? User,string? Password,bool ClearPassword=false);
public sealed class MailConfiguration(MegaDbContext db,AccessControl access,Operations operations,SettingsService settings,ISecretVault secrets)
{
    public static string Address(string? value)
    {
        var text=AccessControl.Text(value,"Correo electrónico",254);
        if(text.Any(char.IsControl)||!MailboxAddress.TryParse(text,out var parsed)||parsed.Address!=text)throw new BusinessException("Introduce una única dirección de correo válida, sin nombre ni lista de destinatarios.");return text;
    }
    public async Task SaveAsync(long userId,MailSettingsChange input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);var current=await settings.CurrentAsync();
        if(current.Id!=input.Id)throw new BusinessException("La configuración cambió. Recarga antes de guardar.");AccessControl.Version(current.Version,input.Version);
        var next=(ConfiguracionNegocio)db.Entry(current).CurrentValues.ToObject();
        if(input.Enabled)
        {
            var host=AccessControl.Text(input.Host,"Servidor SMTP",253);if(Uri.CheckHostName(host)==UriHostNameType.Unknown||host.Any(char.IsControl))throw new BusinessException("Introduce el nombre o dirección del servidor, sin protocolo ni ruta.");
            if(input.Port is <1 or >65535||input.Security is not ("starttls" or "tls"))throw new BusinessException("Introduce puerto válido y seguridad STARTTLS o TLS.");
            next.SmtpHost=host;next.SmtpPuerto=input.Port;next.SmtpSeguridad=input.Security;next.CorreoRemitente=Address(input.Sender);next.SmtpUsuario=AccessControl.Optional(input.User,"Usuario SMTP",254);
            if(input.ClearPassword)next.SmtpSecretoReferencia=null;
            if(string.IsNullOrEmpty(input.Password)&&next.SmtpUsuario is not null&&next.SmtpSecretoReferencia is not null)
            {
                var owner=await db.Set<ConfiguracionNegocio>().AsNoTracking().Where(x=>x.SmtpSecretoReferencia==next.SmtpSecretoReferencia&&x.SmtpHost!=null).OrderByDescending(x=>x.Version).FirstOrDefaultAsync();
                if(owner is null||owner.SmtpHost!=host||owner.SmtpUsuario!=next.SmtpUsuario)throw new BusinessException("Al cambiar servidor o usuario SMTP, introduce de nuevo la credencial.");
            }
            if(!string.IsNullOrEmpty(input.Password)){if(input.Password.Length>1000)throw new BusinessException("La credencial supera el tamaño permitido.");next.SmtpSecretoReferencia=secrets.Save(input.Password);}
            if(next.SmtpUsuario is not null&&next.SmtpSecretoReferencia is null)throw new BusinessException("Introduce una credencial para autenticar la cuenta SMTP.");
        }
        else next.SmtpHost=null;
        var old=await db.Set<ConfiguracionNegocio>().SingleAsync(x=>x.Id==current.Id);var now=Formats.Now();if(string.CompareOrdinal(now,old.VigenteDesde)<=0){await Task.Delay(2);now=Formats.Now();}
        if(string.CompareOrdinal(now,old.VigenteDesde)<=0)throw new BusinessException("Vuelve a intentar guardar la configuración.");old.VigenteHasta=now;await db.SaveChangesAsync();next.Id=0;next.Version=checked(current.Version+1);next.VigenteDesde=now;next.VigenteHasta=null;next.CambiadoPor=userId;db.Add(next);await db.SaveChangesAsync();
        operations.Audit(userId,"Configurar correo","ConfiguracionNegocio",next.Id.ToString(),Guid.NewGuid().ToString(),new{anteriorId=current.Id,nuevaId=next.Id,habilitado=input.Enabled,next.SmtpHost,next.SmtpPuerto,next.SmtpSeguridad,next.CorreoRemitente,next.SmtpUsuario,credencialProtegida=next.SmtpSecretoReferencia is not null});await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
