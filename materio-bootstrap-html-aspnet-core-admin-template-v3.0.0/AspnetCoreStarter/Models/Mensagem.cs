using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("mensagens")]
    public class Mensagem
    {
        [Key]
        [Column("id_mensagem")]
        public int Id { get; set; }

        [Column("id_remetente")]
        public int SenderId { get; set; }

        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }

        [Column("id_destinatario")]
        public int ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; }

        [Required]
        [Column("conteudo")]
        public string Content { get; set; }

        [Column("lida")]
        public bool IsRead { get; set; } = false;

        [Column("data_envio")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
