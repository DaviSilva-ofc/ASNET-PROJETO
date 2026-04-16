using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("salas")]
    public class Sala : ISoftDeletable
    {
        [Key]
        [Column("id_sala")]
        public int Id { get; set; }

        [Column("nome_sala")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("id_bloco")]
        public int? BlockId { get; set; }

        [ForeignKey("BlockId")]
        public virtual Bloco? Block { get; set; }

        [Column("id_setor")]
        public int? SetorId { get; set; }

        [ForeignKey("SetorId")]
        public virtual Setor? Setor { get; set; }

        [Column("id_professor_responsavel")]
        public int? ResponsibleProfessorId { get; set; }

        [ForeignKey("ResponsibleProfessorId")]
        public virtual Professor? ResponsibleProfessor { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        public ICollection<Equipamento> Equipments { get; set; } = new List<Equipamento>();
    }
}
