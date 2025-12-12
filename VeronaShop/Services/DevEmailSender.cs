using System.Diagnostics;

namespace VeronaShop.Services
{
    public class DevEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string to, string subject, string html, byte[] attachment = null, string attachmentName = null)
        {
            // For development: write email to debug output
            Debug.WriteLine($"Sending email to {to}: {subject}\n{html}");
            if (attachment != null)
            {
                Debug.WriteLine($"Attachment: {attachmentName} ({attachment.Length} bytes)");
            }
            return Task.CompletedTask;
        }
    }
}