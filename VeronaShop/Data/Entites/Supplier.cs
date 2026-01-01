using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Supplier company information.
    /// </summary>
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; }

        [MaxLength(200)]
        public string ContactName { get; set; } = string.Empty;

        [EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
