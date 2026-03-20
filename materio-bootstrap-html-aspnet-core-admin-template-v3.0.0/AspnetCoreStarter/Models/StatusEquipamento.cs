using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("status_equipamento")]
    public class StatusEquipamento
    {
        [Key]
        [Column("id_status")]
        public int Id { get; set; }

        [Column("estado")]
        [MaxLength(50)]
        public string? State { get; set; }

        [Column("versao")]
        [MaxLength(50)]
        public string? Version { get; set; }

        [Column("empresa")]
        [MaxLength(100)]
        public string? Company { get; set; }

        [Column("id_equipamento")]
        public int EquipamentoId { get; set; }

        [ForeignKey("EquipamentoId")]
        public Equipamento? Equipamento { get; set; }
    }
}
