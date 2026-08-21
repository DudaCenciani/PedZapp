namespace PedZapp.ViewModels.Cardapio
{
    /// <summary>
    /// Dados públicos consumidos pela View do cardápio. O CardapioController projeta somente campos
    /// necessários e ativos, sem expor entidades completas ou identificadores de empresa.
    /// </summary>
    public class CardapioPublicoViewModel
    {
        public string Slug { get; set; } = string.Empty; public string NomeFantasia { get; set; } = string.Empty; public string? Descricao { get; set; }
        // A View recebe apenas a existência e a versão da logo; os bytes permanecem restritos ao endpoint público por slug.
        public bool PossuiLogo { get; set; }
        public long? LogoVersao { get; set; }
        public string CorPrimaria { get; set; } = "#F6C445"; public string CorSecundaria { get; set; } = "#C98D86"; public bool AbertaAgora { get; set; }
        // Informação pública já configurada pela empresa, exibida apenas como referência visual no cabeçalho.
        public decimal? PedidoMinimo { get; set; }
        public string? Telefone { get; set; } public string? WhatsApp { get; set; }
        public IReadOnlyList<CardapioCategoriaViewModel> Categorias { get; set; } = [];
    }
    public class CardapioCategoriaViewModel { public int Id { get; set; } public string Nome { get; set; } = string.Empty; public IReadOnlyList<CardapioProdutoViewModel> Produtos { get; set; } = []; }
    public class CardapioProdutoViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal Preco { get; set; }
        public decimal? PrecoPromocional { get; set; }
        public string? Imagem { get; set; }
        public bool Destaque { get; set; }
        // Indisponível continua visível, porém não pode ser incluído em novos pedidos.
        public bool Disponivel { get; set; }
        public bool PermiteObservacao { get; set; }
        public IReadOnlyList<CardapioAdicionalViewModel> Adicionais { get; set; } = [];
    }
    public class CardapioAdicionalViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int? MaximoSelecao { get; set; }
    }
}
