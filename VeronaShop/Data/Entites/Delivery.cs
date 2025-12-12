using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public enum DeliveryStatus { Pending = 0, InTransit = 1, Delivered = 2, Failed = 3 }

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

        public string Carrier { get; set; }
        public string TrackingNumber { get; set; }

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    }
}
