using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("blocos")]
    public class Bloco : ISoftDeletable
    {
        [Key]
        [Column("id_bloco")]
        public int Id { get; set; }

        [Column("nome_bloco")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("id_escola")]
        public int SchoolId { get; set; }

        [ForeignKey("SchoolId")]
        public School School { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        public ICollection<Sala> Rooms { get; set; } = new List<Sala>();
    }
}
