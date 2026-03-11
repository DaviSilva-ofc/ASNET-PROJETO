using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("salas")]
    public class Sala
    {
        [Key]
        [Column("id_sala")]
        public int Id { get; set; }

        [Column("nome_sala")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("id_bloco")]
        public int BlockId { get; set; }

        [ForeignKey("BlockId")]
        public Bloco Block { get; set; }

        public ICollection<Equipamento> Equipments { get; set; } = new List<Equipamento>();
    }
}
