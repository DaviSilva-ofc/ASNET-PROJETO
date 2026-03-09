using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("Schools")]
    public class School
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string ContactEmail { get; set; }

        [MaxLength(50)]
        public string Phone { get; set; }

        [MaxLength(100)]
        public string? Grouping { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Relation to users (Teachers)
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
