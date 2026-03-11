using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("escolas")]
    public class School
    {
        [Key]
        [Column("id_escola")]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("nome_escola")]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("localizacao")]
        public string Address { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        [Column("ContactEmail")]
        public string ContactEmail { get; set; }

        [MaxLength(50)]
        [Column("Phone")]
        public string Phone { get; set; }

        [MaxLength(100)]
        [Column("Grouping")]
        public int? AgrupamentoId { get; set; }

        [ForeignKey("AgrupamentoId")]
        public virtual Agrupamento? Agrupamento { get; set; }

        [Column("RegisteredAt")]
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}
