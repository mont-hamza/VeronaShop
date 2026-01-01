using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // relative URL under wwwroot
        public string Url { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
