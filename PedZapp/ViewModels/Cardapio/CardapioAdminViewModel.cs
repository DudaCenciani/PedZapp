namespace PedZapp.ViewModels.Cardapio
{
    public class CardapioAdminViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public bool Publicado { get; set; }
        public int TotalCategoriasAtivas { get; set; }
        public int TotalProdutosAtivos { get; set; }
        public int TotalAdicionaisAtivos { get; set; }
    }

    public class CardapioEditorViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public IReadOnlyList<CardapioCategoriaEditorViewModel> Categorias { get; set; } = [];
    }

    public class CardapioCategoriaEditorViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public int OrdemExibicao { get; set; }
        public IReadOnlyList<CardapioProdutoEditorViewModel> Produtos { get; set; } = [];
    }

    public class CardapioProdutoEditorViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public bool Ativo { get; set; }
        public bool Destaque { get; set; }
        public int OrdemExibicao { get; set; }
    }
}
