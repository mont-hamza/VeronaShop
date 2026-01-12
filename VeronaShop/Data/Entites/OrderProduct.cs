using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Line item snapshot for an order.
    /// </summary>
    public class OrderProduct
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Orders Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Snapshot fields
        [Required, MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        // Selected size snapshot (optional)
        public string? SizeName { get; set; }

        [DataType(DataType.Currency)]
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
