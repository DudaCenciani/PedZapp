using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PedZapp.ViewModels.Produto
{
    public class ProdutoCreateViewModel
    {
        [Required(ErrorMessage = "Informe o nome do produto.")]
        [StringLength(160)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione uma categoria.")]
        public int? CategoriaId { get; set; }

        [StringLength(1000)]
        public string? Descricao { get; set; }

        [Range(
     typeof(decimal),
     "0.01",
     "999999999.99",
     ParseLimitsInInvariantCulture = true,
     ErrorMessage = "O preço deve estar entre R$ 0,01 e R$ 999.999.999,99.")]
        public decimal Preco { get; set; }
        [Range(
            typeof(decimal),
            "0.01",
            "999999999.99",
            ParseLimitsInInvariantCulture = true,
            ErrorMessage = "O preço promocional deve estar entre R$ 0,01 e R$ 999.999.999,99.")]
        public decimal? PrecoPromocional { get; set; }

        [Range(1, 1440, ErrorMessage = "Informe um tempo entre 1 e 1440 minutos.")]
        public int? TempoPreparoMinutos { get; set; }

        // Recebe o binário transitório; a imagem persistida fica em ProdutoImagem no banco.
        public IFormFile? ImagemArquivo { get; set; }
        public bool RemoverImagem { get; set; }
        // Mantido temporariamente para renderizar formulários legados; novas imagens usam ImagemArquivo.
        public string? Imagem { get; set; }

        public bool Destaque { get; set; }
        public bool Ativo { get; set; } = true;
        public bool PermiteObservacao { get; set; } = true;
    }
}
