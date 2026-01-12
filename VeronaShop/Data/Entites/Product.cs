using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // multiple images (primary image is the first by SortOrder)
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // Compatibility property: maps to first ProductImage in the collection.
        // Not mapped so the schema is driven by ProductImages table.
        [NotMapped]
        public string? ImageUrl
        {
            get => Images?.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url ?? null;
            set
            {
                if (value == null) return;
                var first = Images?.OrderBy(i => i.SortOrder).FirstOrDefault();
                if (first != null)
                {
                    first.Url = value;
                }
                else
                {
                    Images.Add(new ProductImage { Url = value, SortOrder = 0, CreatedAt = DateTimeOffset.UtcNow });
                }
            }
        }

        // Relations
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

        // Comma separated list of sizes for the product (e.g. "S,M,L,XL").
        // Admin can edit this as a CSV; client UI will parse and show options when present.
        public string SizesCsv { get; set; } = string.Empty;

        // Audit
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
