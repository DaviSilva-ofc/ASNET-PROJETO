using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("utilizadores")]
    public class User
    {
        [Key]
        [Column("id_utilizador")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("nome")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        [Column("email")]
        public string Email { get; set; }

        [Column("palavra_passe")]
        [Required]
        [MaxLength(255)]
        public string Password { get; set; } // Map to 'palavra_passe'

        // We use PasswordHash for our logic as requested by the user's schema additions
        [Required]
        [Column("PasswordHash")]
        public string PasswordHash { get; set; }

        public string? ProfilePhotoPath { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        [Column("data_criacao")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("status_conta")]
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Membro";
    }
}
