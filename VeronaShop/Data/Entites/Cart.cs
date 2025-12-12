using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        public int? CustomerId { get; set; }
        public Customer Customer { get; set; }

        public string SessionId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}