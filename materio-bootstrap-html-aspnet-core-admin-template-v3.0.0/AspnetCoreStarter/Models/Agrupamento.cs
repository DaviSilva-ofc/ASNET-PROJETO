using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("agrupamentos")]
    public class Agrupamento : ISoftDeletable
    {
        [Key]
        [Column("id_agrupamento")]
        public int Id { get; set; }

        [Column("nome_agrupamento")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<School> Schools { get; set; } = new List<School>();
    }
}
