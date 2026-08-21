namespace PedZapp.ViewModels.Pedido
{
    /// <summary>
    /// Dados mínimos e não sensíveis enviados ao painel administrativo após a criação de um pedido público.
    /// A URL ainda é protegida pelo controller, que valida slug, autenticação e EmpresaId antes de exibir detalhes.
    /// </summary>
    public sealed class NovoPedidoNotificacaoViewModel
    {
        public string EventoId { get; init; } = string.Empty;
        public string NumeroPedido { get; init; } = string.Empty;
        public string NomeCliente { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public string TipoAtendimento { get; init; } = string.Empty;
        public DateTime DataCriacao { get; init; }
        public string UrlDetalhes { get; init; } = string.Empty;
    }
}
