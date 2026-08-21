namespace PedZapp.ViewModels.Produto
{
    public class ProdutosIndexVM
    {
        public string Slug { get; set; } = string.Empty;

        public int TotalProdutos { get; set; }

        public IReadOnlyList<ProdutoCategoriaViewModel> ProdutosPorCategoria { get; set; } = [];

        public IReadOnlyList<ProdutoCategoriaOptionViewModel> Categorias { get; set; } = [];

        public ProdutoCreateViewModel NovoProduto { get; set; } = new();
    }
}
