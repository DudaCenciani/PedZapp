namespace PedZapp.Models
{
    /// <summary>
    /// Item de um pedido, incluindo o preço unitário e a observação registrados no momento da compra.
    /// </summary>
    public class PedidoItem
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }
        public int? ProdutoId { get; set; }
        public Produto? Produto { get; set; }
        public string NomeProdutoSnapshot { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
        public string? Observacao { get; set; }
        public decimal Subtotal { get; set; }
        public ICollection<PedidoItemAdicional> Adicionais { get; set; } = new List<PedidoItemAdicional>();
    }
}
