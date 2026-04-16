using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("empresas")]
    public class Empresa : ISoftDeletable
    {
        [Key]
        [Column("id_empresa")]
        public int Id { get; set; }

        [Required]
        [Column("nome_empresa")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("localizacao")]
        [MaxLength(255)]
        public string? Location { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        // Navigation property for users linked to this company
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
