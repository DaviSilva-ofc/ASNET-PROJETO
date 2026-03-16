using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("professores")]
    public class Professor
    {
        [Key]
        [Column("id_utilizador")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Column("id_bloco")]
        public int? BlocoId { get; set; }

        [ForeignKey("BlocoId")]
        public virtual Bloco? Bloco { get; set; }
    }
}
