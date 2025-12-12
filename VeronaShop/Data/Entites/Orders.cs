using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public enum OrderStatus { Pending = 0, Processing = 1, Shipped = 2, Delivered = 3, Cancelled = 4 }

    public class Orders
    {
        public Orders() { }
        public Orders(int id) { Id = id; }

        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; }

        public DateTimeOffset OrderDate { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        // Order lines
        public virtual ICollection<OrderProduct> OrderProducts { get; set; }

        // Totals
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [DataType(DataType.Currency)]
        public decimal ShippingCost { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public bool IsPaid { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
