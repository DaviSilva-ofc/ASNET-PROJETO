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

        [Column("id_tecnico")]
        public int? TechnicianId { get; set; }

        [Column("id_admin")]
        public int? AdminId { get; set; }

        [ForeignKey("AdminId")]
        public Administrador? Admin { get; set; }
    }
}
