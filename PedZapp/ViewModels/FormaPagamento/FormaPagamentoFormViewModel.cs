using System.ComponentModel.DataAnnotations;
using PedZapp.Enums;

namespace PedZapp.ViewModels.FormaPagamento
{
    public class FormaPagamentoFormViewModel
    {
        [Required(ErrorMessage = "Informe o nome da forma de pagamento.")]
        [StringLength(160)]
        public string Nome { get; set; } = string.Empty;

        [EnumDataType(typeof(TipoFormaPagamento))]
        public TipoFormaPagamento Tipo { get; set; }

        public bool AceitaTroco { get; set; }
        public bool Ativa { get; set; } = true;

        [Range(0, 9999, ErrorMessage = "Informe uma ordem igual ou maior que zero.")]
        public int OrdemExibicao { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }
    }
}
