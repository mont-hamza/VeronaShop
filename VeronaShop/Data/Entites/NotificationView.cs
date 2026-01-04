using System;
using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class NotificationView
    {
        [Key]
        public int Id { get; set; }

        public int NotificationId { get; set; }
        public Notification Notification { get; set; } = null!;

        public int AdminId { get; set; }
        public DateTimeOffset ViewedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
