using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class OrderProduct
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Orders Order { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        // Snapshot fields
        [Required, MaxLength(200)]
        public string ProductName { get; set; }

        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [DataType(DataType.Currency)]
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
