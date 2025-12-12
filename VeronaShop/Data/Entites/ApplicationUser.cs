using Microsoft.AspNetCore.Identity;

namespace VeronaShop.Data.Entites
{
    public class ApplicationUser : IdentityUser<int>
    {
        // Additional profile fields can go here
        public string DisplayName { get; set; }
    }
}