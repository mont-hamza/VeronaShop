using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace VeronaShop.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailSender(string apiKey, string fromEmail, string fromName)
        {
            _apiKey = apiKey;
            _fromEmail = fromEmail;
            _fromName = fromName;
        }

        public async Task SendEmailAsync(string to, string subject, string html, byte[] attachment = null, string attachmentName = null)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var toAddr = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddr, subject, plainTextContent: null, htmlContent: html);

            if (attachment != null && attachment.Length > 0 && !string.IsNullOrEmpty(attachmentName))
            {
                var base64 = System.Convert.ToBase64String(attachment);
                msg.AddAttachment(attachmentName, base64);
            }

            var response = await client.SendEmailAsync(msg);
            if ((int)response.StatusCode >= 400)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new System.InvalidOperationException($"SendGrid failed: {(int)response.StatusCode} {response.StatusCode} - {body}");
            }
        }
    }
}
