using PedZapp.Enums;

namespace PedZapp.ViewModels.Pedido
{
    /// <summary>
    /// Projeção específica para a via de impressão, preenchida pelo BrowserPedidoPrintService.
    /// A View escolhe quais campos exibir conforme o tipo da via, mantendo a cozinha sem dados pessoais desnecessários.
    /// </summary>
    public class PedidoImpressaoViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string CodigoPublico { get; set; } = string.Empty;
        public string TokenImpressao { get; set; } = string.Empty;
        public string NomeEmpresa { get; set; } = string.Empty;
        public string NumeroPedido { get; set; } = string.Empty;
        public string? Mesa { get; set; }
        public string? NumeroComanda { get; set; }
        public string? Funcionario { get; set; }
        public DateTime DataCriacao { get; set; }
        public OrigemPedido Origem { get; set; }
        public TipoImpressao TipoImpressao { get; set; }
        public TipoAtendimento TipoAtendimento { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string TelefoneCliente { get; set; } = string.Empty;
        public string? Bairro { get; set; }
        public string? Rua { get; set; }
        public string? NumeroEndereco { get; set; }
        public string? Complemento { get; set; }
        public string? Referencia { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public bool PrecisaTroco { get; set; }
        public decimal? TrocoPara { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxaEntrega { get; set; }
        public decimal Total { get; set; }
        public string? ObservacaoGeral { get; set; }
        public IReadOnlyList<PedidoImpressaoItemViewModel> Itens { get; set; } = [];
    }

    public class PedidoImpressaoItemViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public int Quantidade { get; set; }

        // Valores registrados no item do pedido para que a impressão preserve o histórico,
        // mesmo que o preço do produto seja alterado posteriormente no cardápio.
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal { get; set; }

        public string? Observacao { get; set; }
        public IReadOnlyList<string> Adicionais { get; set; } = [];
    }

    public sealed class SolicitacaoImpressaoResultado
    {
        public bool Sucesso { get; private init; }
        public bool PedidoNaoEncontrado { get; private init; }
        public string? CodigoPublico { get; private init; }
        public string? TokenPublico { get; private init; }
        public string? Erro { get; private init; }
        public static SolicitacaoImpressaoResultado Criada(string codigoPublico, string tokenPublico) => new() { Sucesso = true, CodigoPublico = codigoPublico, TokenPublico = tokenPublico };
        public static SolicitacaoImpressaoResultado NaoEncontrado() => new() { PedidoNaoEncontrado = true };
        public static SolicitacaoImpressaoResultado Falha(string erro) => new() { Erro = erro };
    }
}
