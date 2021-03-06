﻿using System.ComponentModel.DataAnnotations;

namespace PolloPollo.Shared.DTO
{
    public class UserCreateDTO
    {
        [StringLength(255)]
        [Required]
        public string FirstName { get; set; }

        [StringLength(255)]
        [Required]
        public string SurName { get; set; }

        // Check to match regular email pattern something@domain
        [EmailAddress]
        [StringLength(191)]
        [Required]
        public string Email { get; set; }

        // Countries only contains characters.
        [RegularExpression(@"[^0-9]+")]
        [StringLength(255)]
        [Required]
        public string Country { get; set; }

        [StringLength(255)]
        [MinLength(8)]
        [Required]
        public string Password { get; set; }

        [Required]
        public string UserRole { get; set; }

        // Street only contains characters
        [RegularExpression(@"[^0-9]+")]
        public string Street { get; set; }

        public string StreetNumber { get; set; }

        public string Zipcode { get; set; }

        // City only contains characters
        [RegularExpression(@"[^0-9]+")]
        public string City { get; set; }
    }
}
