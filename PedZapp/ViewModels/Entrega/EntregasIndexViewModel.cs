namespace PedZapp.ViewModels.Entrega
{
    public class EntregasIndexViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string? Busca { get; set; }
        public string? Status { get; set; }
        public IReadOnlyList<BairroEntregaListViewModel> Bairros { get; set; } = [];
        public BairroEntregaFormViewModel NovoBairro { get; set; } = new();
    }

    public class BairroEntregaListViewModel
    {
        public int Id { get; set; }
        public string NomeBairro { get; set; } = string.Empty;
        public decimal TaxaEntrega { get; set; }
        public int? TempoEstimadoEntregaMinutos { get; set; }
        public decimal? PedidoMinimo { get; set; }
        public bool Ativo { get; set; }
        public int OrdemExibicao { get; set; }
    }
}
