using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("reparos")]
    public class Reparo : ISoftDeletable
    {
        [Key]
        [Column("id_reparo")]
        public int Id { get; set; }

        [Column("id_equipamento")]
        public int EquipamentoId { get; set; }

        [ForeignKey("EquipamentoId")]
        public Equipamento? Equipamento { get; set; }

        [Column("id_tecnico")]
        public int TecnicoId { get; set; }

        [ForeignKey("TecnicoId")]
        public Tecnico? Tecnico { get; set; }

        [Column("descricao_avaria")]
        public string? FaultDescription { get; set; }

        [Column("data_reparo")]
        public DateTime? RepairDate { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;
    }
}
