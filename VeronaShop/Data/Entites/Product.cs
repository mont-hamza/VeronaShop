using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Product catalog entry.
    /// </summary>
    public class Product
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Product name.</summary>
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SKU { get; set; } = string.Empty;

        /// <summary>Product description (optional).</summary>
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [DataType(DataType.Currency)]
        public decimal? DiscountPrice { get; set; }

        public int StockQuantity { get; set; }

        public bool IsPublished { get; set; } = true;

        // legacy single-image property (kept for compatibility)
        public string ImageUrl { get; set; } = string.Empty;

        // multiple images
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // Relations
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

        // Audit
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
