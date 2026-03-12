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
        public Administrador? Admin { get; set; }

        [Column("id_tecnico")]
        public int? TechnicianId { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string? Status { get; set; } = "Pedido";
    }
}
