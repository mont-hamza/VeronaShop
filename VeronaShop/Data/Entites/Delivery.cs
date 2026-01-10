using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public enum DeliveryStatus { Pending = 0, InTransit = 1, Delivered = 2, Failed = 3 }

    /// <summary>
    /// Delivery details for an order.
    /// </summary>
    public class Delivery
    {
        public Delivery() { }
        public Delivery(int id) { Id = id; }

        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Orders Order { get; set; }

        public DateTimeOffset EstimatedDeliveryFrom { get; set; }
        public DateTimeOffset? EstimatedDeliveryTo { get; set; }

        public int? CarrierId { get; set; }
        public Carrier? Carrier { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    }
}

namespace VeronaShop.Data.Entites
{
    public class Carrier
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(100)]
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
