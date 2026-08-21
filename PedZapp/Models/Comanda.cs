using PedZapp.Enums;

namespace PedZapp.Models
{
    /// <summary>Consumo aberto de uma mesa. Apenas uma comanda ativa pode existir por mesa.</summary>
    public class Comanda
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
        public int MesaId { get; set; }
        public Mesa? Mesa { get; set; }
        public string NumeroComanda { get; set; } = string.Empty;
        public StatusComanda Status { get; set; } = StatusComanda.Aberta;
        public string CriadaPorUsuarioId { get; set; } = string.Empty;
        public ApplicationUser? CriadaPorUsuario { get; set; }
        public string NomeFuncionarioSnapshot { get; set; } = string.Empty;
        public DateTime DataAbertura { get; set; } = DateTime.UtcNow;
        public DateTime? DataFechamento { get; set; }
        public decimal Subtotal { get; set; }
        public decimal PercentualTaxaServico { get; set; } = 10m;
        public decimal ValorTaxaServico { get; set; }
        public bool TaxaServicoAplicada { get; set; }
        public decimal Total { get; set; }
        public int? FormaPagamentoId { get; set; }
        public FormaPagamento? FormaPagamento { get; set; }
        public string? NomeFormaPagamentoSnapshot { get; set; }
        public bool PrecisaTroco { get; set; }
        public decimal? TrocoPara { get; set; }
        public string? Observacao { get; set; }
        public bool Ativa { get; set; } = true;
        public string CodigoPublicoSeguro { get; set; } = Guid.NewGuid().ToString("N");
        public ICollection<ComandaItem> Itens { get; set; } = new List<ComandaItem>();
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
