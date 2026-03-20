using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("componentes_equipamentos")]
    public class ComponenteEquipamento
    {
        [Key]
        [Column("id_componente")]
        public int Id { get; set; }

        [Column("id_equipamento")]
        public int EquipamentoId { get; set; }

        [ForeignKey("EquipamentoId")]
        public Equipamento? Equipamento { get; set; }

        [Column("processador")]
        [MaxLength(100)]
        public string? Processor { get; set; }

        [Column("memoria_ram")]
        [MaxLength(100)]
        public string? Ram { get; set; }

        [Column("armazenamento")]
        [MaxLength(100)]
        public string? Storage { get; set; }

        [Column("placa_grafica")]
        [MaxLength(100)]
        public string? GraphicsCard { get; set; }

        [Column("sistema_operativo")]
        [MaxLength(100)]
        public string? OS { get; set; }

        [Column("bateria")]
        [MaxLength(100)]
        public string? Battery { get; set; }

        [Column("cooler")]
        [MaxLength(100)]
        public string? Cooler { get; set; }

        [Column("fonte_alimentacao")]
        [MaxLength(100)]
        public string? PowerSupply { get; set; }

        [Column("motherboard")]
        [MaxLength(100)]
        public string? Motherboard { get; set; }

        [Column("observacoes")]
        public string? Observations { get; set; }
    }
}
