using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("historico")]
    public class Historico
    {
        [Key]
        [Column("id_historico")]
        public int Id { get; set; }

        [Column("data_mudanca")]
        public DateTime? ChangedAt { get; set; }

        [Column("estado")]
        [MaxLength(50)]
        public string? State { get; set; }

        [Column("id_equipamento")]
        public int EquipamentoId { get; set; }

        [ForeignKey("EquipamentoId")]
        public Equipamento? Equipamento { get; set; }
    }
}
