using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    public class AdminProfile
    {
        [Key]
        public int Id { get; set; }

        public int ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [MaxLength(200)]
        public string DisplayName { get; set; }

        public string Notes { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}