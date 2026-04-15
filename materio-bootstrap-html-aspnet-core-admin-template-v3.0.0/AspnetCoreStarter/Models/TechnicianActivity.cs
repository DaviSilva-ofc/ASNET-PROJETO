using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("technician_activities")]
    public class TechnicianActivity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("technician_id")]
        public int TechnicianId { get; set; }

        [Column("title")]
        [Required]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("activity_date")]
        public DateTime ActivityDate { get; set; }

        [Column("color")]
        public string? Color { get; set; } = "#f89223"; // Default ASNET Orange

        [Column("data_criacao")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("TechnicianId")]
        public virtual Tecnico? Technician { get; set; }
    }
}
