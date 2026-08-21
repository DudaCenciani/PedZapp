using PedZapp.Enums;

namespace PedZapp.ViewModels.FormaPagamento
{
    public class FormasPagamentoIndexViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public IReadOnlyList<FormaPagamentoListViewModel> Formas { get; set; } = [];
        public FormaPagamentoFormViewModel NovaForma { get; set; } = new();
    }

    public class FormaPagamentoListViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TipoFormaPagamento Tipo { get; set; }
        public bool Ativa { get; set; }
        public bool AceitaTroco { get; set; }
        public int OrdemExibicao { get; set; }
        public string? Observacao { get; set; }
    }
}
