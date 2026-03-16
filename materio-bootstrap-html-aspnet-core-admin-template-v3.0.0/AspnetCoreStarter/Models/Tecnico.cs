using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("tecnicos")]
    public class Tecnico
    {
        [Key]
        [Column("id_utilizador")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Column("area_tecnica")]
        [MaxLength(100)]
        public string? AreaTecnica { get; set; }

        [Column("nivel")]
        [MaxLength(50)]
        public string? Nivel { get; set; }
    }
}
