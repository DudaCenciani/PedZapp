namespace PedZapp.Models
{
    /// <summary>
    /// Adicional selecionado em um item de pedido, mantido como snapshot independente do catálogo atual.
    /// </summary>
    public class PedidoItemAdicional
    {
        public int Id { get; set; }
        public int PedidoItemId { get; set; }
        public PedidoItem? PedidoItem { get; set; }
        public int? AdicionalId { get; set; }
        public Adicional? Adicional { get; set; }
        public string NomeAdicionalSnapshot { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; } = 1;
    }
}
