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

        [Column("numero_serie")]
        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        [Column("processador")]
        [MaxLength(100)]
        public string? Processor { get; set; }

        [Column("discos")]
        [MaxLength(100)]
        public string? Storage { get; set; }

        [Column("placa_video")]
        [MaxLength(100)]
        public string? GraphicsCard { get; set; }

        [Column("bateria")]
        [MaxLength(100)]
        public string? Battery { get; set; }

        [Column("cooler")]
        [MaxLength(100)]
        public string? Cooler { get; set; }

        [Column("memoria")]
        [MaxLength(100)]
        public string? Memory { get; set; }

        [Column("memoria_ram")]
        [MaxLength(100)]
        public string? Ram { get; set; }

        [Column("mac_address")]
        [MaxLength(100)]
        public string? MacAddress { get; set; }

        [Column("ip")]
        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [Column("id_sala")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Sala? Room { get; set; }
    }
}
