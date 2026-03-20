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
        public string? Name { get; set; }

        [Column("tipo")]
        [MaxLength(100)]
        public string? Type { get; set; }

        [Column("marca")]
        [MaxLength(100)]
        public string? Brand { get; set; }

        [Column("modelo")]
        [MaxLength(100)]
        public string? Model { get; set; }

        [Column("numero_patrimonio")]
        public long? AssetNumber { get; set; }

        [Column("numero_serie")]
        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        [Column("mac_address")]
        [MaxLength(100)]
        public string? MacAddress { get; set; }

        [Column("ip")]
        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [Column("id_sala")]
        public int? RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Sala? Room { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; } = "Funcionando";
    }
}
