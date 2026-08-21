using System.ComponentModel.DataAnnotations;

namespace PedZapp.Models
{
    /// <summary>
    /// Opção adicional pertencente a uma empresa. A disponibilidade por categoria é definida
    /// pela entidade de junção AdicionalCategoria, não diretamente pelo produto.
    /// </summary>
    public class Adicional
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        [Required]
        [StringLength(160)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descricao { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "999999999.99",
            ParseLimitsInInvariantCulture = true,
            ErrorMessage = "Informe um valor válido.")]
        public decimal Preco { get; set; }

        public bool Ativo { get; set; } = true;
        public int? MaximoSelecao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public ICollection<AdicionalCategoria> AdicionalCategorias { get; set; } = new List<AdicionalCategoria>();
    }
}
