using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("emprestimos")]
    public class Emprestimo
    {
        [Key]
        [Column("id_emprestimo")]
        public int Id { get; set; }

        [Column("id_agrupamento")]
        public int AgrupamentoId { get; set; }

        [ForeignKey("AgrupamentoId")]
        public Agrupamento? Agrupamento { get; set; }

        [Column("data_emprestimo")]
        public DateTime? LoanDate { get; set; }

        [Column("tipo_emprestimo")]
        [MaxLength(50)]
        public string? LoanType { get; set; }

        [Column("id_equipamento")]
        public int EquipamentoId { get; set; }

        [ForeignKey("EquipamentoId")]
        public Equipamento? Equipamento { get; set; }
    }
}
