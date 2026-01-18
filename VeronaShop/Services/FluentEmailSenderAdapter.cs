using FluentEmail.Core;
using FluentEmail.Core.Models;

namespace VeronaShop.Services
{
    /// <summary>
    /// Adapter to bridge VeronaShop IEmailSender to FluentEmail with optional attachment bytes.
    /// </summary>
    public class FluentEmailSenderAdapter : IEmailSender
    {
        private readonly IFluentEmail _email;

        public FluentEmailSenderAdapter(IFluentEmail email)
        {
            _email = email;
        }

        public async Task SendEmailAsync(string to, string subject, string html, byte[] attachment = null, string attachmentName = null)
        {
            var email = _email
                .To(to)
                .Subject(subject)
                .Body(html, isHtml: true);

            if (attachment != null && !string.IsNullOrWhiteSpace(attachmentName))
            {
                email.Attach(new Attachment
                {
                    Data = new MemoryStream(attachment),
                    Filename = attachmentName,
                    ContentType = "application/octet-stream"
                });
            }

            var result = await email.SendAsync();
            if (!result.Successful)
            {
                throw new InvalidOperationException("Failed to send email: " + string.Join(";", result.ErrorMessages));
            }
        }
    }
}
