using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
    }
}
