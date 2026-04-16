using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("contratos")]
    public class Contrato : ISoftDeletable
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

        [Column("id_escola")]
        public int? SchoolId { get; set; }

        [ForeignKey("SchoolId")]
        public School? School { get; set; }

        [Column("id_empresa")]
        public int? EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa? Empresa { get; set; }

        [Column("id_admin")]
        public int? AdminId { get; set; }

        [ForeignKey("AdminId")]
        public Administrador? Admin { get; set; }

        [Column("data_expiracao")]
        public DateTime? ExpiryDate { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;
    }
}
