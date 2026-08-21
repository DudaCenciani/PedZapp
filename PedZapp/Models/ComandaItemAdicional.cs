namespace PedZapp.Models
{
    public class ComandaItemAdicional
    {
        public int Id { get; set; }
        public int ComandaItemId { get; set; }
        public ComandaItem? ComandaItem { get; set; }
        public int AdicionalId { get; set; }
        public string NomeAdicionalSnapshot { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
    }
}
