namespace PedZapp.Models
{
    public class BairroEntrega
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
        public string NomeBairro { get; set; } = string.Empty;
        public decimal TaxaEntrega { get; set; }
        public int? TempoEstimadoEntregaMinutos { get; set; }
        public decimal? PedidoMinimo { get; set; }
        public bool Ativo { get; set; } = true;
        public int OrdemExibicao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}
