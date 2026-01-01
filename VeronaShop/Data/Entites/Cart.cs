using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Shopping cart entity used for anonymous or authenticated users.
    /// </summary>
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>Browser session id used for anonymous carts.</summary>
        public string SessionId { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}