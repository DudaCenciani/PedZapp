using PedZapp.Enums;

namespace PedZapp.Models
{
    public class FormaPagamento
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TipoFormaPagamento Tipo { get; set; }
        public bool Ativa { get; set; } = true;
        public bool AceitaTroco { get; set; }
        public bool PagamentoNaEntrega { get; set; } = true;
        public int OrdemExibicao { get; set; }
        public string? Observacao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}
