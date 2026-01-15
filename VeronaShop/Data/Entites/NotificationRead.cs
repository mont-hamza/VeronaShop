using System;
using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class NotificationRead
    {
        [Key]
        public int Id { get; set; }

        public int NotificationId { get; set; }
        public Notification Notification { get; set; } = null!;

        public int? UserId { get; set; }

        public string? RecipientEmail { get; set; }

        public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
