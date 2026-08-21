namespace PedZapp.Models
{
    public class AdicionalCategoria
    {
        public int AdicionalId { get; set; }
        public Adicional? Adicional { get; set; }

        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }
    }
}
