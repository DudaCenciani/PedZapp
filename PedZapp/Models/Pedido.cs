using PedZapp.Enums;

namespace PedZapp.Models
{
    /// <summary>
    /// Registro imutável da venda no contexto da empresa. Itens e adicionais preservam snapshots
    /// de nome e preço para que alterações posteriores no cardápio não modifiquem pedidos já criados.
    /// </summary>
    public class Pedido
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
        public string NumeroPedido { get; set; } = string.Empty;
        public string CodigoPublico { get; set; } = string.Empty;
        public Guid ChaveIdempotencia { get; set; }
        public OrigemPedido Origem { get; set; } = OrigemPedido.Site;
        public int? ComandaId { get; set; }
        public Comanda? Comanda { get; set; }
        public string? CriadoPorUsuarioId { get; set; }
        public string? NomeFuncionarioSnapshot { get; set; }
        public StatusPedido Status { get; set; } = StatusPedido.Novo;
        public TipoAtendimento TipoAtendimento { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string TelefoneCliente { get; set; } = string.Empty;
        // Consentimento operacional obtido no checkout; não é utilizado para mensagens de marketing.
        public bool AceitaAtualizacoesWhatsApp { get; set; }
        // Campos de auditoria evitam duplicidade e permitem reenvio controlado após uma falha da API oficial.
        public DateTime? WhatsAppConfirmacaoEmProcessamentoEm { get; set; }
        public DateTime? WhatsAppConfirmacaoEnviadaEm { get; set; }
        public DateTime? WhatsAppConfirmacaoFalhouEm { get; set; }
        public int? WhatsAppConfirmacaoUltimoStatusHttp { get; set; }
        public int? BairroEntregaId { get; set; }
        public BairroEntrega? BairroEntrega { get; set; }
        public string? NomeBairroSnapshot { get; set; }
        public decimal TaxaEntrega { get; set; }
        public string? Rua { get; set; }
        public string? NumeroEndereco { get; set; }
        public string? Complemento { get; set; }
        public string? Referencia { get; set; }
        // Pedidos de mesa são enviados à cozinha antes do pagamento ser definido no fechamento da comanda.
        public int? FormaPagamentoId { get; set; }
        public FormaPagamento? FormaPagamento { get; set; }
        public string NomeFormaPagamentoSnapshot { get; set; } = string.Empty;
        public bool PrecisaTroco { get; set; }
        public decimal? TrocoPara { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string? ObservacaoGeral { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAtualizacao { get; set; }
        public bool Pago { get; set; }
        public bool Cancelado { get; set; }
        public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
        public ICollection<ImpressaoPedido> Impressoes { get; set; } = new List<ImpressaoPedido>();
    }
}
