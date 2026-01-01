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

        public int Quantity { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.Currency)]
        public decimal UnitPrice { get; set; }
    }
}