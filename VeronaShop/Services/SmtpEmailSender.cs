using System.Net;
using System.Net.Mail;

namespace VeronaShop.Services
{
    // dev placeholder SMTP sender. Configure with real SMTP settings in production.
    public class SmtpEmailSender : IEmailSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;

        public SmtpEmailSender(string host, int port, string user, string pass)
        {
            _host = host;
            _port = port;
            _user = user;
            _pass = pass;
        }

        public async Task SendEmailAsync(string to, string subject, string html, byte[] attachment = null, string attachmentName = null)
        {
            using var client = new SmtpClient(_host, _port)
            {
                Credentials = string.IsNullOrWhiteSpace(_user) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(_user, _pass),
                EnableSsl = true,
                Timeout = 15000
            };

            using var message = new MailMessage(_user ?? string.Empty, to, subject, html) { IsBodyHtml = true };
            if (attachment != null && attachmentName != null)
            {
                using var stream = new System.IO.MemoryStream(attachment);
                var attach = new Attachment(stream, attachmentName);
                message.Attachments.Add(attach);
                await client.SendMailAsync(message);
                return;
            }

            await client.SendMailAsync(message);
        }
    }
}