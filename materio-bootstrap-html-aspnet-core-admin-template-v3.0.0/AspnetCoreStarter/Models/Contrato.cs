using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("contratos")]
    public class Contrato
    {
        [Key]
        [Column("id_contrato")]
        public int Id { get; set; }

        [Column("periodo")]
        [MaxLength(50)]
        public string? Period { get; set; }

        [Column("tipo_contrato")]
        [MaxLength(50)]
        public string? ContractType { get; set; }

        [Column("status_contrato")]
        [MaxLength(50)]
        public string? ContractStatus { get; set; }

        [Column("descricao")]
        public string? Description { get; set; }

        [Column("nivel_urgencia")]
        [MaxLength(20)]
        public string? UrgencyLevel { get; set; }

        [Column("id_agrupamento")]
        public int? AgrupamentoId { get; set; }

        [ForeignKey("AgrupamentoId")]
        public Agrupamento? Agrupamento { get; set; }

        [Column("id_admin")]
        public int? AdminId { get; set; }

        [ForeignKey("AdminId")]
        public Administrador? Admin { get; set; }
    }
}
