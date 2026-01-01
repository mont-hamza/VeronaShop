using System;
using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public enum NotificationStatus { Pending = 0, Sent = 1, Failed = 2 }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
        public DateTimeOffset? AttemptedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
