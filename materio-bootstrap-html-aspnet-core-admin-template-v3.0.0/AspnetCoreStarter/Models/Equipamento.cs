using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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
        public virtual Sala? Room { get; set; }

        [Column("id_empresa")]
        public int? EmpresaId { get; set; }
        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; } = "A funcionar";

        public virtual ICollection<StatusEquipamento> StatusEquipamentos { get; set; } = new List<StatusEquipamento>();
    }
}
