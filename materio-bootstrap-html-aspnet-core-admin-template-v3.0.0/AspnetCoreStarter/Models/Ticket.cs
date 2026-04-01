using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("tickets")]
    public class Ticket
    {
        [Key]
        [Column("id_ticket")]
        public int Id { get; set; }

        [Column("nivel")]
        [MaxLength(50)]
        public string? Level { get; set; }

        [Column("descricao")]
        public string? Description { get; set; }

        [Column("periodo")]
        [MaxLength(50)]
        public string? Period { get; set; }

        [Column("id_escola")]
        public int? SchoolId { get; set; }

        [ForeignKey("SchoolId")]
        public School? School { get; set; }

        [Column("id_admin")]
        public int? AdminId { get; set; }

        [ForeignKey("AdminId")]
        public User? Admin { get; set; }

        [Column("id_tecnico")]
        public int? TechnicianId { get; set; }

        [Column("id_equipamento")]
        public int? EquipamentoId { get; set; }

        [ForeignKey("EquipamentoId")]
        public Equipamento? Equipamento { get; set; }

        [ForeignKey("TechnicianId")]
        public virtual User? Technician { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; } = "Pedido";

        [Column("data_criacao")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
