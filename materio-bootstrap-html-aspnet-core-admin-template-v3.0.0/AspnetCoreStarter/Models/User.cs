using System;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreStarter.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string ProfilePhotoPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
