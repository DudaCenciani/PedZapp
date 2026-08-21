namespace PedZapp.ViewModels.Produto
{
    public class ProdutoListViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal Preco { get; set; }
        public decimal? PrecoPromocional { get; set; }
        // Indica a existência sem carregar bytes; a View monta a URL segura pelo slug e Id.
        public bool PossuiImagem { get; set; }
        // Mantém compatibilidade visual até a View migrar integralmente para a URL do endpoint binário.
        public string? Imagem { get; set; }
        public bool Ativo { get; set; }
        // Disponível representa estoque/operação temporária e é independente de o cadastro estar ativo.
        public bool Disponivel { get; set; }
        public bool Destaque { get; set; }
        public int? TempoPreparoMinutos { get; set; }
        public bool PermiteObservacao { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;
    }

    public class ProdutoCategoriaViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public IReadOnlyList<ProdutoListViewModel> Produtos { get; set; } = [];
    }

    public class ProdutoCategoriaOptionViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}
