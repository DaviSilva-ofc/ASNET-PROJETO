using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("tickets")]
    public class Ticket : ISoftDeletable
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

        [Column("id_solicitante")]
        public int? RequestedByUserId { get; set; }

        [ForeignKey("RequestedByUserId")]
        public virtual User? RequestedBy { get; set; }

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

        [Column("data_aceitacao")]
        public DateTime? AcceptedAt { get; set; }

        [Column("data_conclusao")]
        public DateTime? CompletedAt { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("satisfacao_rating")]
        public int? SatisfacaoRating { get; set; }

        [Column("satisfacao_feedback")]
        public string? SatisfacaoFeedback { get; set; }

        [Column("data_avaliacao")]
        public DateTime? DataAvaliacao { get; set; }

        public virtual ICollection<StockEmpresa> UtilizedStocks { get; set; } = new List<StockEmpresa>();

        [InverseProperty("AssociatedTicket")]
        public virtual ICollection<Equipamento> UtilizedEquipments { get; set; } = new List<Equipamento>();
    }
}
