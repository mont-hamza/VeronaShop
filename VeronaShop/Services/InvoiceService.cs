using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace VeronaShop.Services
{
    public class InvoiceService
    {
        private readonly IEmailSender _emailSender;

        public InvoiceService(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public Task<byte[]> GenerateInvoicePdfAsync(Data.Entites.Invoice invoice)
        {
            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Header().Text($"Invoice #{invoice.Id}").FontSize(18).SemiBold();
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Order: {invoice.OrderId}");
                        col.Item().Text($"Issued: {invoice.IssuedAt:u}");
                        col.Item().Text($"Amount: {invoice.Amount:C}").Bold();
                        if (!string.IsNullOrWhiteSpace(invoice.Notes))
                            col.Item().Text(invoice.Notes);
                    });
                });
            }).GeneratePdf();
            return Task.FromResult(bytes);
        }

        public async Task SendInvoiceAsync(Data.Entites.Invoice invoice)
        {
            var pdf = await GenerateInvoicePdfAsync(invoice);
            await _emailSender.SendEmailAsync(invoice.Email, $"Invoice #{invoice.Id}", $"Please find your invoice.", pdf, $"invoice-{invoice.Id}.pdf");
        }
    }
}