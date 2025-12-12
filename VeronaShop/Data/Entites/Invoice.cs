using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Orders Order { get; set; }

        public DateTimeOffset IssuedAt { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.Currency)]
        public decimal Amount { get; set; }

        public string Email { get; set; }

        public string Notes { get; set; }
    }
}