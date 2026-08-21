using System.ComponentModel.DataAnnotations;

namespace PedZapp.Models
{
    /// <summary>
    /// Produto comercial de uma empresa. CategoriaId e EmpresaId devem apontar para o mesmo tenant;
    /// essa combinação é validada nos fluxos administrativos e de criação de pedidos.
    /// </summary>
    public class Produto
    {
        public int Id { get; set; }

        public int EmpresaId { get; set; }

        public Empresa? Empresa { get; set; }

        public int CategoriaId { get; set; }

        public Categoria? Categoria { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Preco { get; set; }

        public decimal? PrecoPromocional { get; set; }

        public string? Imagem { get; set; }

        // A imagem nova é binária e possui tabela própria para não trafegar bytes nas consultas de produto.
        public ProdutoImagem? ImagemProduto { get; set; }

        public bool Ativo { get; set; } = true;

        // Ativo mantém o cadastro no cardápio; Disponivel controla somente a venda temporária sem apagar o produto.
        public bool Disponivel { get; set; } = true;

        public bool Destaque { get; set; }

        public int? TempoPreparoMinutos { get; set; }

        public bool PermiteObservacao { get; set; } = true;

        public int OrdemExibicao { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }

}
