using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using LogHelper = SanShine.SaaS.Core.Common.Core.Utilities.LogHelper;

namespace SanShine.SaaS.Core.Application.Services;

public interface IEmailSendService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string template, string? cc, string? bcc, List<string>? attachments = null);

}

public class EmailSendService : IEmailSendService
{
    private const string MAILFROM = "dys@sanko.com.tr";
    private readonly string _smtpServer = "appposta.sanko.com.tr";
    private readonly int _smtpPort = 25;
    private readonly string _smtpUser = ""; // Eğer kimlik doğrulama gerekiyorsa
    private readonly string _smtpPassword = ""; // Eğer kimlik doğrulama gerekiyorsa
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string template, string? cc, string? bcc, List<string>? attachments = null)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Sanko Mail", MAILFROM));
            var toEmails = toEmail.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var emailAddress in toEmails)
            {
                email.To.Add(MailboxAddress.Parse(emailAddress.Trim()));
            }

            if (!string.IsNullOrEmpty(cc))
            {
                var ccEmails = cc.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var emailAddress in ccEmails)
                {
                    email.Cc.Add(MailboxAddress.Parse(emailAddress.Trim()));
                }
            }

            if (!string.IsNullOrEmpty(bcc))
            {
                var bccEmails = bcc.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var emailAddress in bccEmails)
                {
                    email.Bcc.Add(MailboxAddress.Parse(emailAddress.Trim()));
                }
            }

            email.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = template };

            if (attachments != null && attachments.Any())
            {
                foreach (var attachmentPath in attachments)
                {
                    if (File.Exists(attachmentPath))
                    {
                        bodyBuilder.Attachments.Add(attachmentPath);
                    }
                }
            }

            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(_smtpUser) && !string.IsNullOrEmpty(_smtpPassword))
            {
                await smtp.AuthenticateAsync(_smtpUser, _smtpPassword);
            }

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            return true;
        }
        catch (Exception e)
        {
            LogHelper.WriteToFileAsync($"Mail Gönderimi yapılamadı. Hata: {e}", "Log", "MailErrorLog");
            return false;
        }
    }
}
