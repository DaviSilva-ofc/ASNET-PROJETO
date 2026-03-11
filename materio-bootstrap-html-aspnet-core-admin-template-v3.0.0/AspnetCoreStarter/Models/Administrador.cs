using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("administradores")]
    public class Administrador
    {
        [Key]
        [Column("id_utilizador")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("id_agrupamento")]
        public int? AgrupamentoId { get; set; }

        [ForeignKey("AgrupamentoId")]
        public Agrupamento? Agrupamento { get; set; }
    }
}
