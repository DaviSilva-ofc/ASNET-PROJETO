using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    public enum TipoAcaoHistorico
    {
        Criacao,
        Status,
        Equipamento,
        Comentario
    }

    [Table("ticket_historico")]
    public class TicketHistorico
    {
        [Key]
        [Column("id_historico")]
        public int Id { get; set; }

        [Column("id_ticket")]
        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [Column("data_acao")]
        public DateTime Data { get; set; } = DateTime.UtcNow;

        [Column("acao")]
        public string? Acao { get; set; }

        [Column("autor")]
        [MaxLength(100)]
        public string? Autor { get; set; }

        [Column("tipo_acao")]
        public TipoAcaoHistorico TipoAcao { get; set; }
    }
}
