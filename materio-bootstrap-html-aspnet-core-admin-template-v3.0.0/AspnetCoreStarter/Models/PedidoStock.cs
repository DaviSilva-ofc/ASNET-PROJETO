using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreStarter.Models
{
    [Table("pedidos_stock")]
    public class PedidoStock
    {
        [Key]
        [Column("id_pedido")]
        public int Id { get; set; }

        [Column("nome_artigo")]
        [MaxLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [Column("tipo_artigo")]
        [MaxLength(100)]
        public string? ItemType { get; set; }

        [Column("quantidade")]
        public int Quantity { get; set; } = 1;

        [Column("notas")]
        public string? Notes { get; set; }

        [Column("id_coordenador")]
        public int? RequestedByUserId { get; set; }

        [ForeignKey("RequestedByUserId")]
        public virtual User? RequestedBy { get; set; }

        [Column("id_escola")]
        public int? SchoolId { get; set; }

        [ForeignKey("SchoolId")]
        public virtual School? School { get; set; }

        [Column("id_agrupamento")]
        public int? AgrupamentoId { get; set; }

        [ForeignKey("AgrupamentoId")]
        public virtual Agrupamento? Agrupamento { get; set; }

        /// <summary>
        /// Possíveis values: Pendente_Diretor, Pendente_Admin, Atendido, Recusado
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "Pendente_Diretor";

        [Column("notas_diretor")]
        public string? DirectorNotes { get; set; }

        [Column("id_admin")]
        public int? AdminId { get; set; }

        [ForeignKey("AdminId")]
        public virtual User? Admin { get; set; }

        [Column("data_criacao")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("data_atualizacao")]
        public DateTime? UpdatedAt { get; set; }
    }
}
