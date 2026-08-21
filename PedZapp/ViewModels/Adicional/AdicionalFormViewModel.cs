using System.ComponentModel.DataAnnotations;

namespace PedZapp.ViewModels.Adicional
{
    public class AdicionalFormViewModel
    {
        [Required(ErrorMessage = "Informe o nome do adicional.")]
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

        [Range(1, 100, ErrorMessage = "Informe um máximo entre 1 e 100.")]
        public int? MaximoSelecao { get; set; }

        public bool Ativo { get; set; } = true;
        public List<int> CategoriaIds { get; set; } = [];
    }
}
