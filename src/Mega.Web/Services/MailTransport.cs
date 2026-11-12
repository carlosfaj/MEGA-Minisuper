using System.Security.Cryptography;
using System.Text;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Mega.Web.Data;
using MimeKit;

namespace Mega.Web.Services;
public sealed record MailDocument(ConfiguracionNegocio Configuration,string Recipient,string MessageId,string ReportKind,string FileName,byte[] Content);
public sealed record MailAcceptance(string Reference);
public sealed class MailDispatchException(string code,bool uncertain,bool retryable,string stage,string? smtpCode=null):Exception(code)
{public string Code{get;}=code;public bool Uncertain{get;}=uncertain;public bool Retryable{get;}=retryable;public string Stage{get;}=stage;public string? SmtpCode{get;}=smtpCode;}
public interface IMailTransport{Task<MailAcceptance> SendAsync(MailDocument document);}
public sealed class MailTransport(ISecretVault secrets):IMailTransport
{
    public Func<SmtpClient>? ClientFactory{get;init;}
    public async Task<MailAcceptance> SendAsync(MailDocument document)
    {
        var c=document.Configuration;var stage="conexion";var sending=false;
        using var client=ClientFactory?.Invoke()??new SmtpClient();client.Timeout=30000;using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try
        {
            var message=new MimeMessage{MessageId=document.MessageId,Subject="MEGA · "+ReportService.Kinds[document.ReportKind]};message.From.Add(MailboxAddress.Parse(c.CorreoRemitente!));message.To.Add(MailboxAddress.Parse(document.Recipient));
            var body=new BodyBuilder{TextBody="Reporte interno de MEGA solicitado por una cuenta de gestión. El archivo adjunto conserva los datos al generarlo; no acredita validación contable ni fiscal."};
            body.Attachments.Add(document.FileName,document.Content,ContentType.Parse(document.FileName.EndsWith(".pdf")?"application/pdf":"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));message.Body=body.ToMessageBody();
            string? password=null;if(c.SmtpUsuario is not null)password=secrets.Read(c.SmtpSecretoReferencia!);
            await client.ConnectAsync(c.SmtpHost!,checked((int)c.SmtpPuerto!.Value),c.SmtpSeguridad=="tls"?SecureSocketOptions.SslOnConnect:SecureSocketOptions.StartTls,timeout.Token);
            if(c.SmtpUsuario is not null){stage="autenticacion";await client.AuthenticateAsync(c.SmtpUsuario,password!,timeout.Token);}
            stage="contenido";sending=true;var response=await client.SendAsync(message,timeout.Token);
            if(response.AcceptedRecipients.Count!=1)throw new MailDispatchException("El servidor no confirmó el destinatario solicitado.",response.RejectedRecipients.Count==0,false,"respuesta_final");
            return new(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(response.ResponseText))));
        }
        catch(MailDispatchException){throw;}
        catch(SmtpCommandException error){throw new MailDispatchException("El servidor rechazó la operación SMTP.",false,(int)error.StatusCode is >=400 and <500,sending?"respuesta_final":stage,((int)error.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));}
        catch(MailKit.Security.AuthenticationException){throw new MailDispatchException("El servidor rechazó la autenticación. Revisa la configuración.",false,false,"autenticacion");}
        catch(SslHandshakeException){throw new MailDispatchException("No se pudo verificar una conexión TLS segura. Revisa servidor y certificado.",false,false,"conexion");}
        catch(BusinessException){throw new MailDispatchException("La credencial protegida requiere volver a configurarse.",false,false,"autenticacion");}
        catch{throw new MailDispatchException(sending?"Se perdió el resultado después de comenzar el envío. No se reenviará automáticamente.":"No se pudo conectar con el servidor. El reporte se conserva localmente.",sending,!sending,stage);}
        finally{if(client.IsConnected)try{await client.DisconnectAsync(true,CancellationToken.None);}catch{/* La desconexión no cambia una aceptación SMTP confirmada. */}}
    }
}
