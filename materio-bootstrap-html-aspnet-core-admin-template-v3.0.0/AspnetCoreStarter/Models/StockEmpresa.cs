using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("stock_empresa")]
    public class StockEmpresa
    {
        [Key]
        [Column("id_stock")]
        public int Id { get; set; }

        [Column("nome_equipamento")]
        [MaxLength(100)]
        public string? EquipmentName { get; set; }

        [Column("tipo")]
        [MaxLength(100)]
        public string? Type { get; set; }

        [Column("descricao")]
        public string? Description { get; set; }

        [Column("disponivel")]
        public bool IsAvailable { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; } = "Disponível";

        [Column("id_tecnico")]
        public int? TechnicianId { get; set; }

        [ForeignKey("TechnicianId")]
        public virtual Tecnico? Technician { get; set; }

        [Column("id_professor")]
        public int? ProfessorId { get; set; }

        [ForeignKey("ProfessorId")]
        public virtual Professor? Professor { get; set; }

        [Column("id_diretor")]
        public int? DirectorId { get; set; }

        [ForeignKey("DirectorId")]
        public virtual Diretor? Director { get; set; }

        [Column("id_agrupamento")]
        public int? AgrupamentoId { get; set; }

        [ForeignKey("AgrupamentoId")]
        public virtual Agrupamento? Agrupamento { get; set; }

        [Column("id_escola")]
        public int? SchoolId { get; set; }

        [ForeignKey("SchoolId")]
        public virtual School? School { get; set; }

        [Column("id_admin")]
        public int? AdminId { get; set; }

        [ForeignKey("AdminId")]
        public virtual Administrador? Admin { get; set; }

        [Column("id_empresa")]
        public int? EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }
    }
}
