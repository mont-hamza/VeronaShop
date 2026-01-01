using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Administrator profile extensions.
    /// </summary>
    public class AdminProfile
    {
        [Key]
        public int Id { get; set; }

        public int ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; } = null!;

        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}