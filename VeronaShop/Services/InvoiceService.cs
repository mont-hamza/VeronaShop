using System.Text;

namespace VeronaShop.Services
{
    public class InvoiceService
    {
        private readonly IEmailSender _emailSender;

        public InvoiceService(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Data.Entites.Invoice invoice)
        {
            // Simple placeholder PDF content as bytes (replace with QuestPDF for real use)
            var text = $"Invoice #{invoice.Id}\nAmount: {invoice.Amount}\nIssued: {invoice.IssuedAt}";
            return Encoding.UTF8.GetBytes(text);
        }

        public async Task SendInvoiceAsync(Data.Entites.Invoice invoice)
        {
            var pdf = await GenerateInvoicePdfAsync(invoice);
            await _emailSender.SendEmailAsync(invoice.Email, $"Invoice #{invoice.Id}", $"Please find your invoice.", pdf, $"invoice-{invoice.Id}.pdf");
        }
    }
}