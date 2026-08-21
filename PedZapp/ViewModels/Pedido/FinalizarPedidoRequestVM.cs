using System.ComponentModel.DataAnnotations;
using PedZapp.Enums;

namespace PedZapp.ViewModels.Pedido
{
    /// <summary>
    /// Dados de intenção enviados pelo checkout público e pela tela de pedido manual.
    /// Não contém EmpresaId, preços, totais ou status para evitar overposting e valores adulterados.
    /// </summary>
    public class FinalizarPedidoRequestVM
    {
        public string ChaveIdempotencia { get; set; } = string.Empty;
        public TipoAtendimento TipoAtendimento { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string TelefoneCliente { get; set; } = string.Empty;
        // Opt-in específico para atualizações deste pedido pelo WhatsApp, distinto de marketing.
        public bool AceitaAtualizacoesWhatsApp { get; set; }
        public int? BairroEntregaId { get; set; }
        // Rua é opcional para atender zonas rurais e pontos de referência; o serviço normaliza texto vazio para null ao salvar.
        [StringLength(160)]
        public string? Rua { get; set; }
        public string? NumeroEndereco { get; set; }
        public bool SemNumero { get; set; }
        public string? Complemento { get; set; }
        public string? Referencia { get; set; }
        public int FormaPagamentoId { get; set; }
        public bool PrecisaTroco { get; set; }
        public string? TrocoPara { get; set; }
        public string? ObservacaoGeral { get; set; }
        public List<FinalizarPedidoItemRequestVM> Itens { get; set; } = [];
    }

    public class FinalizarPedidoItemRequestVM
    {
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public string? Observacao { get; set; }
        public List<int> AdicionalIds { get; set; } = [];
    }

    public class PedidoConfirmacaoPublicaViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public string NumeroPedido { get; set; } = string.Empty;
        public string CodigoPublico { get; set; } = string.Empty;
        public string TipoAtendimento { get; set; } = string.Empty;
        public string FormaPagamento { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
