namespace PedZapp.ViewModels.Adicional
{
    public class AdicionaisIndexViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string? Busca { get; set; }
        public int? CategoriaId { get; set; }
        public string? Status { get; set; }
        public IReadOnlyList<AdicionalCategoriaOptionViewModel> Categorias { get; set; } = [];
        public IReadOnlyList<AdicionalListViewModel> Adicionais { get; set; } = [];
        public AdicionalFormViewModel NovoAdicional { get; set; } = new();
    }

    public class AdicionalCategoriaOptionViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class AdicionalListViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal Preco { get; set; }
        public bool Ativo { get; set; }
        public int? MaximoSelecao { get; set; }
        public IReadOnlyList<int> CategoriaIds { get; set; } = [];
        public IReadOnlyList<string> Categorias { get; set; } = [];
    }
}
