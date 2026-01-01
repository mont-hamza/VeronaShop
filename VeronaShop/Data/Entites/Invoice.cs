using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Invoice record for an order.
    /// </summary>
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Orders Order { get; set; } = null!;

        public DateTimeOffset IssuedAt { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.Currency)]
        public decimal Amount { get; set; }

        /// <summary>Contact email for the invoice.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Optional internal notes.</summary>
        public string Notes { get; set; } = string.Empty;
    }
}