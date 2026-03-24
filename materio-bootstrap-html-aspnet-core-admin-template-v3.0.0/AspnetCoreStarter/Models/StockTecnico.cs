using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("stock_tecnico")]
    public class StockTecnico
    {
        [Key]
        [Column("id_stock_tecnico")]
        public int Id { get; set; }

        [Column("nome_equipamento")]
        [MaxLength(100)]
        public string? EquipmentName { get; set; }

        [Column("descricao")]
        public string? Description { get; set; }

        [Column("disponivel")]
        public bool IsAvailable { get; set; }

        [Column("id_tecnico")]
        public int? TechnicianId { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; } = "Disponível";
    }
}
