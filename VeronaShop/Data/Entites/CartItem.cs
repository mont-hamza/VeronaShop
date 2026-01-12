using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Item line in a shopping cart.
    /// </summary>
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Selected size for product (optional)
        public int? SizeId { get; set; }
        public string? SizeName { get; set; }

        public int Quantity { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.Currency)]
        public decimal UnitPrice { get; set; }
    }
}