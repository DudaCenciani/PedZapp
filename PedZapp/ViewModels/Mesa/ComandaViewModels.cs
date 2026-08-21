using System.ComponentModel.DataAnnotations;
using PedZapp.Enums;

namespace PedZapp.ViewModels.Mesa
{
    public class ComandaViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string NomeEmpresa { get; set; } = string.Empty;
        public int MesaId { get; set; }
        public string NomeMesa { get; set; } = string.Empty;
        public string NumeroComanda { get; set; } = string.Empty;
        public StatusComanda Status { get; set; }
        public DateTime DataAbertura { get; set; }
        public string Funcionario { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal PercentualTaxaServico { get; set; }
        public decimal ValorTaxaServico { get; set; }
        public bool TaxaServicoAplicada { get; set; }
        public decimal Total { get; set; }
        public string? FormaPagamento { get; set; }
        public bool PrecisaTroco { get; set; }
        public decimal? TrocoPara { get; set; }
        public IReadOnlyList<ComandaItemViewModel> Itens { get; set; } = [];
        public IReadOnlyList<ComandaCatalogoProdutoViewModel> Produtos { get; set; } = [];
        public IReadOnlyList<ComandaCatalogoAdicionalViewModel> Adicionais { get; set; } = [];
        public IReadOnlyList<ComandaFormaPagamentoViewModel> FormasPagamento { get; set; } = [];
    }
    public class ComandaItemInputViewModel
    {
        public int ProdutoId { get; set; }
        [Range(1, 99)] public int Quantidade { get; set; }
        [StringLength(500)] public string? Observacao { get; set; }
        public List<int> AdicionalIds { get; set; } = [];
    }
    public class FecharComandaInputViewModel
    {
        public bool TaxaServicoAplicada { get; set; }
        [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true)] public decimal PercentualTaxaServico { get; set; }
        public int FormaPagamentoId { get; set; }
        public bool PrecisaTroco { get; set; }
        [Range(typeof(decimal), "0.01", "999999999.99", ParseLimitsInInvariantCulture = true)] public decimal? TrocoPara { get; set; }
    }
    public class ComandaItemViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string? Observacao { get; set; }
        public bool EnviadoParaCozinha { get; set; }
        public IReadOnlyList<string> Adicionais { get; set; } = [];
    }
    public class ComandaCatalogoProdutoViewModel
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public decimal? PrecoPromocional { get; set; }
    }
    public class ComandaCatalogoAdicionalViewModel { public int Id { get; set; } public int CategoriaId { get; set; } public string Nome { get; set; } = string.Empty; public decimal Preco { get; set; } }
    public class ComandaFormaPagamentoViewModel { public int Id { get; set; } public string Nome { get; set; } = string.Empty; public TipoFormaPagamento Tipo { get; set; } public bool AceitaTroco { get; set; } }
}
