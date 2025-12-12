using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; }

        [MaxLength(200)]
        public string ContactName { get; set; }

        [EmailAddress, MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(100)]
        public string Phone { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Product> Products { get; set; }
    }
}
