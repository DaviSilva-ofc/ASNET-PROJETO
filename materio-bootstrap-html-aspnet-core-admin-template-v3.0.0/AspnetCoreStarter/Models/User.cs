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
        [MaxLength(200)]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [Column("palavra_passe")]
        public string PasswordHash { get; set; }

        [Column("ProfilePhotoPath")]
        public string? ProfilePhotoPath { get; set; }

        [Column("PasswordResetToken")]
        public string? PasswordResetToken { get; set; }

        [Column("ResetTokenExpiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        [Column("data_criacao")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("status_conta")]
        [MaxLength(50)]
        public string AccountStatus { get; set; } = "Pendente";

        [Column("cargo")]
        [MaxLength(50)]
        public string Role { get; set; } = "User";
    }
}

