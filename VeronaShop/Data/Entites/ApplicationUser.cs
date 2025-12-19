using Microsoft.AspNetCore.Identity;

namespace VeronaShop.Data.Entites
{
    public class ApplicationUser : IdentityUser<int>
    {
        // Additional profile fields can go here
        public string DisplayName { get; set; }

        // Extended profile fields stored on AspNetUsers
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Address information (optional)
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}