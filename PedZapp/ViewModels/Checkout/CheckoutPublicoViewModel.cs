namespace PedZapp.ViewModels.Checkout
{
    /// <summary>
    /// Opções públicas de entrega e pagamento para o checkout. O POST não confia nestes valores
    /// e os valida novamente no PedidoService antes de criar o pedido.
    /// </summary>
    public class CheckoutPublicoViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public string CorPrimaria { get; set; } = "#F6C445";
        public string CorSecundaria { get; set; } = "#C98D86";
        public IReadOnlyList<BairroCheckoutViewModel> Bairros { get; set; } = [];
        public IReadOnlyList<FormaPagamentoCheckoutViewModel> FormasPagamento { get; set; } = [];
    }

    public class BairroCheckoutViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal TaxaEntrega { get; set; }
        public int? TempoEstimadoEntregaMinutos { get; set; }
        public decimal? PedidoMinimo { get; set; }
    }

    public class FormaPagamentoCheckoutViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Tipo { get; set; }
        public bool AceitaTroco { get; set; }
        public bool PagamentoNaEntrega { get; set; }
        public string? Observacao { get; set; }
    }
}
