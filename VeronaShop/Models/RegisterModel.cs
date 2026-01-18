using System;
using System.ComponentModel.DataAnnotations;

namespace VeronaShop.Models
{
    public class RegisterModel
    {
        [Required]
        public string UserName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, MinLength(6)]
        public string Password { get; set; }
        public string DisplayName { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Phone { get; set; }
    }
}
