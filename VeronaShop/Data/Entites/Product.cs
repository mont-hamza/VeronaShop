using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string SKU { get; set; }

        public string Description { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [DataType(DataType.Currency)]
        public decimal? DiscountPrice { get; set; }

        public int StockQuantity { get; set; }

        public bool IsPublished { get; set; } = true;

        public string ImageUrl { get; set; }

        // Relations
        public int? SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public int? CategoryId { get; set; }
        public Category Category { get; set; }

        public virtual ICollection<OrderProduct> OrderProducts { get; set; }

        // Audit
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
