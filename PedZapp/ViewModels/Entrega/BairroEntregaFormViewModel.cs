using System.ComponentModel.DataAnnotations;

namespace PedZapp.ViewModels.Entrega
{
    public class BairroEntregaFormViewModel
    {
        [Required(ErrorMessage = "Informe o nome do bairro.")]
        [StringLength(160)]
        public string NomeBairro { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "Informe uma taxa válida.")]
        public decimal TaxaEntrega { get; set; }

        [Range(1, 1440, ErrorMessage = "Informe um tempo entre 1 e 1440 minutos.")]
        public int? TempoEstimadoEntregaMinutos { get; set; }

        [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "Informe um pedido mínimo válido.")]
        public decimal? PedidoMinimo { get; set; }

        [Range(0, 9999, ErrorMessage = "Informe uma ordem igual ou maior que zero.")]
        public int OrdemExibicao { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
