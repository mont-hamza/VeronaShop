using System;

namespace VeronaShop.Models
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsViewedForMe { get; set; }
    }
}
