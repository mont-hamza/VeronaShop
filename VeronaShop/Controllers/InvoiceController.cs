using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using VeronaShop.Data;
using VeronaShop.Data.Entites;
using VeronaShop.Services;

namespace VeronaShop.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoiceController : ControllerBase
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IEmailSender _emailSender;

        public InvoiceController(IDbContextFactory<ApplicationDbContext> dbFactory, IEmailSender emailSender)
        {
            _dbFactory = dbFactory;
            _emailSender = emailSender;
        }

        [HttpGet("{id}.pdf")]
        public async Task<IActionResult> GetPdf(int id)
        {
            using var db = _dbFactory.CreateDbContext();
            var order = await db.Orders.Include(o => o.OrderProducts).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            // generate simple PDF
            using var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Verdana", 12);
            gfx.DrawString($"Invoice for order {order.OrderNumber}", font, XBrushes.Black, new XRect(40, 40, page.Width - 80, 20));
            gfx.DrawString($"Placed: {order.OrderDate:u}", font, XBrushes.Black, new XRect(40, 60, page.Width - 80, 20));
            gfx.DrawString($"Total: {order.TotalAmount:C}", font, XBrushes.Black, new XRect(40, 80, page.Width - 80, 20));
            int y = 110;
            foreach (var it in order.OrderProducts)
            {
                gfx.DrawString($"- {it.ProductName} x {it.Quantity} @ {it.UnitPrice:C}", font, XBrushes.Black, new XRect(40, y, page.Width - 80, 20));
                y += 20;
            }

            using var ms = new System.IO.MemoryStream();
            doc.Save(ms);
            ms.Position = 0;
            return File(ms.ToArray(), "application/pdf", $"invoice-{order.OrderNumber}.pdf");
        }

        [HttpPost("{id}/email")]
        public async Task<IActionResult> EmailInvoice(int id, [FromBody] EmailRequest req)
        {
            using var db = _dbFactory.CreateDbContext();
            var order = await db.Orders.Include(o => o.OrderProducts).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            // Generate PDF bytes
            using var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Verdana", 12);
            gfx.DrawString($"Invoice for order {order.OrderNumber}", font, XBrushes.Black, new XRect(40, 40, page.Width - 80, 20));
            int y = 80;
            foreach (var it in order.OrderProducts)
            {
                gfx.DrawString($"- {it.ProductName} x {it.Quantity} @ {it.UnitPrice:C}", font, XBrushes.Black, new XRect(40, y, page.Width - 80, 20));
                y += 20;
            }
            using var ms = new System.IO.MemoryStream();
            doc.Save(ms);
            var bytes = ms.ToArray();

            // send email with simple HTML template
            var to = req?.To ?? order.Customer?.Email ?? order.Customer?.IdentityUser?.Email ?? string.Empty;
            if (string.IsNullOrEmpty(to)) return BadRequest("No recipient email");

            var html = $@"
                <h2>Invoice for order {order.OrderNumber}</h2>
                <p>Placed: {order.OrderDate:u}</p>
                <p>Total: {order.TotalAmount:C}</p>
                <table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse'>
                    <thead><tr><th>Product</th><th>Qty</th><th>Unit</th><th>Line</th></tr></thead>
                    <tbody>
            ";
            foreach (var it in order.OrderProducts)
            {
                html += $"<tr><td>{System.Net.WebUtility.HtmlEncode(it.ProductName)}</td><td>{it.Quantity}</td><td>{it.UnitPrice:C}</td><td>{(it.UnitPrice * it.Quantity):C}</td></tr>";
            }
            html += "</tbody></table>";

            await _emailSender.SendEmailAsync(to, $"Invoice {order.OrderNumber}", html, bytes, $"invoice-{order.OrderNumber}.pdf");
            return Ok(new { sent = true });
        }

        public class EmailRequest { public string? To { get; set; } }
    }
}
