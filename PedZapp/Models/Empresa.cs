
namespace PedZapp.Models
{
    /// <summary>
    /// Tenant do SaaS. O slug identifica a empresa nas URLs e o Id é a fronteira
    /// de isolamento usada em consultas, gravações e relacionamentos operacionais.
    /// </summary>
    public class Empresa
    {
        public int Id { get; set; }

        // Identificação
        public string NomeFantasia { get; set; }
            = string.Empty;

        public string? RazaoSocial { get; set; }

        public string CpfCnpj { get; set; }
            = string.Empty;

        public string Slug { get; set; }
            = string.Empty;

        // Contato
        public string Email { get; set; }
            = string.Empty;

        public string? Telefone { get; set; }

        public string? WhatsApp { get; set; }

        // Loja
        public string? Logo { get; set; }

        public byte[]? LogoDados { get; set; }
        public string? LogoTipoConteudo { get; set; }
        public string? LogoNomeArquivo { get; set; }
        public long? LogoTamanho { get; set; }
        public DateTime? LogoAtualizadaEm { get; set; }

        public string? Descricao { get; set; }

        // Endereço
        public string? Endereco { get; set; }

        public string? Numero { get; set; }

        public string? Bairro { get; set; }

        public string? Cidade { get; set; }

        public string? Estado { get; set; }

        public string? CEP { get; set; }

        // Sistema
        public bool Ativa { get; set; }
            = true;

        public bool PlanoAtivo { get; set; }
            = true;

        public DateTime DataCriacao { get; set; }
            = DateTime.Now;

        public DateTime? DataExpiracaoPlano
        { get; set; }

        public ICollection<Categoria>
    Categorias
        { get; set; }
    = new List<Categoria>();

        public ICollection<Produto>
            Produtos
        { get; set; }
            = new List<Produto>();

        public ICollection<Adicional> Adicionais { get; set; } = new List<Adicional>();
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
        public ICollection<Mesa> Mesas { get; set; } = new List<Mesa>();
        public ICollection<Comanda> Comandas { get; set; } = new List<Comanda>();
        public ICollection<ImpressaoPedido> ImpressoesPedido { get; set; } = new List<ImpressaoPedido>();

        public ICollection<BairroEntrega> BairrosEntrega { get; set; } = new List<BairroEntrega>();

        public ICollection<FormaPagamento> FormasPagamento { get; set; } = new List<FormaPagamento>();
        public ICollection<HorarioFuncionamento> HorariosFuncionamento { get; set; } = new List<HorarioFuncionamento>();
        public ConfiguracaoLoja? ConfiguracaoLoja { get; set; }

        // Relacionamento
        public ICollection<ApplicationUser>
            Usuarios
        { get; set; }
            = new List<ApplicationUser>();
    }

}

