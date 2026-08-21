using Microsoft.AspNetCore.Http;

namespace PedZapp.Services;

/// <summary>Valida arquivos de imagem antes de qualquer gravação binária no banco.</summary>
public interface IImagemEmpresaService
{
    Task<(byte[]? Dados, string? TipoConteudo, string? NomeArquivo, string? Erro)> ValidarAsync(IFormFile arquivo);
}
