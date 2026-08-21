using PedZapp.Enums;

namespace PedZapp.ViewModels.Pedido
{
    public class PedidosIndexViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string? Busca { get; set; }
        public StatusPedido? Status { get; set; }
        public DateTime? DataInicial { get; set; }
        // Texto pronto para informar o período realmente aplicado na consulta administrativa.
        public string PeriodoExibicao { get; set; } = string.Empty;
        public int TotalHoje { get; set; }
        public int TotalNovos { get; set; }
        public int TotalEmPreparo { get; set; }
        public int TotalFinalizados { get; set; }
        // A View usa este valor para exibir somente as colunas permitidas à empresa.
        public TipoFluxoPedido TipoFluxoPedido { get; set; } = TipoFluxoPedido.Completo;
        public IReadOnlyList<PedidoCardViewModel> Pedidos { get; set; } = [];
    }

    public class PedidoCardViewModel
    {
        public int Id { get; set; }
        public string CodigoPublico { get; set; } = string.Empty;
        public string NumeroPedido { get; set; } = string.Empty;
        public string NomeCliente { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public StatusPedido Status { get; set; }
        public TipoAtendimento TipoAtendimento { get; set; }
        public decimal Total { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public int QuantidadeItens { get; set; }
        public bool Pago { get; set; }
        // Informações presenciais projetadas somente para identificar o lote de mesa no Kanban da própria empresa.
        public string? NomeMesa { get; set; }
        public string? NumeroComanda { get; set; }
        public string? NomeFuncionario { get; set; }
        public StatusImpressao? UltimaImpressaoStatus { get; set; }
    }

    public class PedidoDetalhesViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public int Id { get; set; }
        public string CodigoPublico { get; set; } = string.Empty;
        public string NumeroPedido { get; set; } = string.Empty;
        public StatusPedido Status { get; set; }
        public DateTime DataCriacao { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string TelefoneCliente { get; set; } = string.Empty;
        public TipoAtendimento TipoAtendimento { get; set; }
        public string? Bairro { get; set; }
        public decimal TaxaEntrega { get; set; }
        public string? Rua { get; set; }
        public string? NumeroEndereco { get; set; }
        public string? Complemento { get; set; }
        public string? Referencia { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public bool PrecisaTroco { get; set; }
        public decimal? TrocoPara { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public bool Pago { get; set; }
        // Estado administrativo da confirmação opcional enviada pela API oficial da Meta.
        public bool AceitaAtualizacoesWhatsApp { get; set; }
        public DateTime? WhatsAppConfirmacaoEnviadaEm { get; set; }
        public DateTime? WhatsAppConfirmacaoFalhouEm { get; set; }
        public StatusImpressao? UltimaImpressaoStatus { get; set; }
        public IReadOnlyList<PedidoDetalheItemViewModel> Itens { get; set; } = [];
    }

    public class PedidoDetalheItemViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string? Observacao { get; set; }
        public IReadOnlyList<PedidoDetalheAdicionalViewModel> Adicionais { get; set; } = [];
    }

    public class PedidoDetalheAdicionalViewModel { public string Nome { get; set; } = string.Empty; public decimal PrecoUnitario { get; set; } public int Quantidade { get; set; } }
}
