namespace PedZapp.Models
{
    /// <summary>Item lançado na comanda antes de ser enviado à cozinha como pedido presencial.</summary>
    public class ComandaItem
    {
        public int Id { get; set; }
        public int ComandaId { get; set; }
        public Comanda? Comanda { get; set; }
        public int ProdutoId { get; set; }
        public Produto? Produto { get; set; }
        public string NomeProdutoSnapshot { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
        public string? Observacao { get; set; }
        public decimal Subtotal { get; set; }
        public bool EnviadoParaCozinha { get; set; }
        public ICollection<ComandaItemAdicional> Adicionais { get; set; } = new List<ComandaItemAdicional>();
    }
}
