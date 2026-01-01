using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeronaShop.Data.Entites
{
    public enum CustomerStatus { Active = 1, Inactive = 2, Banned = 3 }

}
namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Represents a customer record linked to an identity user.
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Customer display name.
        /// </summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Contact email address.
        /// </summary>
        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone, MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Region { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        // Link to Identity user
        public int? IdentityUserId { get; set; }
        public ApplicationUser? IdentityUser { get; set; }

        // Audit
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public CustomerStatus Status { get; set; } = CustomerStatus.Active;

        // Navigation properties
        public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
    }
}
