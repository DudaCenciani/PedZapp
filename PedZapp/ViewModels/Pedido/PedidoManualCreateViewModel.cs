using PedZapp.Enums;

namespace PedZapp.ViewModels.Pedido
{
    /// <summary>
    /// Catálogo reduzido que o PedidosController entrega à tela de pedido manual.
    /// Os valores exibidos servem apenas de referência; a criação usa FinalizarPedidoRequestVM e recálculo no servidor.
    /// </summary>
    public class PedidoManualCreateViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public IReadOnlyList<PedidoManualCategoriaViewModel> Categorias { get; set; } = [];
        public IReadOnlyList<PedidoManualProdutoViewModel> Produtos { get; set; } = [];
        public IReadOnlyList<PedidoManualAdicionalViewModel> Adicionais { get; set; } = [];
        public IReadOnlyList<PedidoManualBairroViewModel> Bairros { get; set; } = [];
        public IReadOnlyList<PedidoManualFormaPagamentoViewModel> FormasPagamento { get; set; } = [];
    }

    public class PedidoManualCategoriaViewModel { public int Id { get; set; } public string Nome { get; set; } = string.Empty; }
    public class PedidoManualProdutoViewModel { public int Id { get; set; } public int CategoriaId { get; set; } public string Nome { get; set; } = string.Empty; public decimal Preco { get; set; } public decimal? PrecoPromocional { get; set; } public bool PermiteObservacao { get; set; } }
    public class PedidoManualAdicionalViewModel { public int Id { get; set; } public int CategoriaId { get; set; } public string Nome { get; set; } = string.Empty; public decimal Preco { get; set; } }
    public class PedidoManualBairroViewModel { public int Id { get; set; } public string Nome { get; set; } = string.Empty; public decimal TaxaEntrega { get; set; } public decimal? PedidoMinimo { get; set; } }
    public class PedidoManualFormaPagamentoViewModel { public int Id { get; set; } public string Nome { get; set; } = string.Empty; public TipoFormaPagamento Tipo { get; set; } public bool AceitaTroco { get; set; } }
}
