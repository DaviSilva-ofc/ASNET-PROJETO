using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("coordenadores")]
    public class Coordenador
    {
        [Key]
        [Column("id_utilizador")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Column("id_escola")]
        public int? SchoolId { get; set; }

        [ForeignKey("SchoolId")]
        public virtual School? School { get; set; }
    }
}
