namespace PedZapp.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        public string Nome { get; set; }
            = string.Empty;

        public bool Ativa { get; set; }
            = true;

        public int OrdemExibicao { get; set; }

        public int EmpresaId { get; set; }

        public Empresa? Empresa { get; set; }
            = null!;

        public ICollection<Produto> Produtos { get; set; }
            = new List<Produto>();

        public ICollection<AdicionalCategoria> AdicionalCategorias { get; set; }
            = new List<AdicionalCategoria>();
    }
}
