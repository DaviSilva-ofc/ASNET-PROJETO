using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("Users")]
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

        public string? ProfilePhotoPath { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Teacher specific fields ---


        [MaxLength(100)]
        public string? Subject { get; set; } // Disciplina

        [MaxLength(100)]
        public string? Grouping { get; set; } // Agrupamento

        // Default role could be "Membro" or "Professor"
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Membro";
    }
}
