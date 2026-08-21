using Microsoft.AspNetCore.Http;

namespace PedZapp.Services;

/// <summary>Confere extensão, MIME informado e assinatura binária para bloquear arquivos renomeados.</summary>
public sealed class ImagemEmpresaService : IImagemEmpresaService
{
    // Limita uploads de logo e produto para evitar consumo excessivo de varbinary(max).
    private const long TamanhoMaximo = 2 * 1024 * 1024;
    // Aceita somente formatos raster previstos para o PedZapp.
    private static readonly Dictionary<string, string> Tipos = new(StringComparer.OrdinalIgnoreCase) { [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp" };

    public async Task<(byte[]? Dados, string? TipoConteudo, string? NomeArquivo, string? Erro)> ValidarAsync(IFormFile arquivo)
    {
        // Rejeita arquivo vazio, grande ou com extensão/MIME não permitido antes de ler seus bytes.
        var extensao = Path.GetExtension(arquivo.FileName);
        if (arquivo.Length == 0 || arquivo.Length > TamanhoMaximo || !Tipos.TryGetValue(extensao, out var tipoEsperado) || !string.Equals(arquivo.ContentType, tipoEsperado, StringComparison.OrdinalIgnoreCase)) return (null, null, null, "Envie uma imagem JPG, PNG ou WebP de até 2 MB.");
        // Materializa o arquivo uma vez para conferir a assinatura e persistir exatamente os mesmos bytes.
        await using var memoria = new MemoryStream(); await arquivo.CopyToAsync(memoria); var dados = memoria.ToArray();
        // A assinatura é a validação determinante; ContentType e extensão podem ser forjados pelo navegador.
        if (!AssinaturaValida(dados, tipoEsperado)) return (null, null, null, "O arquivo enviado não possui uma assinatura de imagem válida.");
        return (dados, tipoEsperado, Path.GetFileName(arquivo.FileName), null);
    }

    // Reconhece assinaturas mínimas de JPEG, PNG e WebP sem aceitar formatos executáveis ou vetoriais.
    private static bool AssinaturaValida(byte[] b, string tipo) => tipo switch
    {
        "image/jpeg" => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
        "image/png" => b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A,
        "image/webp" => b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50,
        _ => false
    };
}
