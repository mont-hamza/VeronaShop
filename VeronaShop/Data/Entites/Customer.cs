using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeronaShop.Data.Entites
{
    public enum CustomerStatus { Active = 1, Inactive = 2, Banned = 3 }

}
namespace VeronaShop.Data.Entites
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; }

        [Phone, MaxLength(50)]
        public string Phone { get; set; }

        [MaxLength(500)]
        public string Address { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        [MaxLength(100)]
        public string Country { get; set; }

        [MaxLength(100)]
        public string Region { get; set; }

        public DateTime? DateOfBirth { get; set; }

        // Link to Identity user
        public int? IdentityUserId { get; set; }
        public ApplicationUser IdentityUser { get; set; }

        // Audit
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public CustomerStatus Status { get; set; } = CustomerStatus.Active;

        // Navigation properties
        public virtual ICollection<Orders> Orders { get; set; }
    }
}
