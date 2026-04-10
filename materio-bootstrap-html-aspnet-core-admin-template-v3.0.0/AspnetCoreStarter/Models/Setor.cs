using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("setores")]
    public class Setor
    {
        [Key]
        [Column("id_setor")]
        public int Id { get; set; }

        [Required]
        [Column("nome_setor")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("id_departamento")]
        public int DepartamentoId { get; set; }

        [ForeignKey("DepartamentoId")]
        public virtual Departamento? Departamento { get; set; }

        public virtual ICollection<Sala> Salas { get; set; } = new List<Sala>();
    }
}
