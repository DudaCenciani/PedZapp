namespace PedZapp.Models;

/// <summary>Imagem binária de um produto, isolada pela empresa e carregada somente pelo endpoint de arquivo.</summary>
public class ProdutoImagem
{
    public int Id { get; set; }
    // Mantém a fronteira de tenant também na tabela de imagens.
    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
    // Garante uma imagem atual por produto.
    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    // Armazena bytes em varbinary(max), nunca caminho físico ou Base64 textual.
    public byte[] Dados { get; set; } = [];
    public string TipoConteudo { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;
    public long Tamanho { get; set; }
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
}
