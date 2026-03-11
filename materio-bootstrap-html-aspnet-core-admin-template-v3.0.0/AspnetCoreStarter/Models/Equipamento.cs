using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("equipamentos")]
    public class Equipamento
    {
        [Key]
        [Column("id_equipamento")]
        public int Id { get; set; }

        [Column("nome_equipamento")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("tipo")]
        [MaxLength(100)]
        public string Type { get; set; }

        [Column("numero_serie")]
        [MaxLength(100)]
        public string SerialNumber { get; set; }

        [Column("processador")]
        [MaxLength(100)]
        public string Processor { get; set; }

        [Column("discos")]
        [MaxLength(100)]
        public string Storage { get; set; }

        [Column("id_sala")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Sala Room { get; set; }
    }
}
