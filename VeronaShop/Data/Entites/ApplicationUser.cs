using Microsoft.AspNetCore.Identity;

namespace VeronaShop.Data.Entites
{
    /// <summary>
    /// Application user extending IdentityUser with additional profile fields.
    /// </summary>
    public class ApplicationUser : IdentityUser<int>
    {
        /// <summary>Display name shown in the UI.</summary>
        public string DisplayName { get; set; } = string.Empty;

        // Extended profile fields stored on AspNetUsers
        /// <summary>Optional first name.</summary>
        public string? FirstName { get; set; }
        /// <summary>Optional last name.</summary>
        public string? LastName { get; set; }

        // Address information (optional)
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}