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

        [Column("nome_escola")]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Column("localizacao")]
        [MaxLength(100)]
        public string? Address { get; set; }

        [Column("id_agrupamento")]
        public int? AgrupamentoId { get; set; }

        [ForeignKey("AgrupamentoId")]
        public virtual Agrupamento? Agrupamento { get; set; }
    }
}
