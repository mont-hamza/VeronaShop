namespace VeronaShop.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string html, byte[] attachment = null, string attachmentName = null);
    }
}