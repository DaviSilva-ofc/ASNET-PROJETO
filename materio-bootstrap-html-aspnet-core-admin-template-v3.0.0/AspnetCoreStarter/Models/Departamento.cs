using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("departamentos")]
    public class Departamento
    {
        [Key]
        [Column("id_departamento")]
        public int Id { get; set; }

        [Required]
        [Column("nome_departamento")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("id_empresa")]
        public int EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        public virtual ICollection<Setor> Setores { get; set; } = new List<Setor>();
    }
}
